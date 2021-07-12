using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SpotifyAPI.Web;
using TaggBot.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using TaggBot.Preconditions;
using Discord.WebSocket;
using System.Net;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Xabe.FFmpeg;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace TaggBot.Modules
{

    public class PublicModule : ModuleBase<SocketCommandContext>
    {

        public SpotifyClientService SpotifyClientService { get; set; }
        public YoutubeClientService YoutubeClientService { get; set; }
        public DiscordSocketClient DiscordClientService { get; set; }
        public FFMPEGService FFMPEGService { get; set; }

        JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

    /// <summary>
    /// Test command that makes the bot reply "pong!".
    /// </summary>
    /// <returns></returns>
    [Command("ping")]
        [CommandCooldown(10)]
        public Task PingAsync()
            => ReplyAsync("pong!");

        /// <summary>
        /// Copies a user's avatar and nickname and assigns it to the bot.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns></returns>

        // TODO: Check Discord API rate limits on avatar changes and set command cooldown appropriately.
        [Command("clone")]
        [CommandCooldown(60)]
        public async Task Clone(IGuildUser user, [Remainder] string message)
        {
            await Context.Guild.DownloadUsersAsync(); // Update guild users (as per new Discord API terms)
            using (WebClient client = new WebClient())
            {
                client.DownloadFileAsync(new Uri(user.GetAvatarUrl()), @"D:\TaggBot\avatars\" + user.Id + ".png");
            }
            await Task.Delay(2000); // Delay to ensure the file download happens in time
            var avatar = new FileStream(@"D:\TaggBot\avatars\" + user.Id + ".png", FileMode.Open);
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Discord.Image(avatar));
            await Context.Guild.GetUser(DiscordClientService.CurrentUser.Id).ModifyAsync(x =>
            {
                x.Nickname = user.Nickname == null ? user.Username : user.Nickname;
            });
            await ReplyAsync(message);
        }

        /// <summary>
        /// replaces the audio of the attached video with the audio
        /// of a youtube video id (youtube.com/watch?v=________)
        /// with the audio starting at the supplied start time.
        /// </summary>
        /// <param name="videoId">Youtube video id of the audio to use</param>
        /// <param name="audioStart">Time at which to start the audio</param>
        /// <returns></returns>
        [Command("music")]
        public async Task Music(string videoId, float audioStart)
        {
            string input = "";
            string attachmentUrl = "";
            if(Context.Message.Attachments.Count != 1)
            {
                await ReplyAsync("Please attach 1 video to your message.");
                return;
            }
            foreach (Attachment a in Context.Message.Attachments) // Easy way to grab elements from a Collection
            {
                input = $@"D:\TaggBot\video\base\{a.Filename}";
                attachmentUrl = a.Url;
            }
            // Delete remaining videos from previous operations
            File.Delete(@"D:\TaggBot\trim.mp4");
            File.Delete($@"D:\TaggBot\{videoId}.mp4");
            if (File.Exists(input)) { File.Delete(input); }
            
            using (WebClient client = new WebClient()) // Download base video
            {
                client.DownloadFile(new Uri(attachmentUrl), input);
                Console.WriteLine("Downloaded file from " + attachmentUrl);
            }

            ShellFile so = ShellFile.FromFilePath(input); // Open file to read duration data
            double nanoseconds;
            double.TryParse(so.Properties.System.Media.Duration.Value.ToString(),
            out nanoseconds);
            //Console.WriteLine("Duration (seconds):" + nanoseconds/10000000);

            var manifest = await YoutubeClientService.GetClient().Videos.Streams.GetManifestAsync(videoId); // Get video manifest
            var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate(); // Get video info
            await YoutubeClientService.GetClient().Videos.Streams.DownloadAsync(streamInfo, $@"D:\TaggBot\video\{videoId}.{streamInfo.Container}"); // Dowmload the video

            // Trim the audio to fit input duration
            var trimAudio = await FFmpeg.Conversions.FromSnippet.Split($@"D:\TaggBot\video\{videoId}.{streamInfo.Container}", $@"D:\TaggBot\trim.mp4", TimeSpan.FromSeconds(audioStart), TimeSpan.FromSeconds(nanoseconds / 10000000));
            await trimAudio.Start();
            // Merge audio and video
            var conversion = await FFmpeg.Conversions.FromSnippet.AddAudio(input, $@"D:\TaggBot\trim.mp4", $@"D:\TaggBot\{videoId}.mp4");
            await conversion.Start();
            // Send created video
            await Context.Channel.SendFileAsync($@"D:\TaggBot\{videoId}.mp4");
        }

        /// <summary>
        /// Resets TaggBot to its original avatar and nickname.
        /// </summary>
        /// <returns></returns>
        [Command("unclone")]
        [CommandCooldown(60)]
        public async Task Unclone()
        {
            var avatar = new FileStream(@"D:\TaggBot\avatars\default.png", FileMode.Open);
            await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Discord.Image(avatar));
            await Context.Guild.GetUser(DiscordClientService.CurrentUser.Id).ModifyAsync(x =>
            {
                x.Nickname = null;
            });
        }

        /// <summary>
        /// Sends an embed containing song info about the given spotify track.
        /// </summary>
        /// <param name="spotifyTrackLink">A link to a spotify track.</param>
        /// <returns></returns>
        [Command("spotify")]
        public async Task SpotifyAsync([Remainder] string spotifyTrackLink)
        {
            try
            {
                string trackId = spotifyTrackLink.Split('/')[4].Split('?')[0]; // Parse the track id from the track's url
                FullTrack fullTrack = await SpotifyClientService.GetClient().Tracks.Get(trackId); // Get full track object
                TrackAudioFeatures trackAudioFeatures = await SpotifyClientService.GetClient().Tracks.GetAudioFeatures(trackId); // Get audio features of the track

                EmbedBuilder builder = new EmbedBuilder(); // Begin building the embed to send
                builder.ThumbnailUrl = fullTrack.Album.Images[0].Url; // Set thumbnail to the album's image
                builder.Title = fullTrack.Artists[0].Name + " - " + fullTrack.Name; // Set title to artist - song name
                builder.Color = new Color(30, 215, 96); // Spotify's brand colour
                builder.Description = "From: " + fullTrack.Album.Name; // Put album in the embed description
                // Add audio feature info
                builder.AddField("Danceability", Math.Round(trackAudioFeatures.Danceability * 100, 1) + "%", true);
                builder.AddField("Energy", Math.Round(trackAudioFeatures.Energy * 100, 1) + "%", true);
                builder.AddField("Happiness", Math.Round(trackAudioFeatures.Valence * 100, 1) + "%", true);
                builder.AddField("Speech", Math.Round(trackAudioFeatures.Speechiness * 100, 2) + "%", true);
                builder.AddField("Instrumental", Math.Round(trackAudioFeatures.Instrumentalness * 100, 2) + "%", true);

                await ReplyAsync(embed: builder.Build()); // Build and send the embed
            }
            catch(Exception e)
            {
                await ReplyAsync(e.Message);
            }
        }

        /// <summary>
        /// Bans a given user from the server with an optional reason.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }

        [Command("list")]
        public Task ListAsync(params string[] objects)
            => ReplyAsync("You listed: " + string.Join("; ", objects));

        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");
    }
}

using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaggBot.Services
{
    public class SpotifyClientService
    {
        private SpotifyClient _spotifyClient;

        public SpotifyClientService()
        {
            _spotifyClient = new SpotifyClient(File.ReadAllLines(@"D:\TaggBot\tokens.txt")[1]);
        }

        public SpotifyClient GetClient()
        {
            return _spotifyClient;
        }
    }
}

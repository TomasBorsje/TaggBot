using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;

namespace TaggBot.Services
{
    public class YoutubeClientService
    {
        private YoutubeClient _youtubeClient;

        public YoutubeClientService()
        {
            _youtubeClient = new YoutubeClient();
        }

        public YoutubeClient GetClient()
        {
            return _youtubeClient;
        }
    }
}

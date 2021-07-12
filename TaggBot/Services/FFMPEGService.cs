using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace TaggBot.Services
{
    public class FFMPEGService
    {
        private Conversion _ffmpegClient;

        public FFMPEGService()
        {
            _ffmpegClient = new Conversion();
        }

        public Conversion GetClient()
        {
            return _ffmpegClient;
        }
    }
}

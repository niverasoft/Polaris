using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.VoiceNext;
using Polaris.Discord;
using Polaris.VoiceCapture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Core
{
    public class ServerSpeechRecognitionCore
    {
        private LavalinkNodeConnection _lava;
        private VoiceNextConnection _vc;
        private Dictionary<uint, SpeechRecognizer> _speechCapture;

        public ServerSpeechRecognitionCore()
        {
            _lava = null;          
        }

        public void OnConnectedToVoice(DiscordChannel voice)
        {
            _vc = DiscordNetworkHandlers.GlobalClient.GetVoiceNext().GetConnection(voice.Guild);

            if (_vc.TargetChannel == null)
                return;


        }
    }
}
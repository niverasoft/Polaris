using System;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
using System.IO;

using DSharpPlus.VoiceNext;

namespace Polaris.VoiceCapture
{
    public class SpeechRecognizer
    {
        private SpeechRecognitionEngine _engine;
        private Action<RecognitionResult> _onRecognizedPending;

        public SpeechRecognizer()
        {
            _engine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            _engine.LoadGrammar(new DictationGrammar());
            _engine.SpeechRecognized += OnRecognized;
        }

        private void OnRecognized(object sender, SpeechRecognizedEventArgs ev)
        {
            _onRecognizedPending(ev.Result);
            _engine.RecognizeAsyncStop();
        }

        public void Recognize(AudioFormat audioFormat, Stream inputStream, Action<RecognitionResult> onRecognized)
        {
            _onRecognizedPending = onRecognized;
            _engine.SetInputToAudioStream(inputStream, new SpeechAudioFormatInfo(audioFormat.SampleRate, AudioBitsPerSample.Sixteen, audioFormat.ChannelCount == 2 ? AudioChannel.Stereo : AudioChannel.Mono));
            _engine.RecognizeAsync(RecognizeMode.Multiple);
        }
    }
}

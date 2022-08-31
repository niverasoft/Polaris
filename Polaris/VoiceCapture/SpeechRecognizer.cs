using System.IO;

using Syn.Speech.Api;

namespace Polaris.VoiceCapture
{
    public class SpeechRecognizer
    {
        private Configuration _config;
        private StreamSpeechRecognizer _recognizer;

        public SpeechRecognizer()
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/SpeechData"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/SpeechData");

            _config = new Configuration
            {
                SampleRate = 48000,
                AcousticModelPath = Directory.GetCurrentDirectory(),
                DictionaryPath = Path.Combine(Directory.GetCurrentDirectory(), "cmudict-en-us.dict"),
                LanguageModelPath = Path.Combine(Directory.GetCurrentDirectory(), "en-us.lm.dmp"),
            };

            _recognizer = new StreamSpeechRecognizer(_config);
        }

        public void StartRecognize(Stream inputStream)
        {
            _recognizer.StartRecognition(inputStream);
        }

        public string StopRecognize()
        {
            _recognizer.StopRecognition();
            var result = _recognizer.GetResult();

            return result == null ? null : result.GetHypothesis();
        }
    }
}

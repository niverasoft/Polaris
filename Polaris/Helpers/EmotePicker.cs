using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Polaris.Helpers
{
    public static class EmotePicker
    {
        public static readonly string ErrorEmote = "❌";
        public static readonly string CheckEmote = "✅";
        public static readonly string WarnEmote = "⚠️";
        public static readonly string LinkEmote = "🔗";
        public static readonly string ShuffleEmote = "🔀";
        public static readonly string RepeatEmote = "🔁";
        public static readonly string PlayEmote = "▶️";
        public static readonly string NextTrackEmote = "⏭️";
        public static readonly string TrackForwardEmote = "⏩";
        public static readonly string PauseEmote = "⏸️";
        public static readonly string StopEmote = "⏹️";
        public static readonly string TrackReverseEmote = "⏪";
        public static readonly string BlueCircleEmote = "🔵";
        public static readonly string RedCircleEmote = "🔴";
        public static readonly string GreenCircleEmote = "🟢";
        public static readonly string YellowCircleEmote = "🟡";
        public static readonly string BlackCircleEmote = "⚫";
        public static readonly string NotesEmote = "🎶";
        public static readonly string NoteEmote = "🎵";
        public static readonly string LoudspeakerEmote = "📢";
        public static readonly string MutedSpeakerEmote = "🔇";
        public static readonly string SpeakerLowEmote = "🔈";
        public static readonly string SpeakerMediumEmote = "🔉";
        public static readonly string SpeakerHighEmote = "🔊";
        public static readonly string CycloneEmote = "🌀";
        public static readonly string SlashedBellEmote = "🔕";
        public static readonly string BellEmote = "🔔";
        public static readonly string AntennaBarsEmote = "📶";
        public static readonly string RadioButtonEmote = "🔘";
        public static readonly string WaveHandEmote = "👋";
        public static readonly string PopEmote = "💥";
        public static readonly string GearEmote = "⚙️";
        public static readonly string LoadingGif = "<a:polarisLoad:1014229111402663966>";
    }

    public static class NumberEmotes
    {
        public static readonly string Zero = "0️⃣";
        public static readonly string One = "1️⃣";
        public static readonly string Two = "2️⃣";
        public static readonly string Three = "3️⃣";
        public static readonly string Four = "4️⃣";
        public static readonly string Five = "5️⃣";
        public static readonly string Six = "6️⃣";
        public static readonly string Seven = "7️⃣";
        public static readonly string Eight = "8️⃣";
        public static readonly string Nine = "9️⃣";
        public static readonly string Hashtag = "#️⃣";
        public static readonly string Ten = "🔟";

        public static readonly string Minus = "➖";

        public static Dictionary<string, string> NumbersToEmotes = new Dictionary<string, string>();

        static NumberEmotes()
        {
            foreach (var field in typeof(NumberEmotes).GetFields())
            {
                if (field.FieldType != typeof(string))
                    continue;

                NumbersToEmotes.Add(field.Name, field.GetValue(null).ToString());
            }
        }

        public static string SingleNumberToString(double singleNumber)
        {
            if (double.IsNegative(singleNumber))
            {
                string str = singleNumber.ToString().Replace("-", "");

                singleNumber = double.Parse(str);
            }

            if (singleNumber == 0)
                return "Zero";

            if (singleNumber == 1)
                return "One";

            if (singleNumber == 2)
                return "Two";

            if (singleNumber == 3)
                return "Three";

            if (singleNumber == 4)
                return "Four";

            if (singleNumber == 5)
                return "Five";

            if (singleNumber == 6)
                return "Six";

            if (singleNumber == 7)
                return "Seven";

            if (singleNumber == 8)
                return "Eight";

            if (singleNumber == 9)
                return "Nine";

            return null;
        }

        public static List<string> MultipleToString(double num)
        {
            List<string> numbers = new List<string>();

            string str = num.ToString();

            foreach (char number in str)
            {
                numbers.Add(SingleNumberToString(double.Parse(number.ToString())));
            }

            return numbers;
        }

        public static string ReplaceByEmotes(List<string> numbers)
        {
            string str = "";

            foreach (var num in numbers)
            {
                var emote = NumberToEmote(num);

                if (emote != null)
                {
                    str += $"{emote}";
                }
            }

            return str;
        }

        public static string NumberToEmote(string str)
        {
            if (NumbersToEmotes.TryGetValue(str, out string emote))
                return emote;

            return null;
        }

        public static string NumberToEmote(double num)
        {
            if (num >= -9 && num <= 9)
                return ReplaceByEmotes(new List<string> { SingleNumberToString(num) });

            if (double.IsNegative(num))
            {
                return $"{Minus}{ReplaceByEmotes(MultipleToString(num))}";
            }
            else
            {
                return ReplaceByEmotes(MultipleToString(num));
            }
        }
    }
}

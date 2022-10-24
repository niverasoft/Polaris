using DSharpPlus.Entities;

namespace Polaris.Helpers
{
    public static class EmbedHelper
    {
        public static string VolumeEmoteToUse(int volume)
        {
            if (volume <= 0)
                return EmotePicker.MutedSpeakerEmote;

            if (volume < 25)
                return EmotePicker.SpeakerLowEmote;

            if (volume < 50)
                return EmotePicker.SpeakerMediumEmote;

            return EmotePicker.SpeakerHighEmote;
        }

        public static DiscordEmbedBuilder CheckLimits(this DiscordEmbedBuilder build)
        {
            return build;
        }

        public static DiscordEmbedBuilder AddEmoteTitle(this DiscordEmbedBuilder build, string emote)
        {
            build.Title = $"{emote} {build.Title}";

            return build;
        }

        public static DiscordEmbedBuilder AddEmoteDescription(this DiscordEmbedBuilder build, string emote)
        {
            build.Description = $"{emote} {build.Description}";

            return build;
        }

        public static DiscordEmbedField AddEmoteName(this DiscordEmbedField field, string emote)
        {
            field.Name = $"{emote} {field.Name}";

            return field;
        }

        public static DiscordEmbedBuilder AddEmoteAuthor(this DiscordEmbedBuilder build, string emote)
        {
            build.Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                IconUrl = null,
                Name = $"{emote} {build.Author.Name}",
                Url = null
            };

            return build;
        }

        public static DiscordEmbedBuilder MakeInfo(this DiscordEmbedBuilder build)
            => build.AddEmoteAuthor(EmotePicker.BlueCircleEmote).WithColor(ColorPicker.InfoColor).CheckLimits();

        public static DiscordEmbedBuilder MakeWarn(this DiscordEmbedBuilder build)
            => build.AddEmoteAuthor(EmotePicker.WarnEmote).WithColor(ColorPicker.WarnColor).CheckLimits();

        public static DiscordEmbedBuilder MakeError(this DiscordEmbedBuilder build)
            => build.AddEmoteAuthor(EmotePicker.ErrorEmote).WithColor(ColorPicker.ErrorColor).CheckLimits();

        public static DiscordEmbedBuilder MakeSuccess(this DiscordEmbedBuilder build)
            => build.AddEmoteAuthor(EmotePicker.CheckEmote).WithColor(ColorPicker.SuccessColor).CheckLimits();
    }
}

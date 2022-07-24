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
            if (build.Author != null)
            {
                if (!string.IsNullOrEmpty(build.Author.Name))
                {
                    if (build.Author.Name.Length > EmbedLimits.AuthorName)
                    {
                        build.Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            IconUrl = build.Author.IconUrl,
                            Url = build.Author.Url,
                            Name = StringHelpers.RemoveAfterIndex(build.Author.Name, EmbedLimits.AuthorName - 1)
                        };
                    }
                }
            }

            if (!string.IsNullOrEmpty(build.Description))
            {
                if (build.Description.Length > EmbedLimits.Description)
                {
                    build.Description = StringHelpers.RemoveAfterIndex(build.Description, EmbedLimits.Description - 1);
                }
            }

            if (build.Fields.Count > 0)
            {
                foreach (var field in build.Fields)
                {
                    if (!string.IsNullOrEmpty(field.Name))
                    {
                        if (field.Name.Length > EmbedLimits.FieldName)
                            field.Name = StringHelpers.RemoveAfterIndex(field.Name, EmbedLimits.FieldName - 1);
                    }

                    if (!string.IsNullOrEmpty(field.Value))
                    {
                        if (field.Value.Length > EmbedLimits.FieldValue)
                            field.Value = StringHelpers.RemoveAfterIndex(field.Value, EmbedLimits.FieldValue - 1);
                    }
                }

                if (build.Fields.Count > EmbedLimits.FieldCount)
                {
                    build.RemoveFieldRange(EmbedLimits.FieldCount - 1, build.Fields.Count - EmbedLimits.FieldCount);
                }
            }

            if (build.Footer != null)
            {
                if (!string.IsNullOrEmpty(build.Footer.Text))
                {
                    if (build.Footer.Text.Length > EmbedLimits.FooterText)
                    {
                        build.Footer.Text = StringHelpers.RemoveAfterIndex(build.Footer.Text, EmbedLimits.FooterText - 1);
                    }
                }
            }

            if (!string.IsNullOrEmpty(build.Title))
            {
                if (build.Title.Length > EmbedLimits.Title)
                    build.Title = StringHelpers.RemoveAfterIndex(build.Title, EmbedLimits.Title - 1);
            }

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

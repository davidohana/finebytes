using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.RenameList.Fields.Media
{
    /// <summary>
    /// Formats <see cref="MediaProperties"/> for formatter tokens and Rename List columns.
    /// </summary>
    internal static class MediaPropertiesFormatting
    {
        /// <summary>
        /// Formats one media property for display.
        /// </summary>
        /// <param name="media">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <param name="context">
        /// Display surface. Media arms are currently identical for Token and Grid; the
        /// parameter is required so Media shares one formatter signature with Image/PDF.
        /// </param>
        /// <returns>Formatted text, or empty when absent.</returns>
        internal static string Format(MediaProperties? media, MediaPropertyField field, PropertyDisplayContext context)
        {
            // No Token vs Grid fork for Media today; keep the parameter for signature parity.
            _ = context;

            if (media is null)
            {
                return string.Empty;
            }

            return field switch
            {
                MediaPropertyField.MimeType => RenameListFieldDisplay.FormatOptionalText(media.MimeType),
                MediaPropertyField.Corrupt => RenameListFieldDisplay.FormatYesNo(media.PossiblyCorrupt),
                MediaPropertyField.Duration => RenameListFieldDisplay.FormatDuration(media.Duration),
                MediaPropertyField.DurationSec => RenameListFieldDisplay.FormatDurationSec(media.Duration),
                MediaPropertyField.MediaTypes => RenameListFieldDisplay.FormatOptionalText(media.MediaTypes),
                MediaPropertyField.Description => RenameListFieldDisplay.FormatOptionalText(media.Description),
                MediaPropertyField.AudioBitrate => RenameListFieldDisplay.FormatPositiveInt(media.AudioBitrate),
                MediaPropertyField.SampleRate => RenameListFieldDisplay.FormatPositiveInt(media.AudioSampleRate),
                MediaPropertyField.BitsPerSample => RenameListFieldDisplay.FormatPositiveInt(media.BitsPerSample),
                MediaPropertyField.Channels => RenameListFieldDisplay.FormatPositiveInt(media.AudioChannels),
                MediaPropertyField.VideoWidth => RenameListFieldDisplay.FormatPositiveInt(media.VideoWidth),
                MediaPropertyField.VideoHeight => RenameListFieldDisplay.FormatPositiveInt(media.VideoHeight),
                MediaPropertyField.PhotoWidth => RenameListFieldDisplay.FormatPositiveInt(media.PhotoWidth),
                MediaPropertyField.PhotoHeight => RenameListFieldDisplay.FormatPositiveInt(media.PhotoHeight),
                MediaPropertyField.PhotoQuality => RenameListFieldDisplay.FormatPositiveInt(media.PhotoQuality),
                _ => throw new UnreachableException(),
            };
        }
    }
}

using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Display and lookup for original Rename List metadata load failures (Phase 6b).
    /// </summary>
    internal static class RenameListFieldLoadError
    {
        /// <summary>
        /// Grid text for a failed original metadata bucket (MFR7 <c>PropDisplay</c> with exception).
        /// </summary>
        internal const string DisplayText = RenameListFieldCatalog.FieldLoadErrorText;

        /// <summary>
        /// Returns whether <paramref name="text"/> is the field-load error display value.
        /// </summary>
        /// <param name="text">Resolved grid text.</param>
        /// <returns><see langword="true"/> when the cell shows a load failure.</returns>
        internal static bool IsErrorDisplayText(string text)
        {
            return string.Equals(text, DisplayText, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the stored load exception for one original field, when present.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <param name="key">Original field key.</param>
        /// <param name="error">Stored exception when the field's metadata bucket failed.</param>
        /// <returns><see langword="true"/> when the field should display <see cref="DisplayText"/>.</returns>
        internal static bool TryGetLoadError(RenameItem item, RenameListFieldKey key, out Exception? error)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (key.IsPreview || !RenameListFieldCatalog.TryGetField(key, out var field))
            {
                error = null;
                return false;
            }

            return TryGetLoadError(item, field.MetadataRequirement, out error);
        }

        /// <summary>
        /// Returns the stored load exception for one metadata requirement, when present.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <param name="requirement">Metadata bucket required by a catalog field.</param>
        /// <param name="error">Stored exception when the bucket failed.</param>
        /// <returns><see langword="true"/> when fields using the requirement should show <see cref="DisplayText"/>.</returns>
        internal static bool TryGetLoadError(
            RenameItem item,
            RenameListMetadataRequirement requirement,
            out Exception? error
        )
        {
            ArgumentNullException.ThrowIfNull(item);
            return _TryGetLoadError(item, requirement, out error);
        }

        /// <summary>
        /// Returns whether resolving an original field would show <see cref="DisplayText"/>.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <param name="key">Original field key.</param>
        /// <returns><see langword="true"/> when the metadata bucket failed to load.</returns>
        internal static bool HasLoadError(RenameItem item, RenameListFieldKey key)
        {
            return TryGetLoadError(item, key, out _);
        }

        /// <summary>
        /// Returns a plain-language explanation for a stored metadata load failure.
        /// </summary>
        /// <param name="error">Stored reader exception.</param>
        /// <param name="requirement">Metadata bucket that failed.</param>
        /// <returns>User-facing explanation; technical details stay on <paramref name="error"/>.</returns>
        internal static string DescribeUserMessage(Exception error, RenameListMetadataRequirement requirement)
        {
            ArgumentNullException.ThrowIfNull(error);

            if (error is IOException or UnauthorizedAccessException)
            {
                return "The file is missing or could not be opened.";
            }

            if (
                requirement.HasFlag(RenameListMetadataRequirement.EmbeddedAudioTags)
                || requirement.HasFlag(RenameListMetadataRequirement.MediaProperties)
            )
            {
                return "This file could not be read as audio or media metadata.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.ImageProperties))
            {
                return "This file could not be read as image or EXIF metadata.";
            }

            return "This field could not be loaded from disk.";
        }

        private static bool _TryGetLoadError(
            RenameItem item,
            RenameListMetadataRequirement requirement,
            out Exception? error
        )
        {
            if (requirement == RenameListMetadataRequirement.None)
            {
                error = null;
                return false;
            }

            if (
                requirement.HasFlag(RenameListMetadataRequirement.EmbeddedAudioTags)
                || requirement.HasFlag(RenameListMetadataRequirement.MediaProperties)
            )
            {
                error = item.GetTagLibMetadataLoadError();
                if (error is not null)
                {
                    return true;
                }
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.ImageProperties))
            {
                error = item.ImagePropertiesLoadError;
                if (error is not null)
                {
                    return true;
                }
            }

            error = null;
            return false;
        }
    }
}

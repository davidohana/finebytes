using Mfr.Models.Rename;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Display and lookup for original Rename List metadata load failures (Phase 6b).
    /// </summary>
    internal static class RenameListMetadataLoadErrors
    {
        /// <summary>
        /// Returns the stored load exception for one original field, when present.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <param name="key">Original field key.</param>
        /// <param name="error">Stored exception when the field's metadata bucket failed.</param>
        /// <returns><see langword="true"/> when the field should display <see cref="RenameListFieldCatalog.LoadErrorText"/>.</returns>
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
        /// <returns>
        /// <see langword="true"/> when fields using the requirement should show
        /// <see cref="RenameListFieldCatalog.LoadErrorText"/>.
        /// </returns>
        internal static bool TryGetLoadError(
            RenameItem item,
            RenameListMetadataRequirement requirement,
            out Exception? error
        )
        {
            ArgumentNullException.ThrowIfNull(item);

            if (requirement == RenameListMetadataRequirement.None)
            {
                error = null;
                return false;
            }

            foreach (var bucket in RenameListMetadataBuckets.All)
            {
                if (!requirement.HasFlag(bucket))
                {
                    continue;
                }

                error = item.GetMetadataLoadError(bucket);
                if (error is not null)
                {
                    return true;
                }
            }

            error = null;
            return false;
        }

        /// <summary>
        /// Returns whether resolving an original field would show <see cref="RenameListFieldCatalog.LoadErrorText"/>.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <param name="key">Original field key.</param>
        /// <returns><see langword="true"/> when the metadata bucket failed to load.</returns>
        internal static bool HasLoadError(RenameItem item, RenameListFieldKey key)
        {
            return TryGetLoadError(item, key, out _);
        }

        /// <summary>
        /// Returns whether the row has any original metadata load failure.
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <returns><see langword="true"/> when any disk metadata bucket failed to load.</returns>
        internal static bool HasAny(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            foreach (var bucket in RenameListMetadataBuckets.All)
            {
                if (item.GetMetadataLoadError(bucket) is not null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Lists distinct reader failures stored on the row (at most one per disk metadata bucket).
        /// </summary>
        /// <param name="item">Rename row.</param>
        /// <returns>User-facing entries for Show Load Errors, in reader order.</returns>
        internal static IReadOnlyList<RenameListLoadError> List(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var errors = new List<RenameListLoadError>(RenameListMetadataBuckets.All.Count);
            foreach (var bucket in RenameListMetadataBuckets.All)
            {
                if (item.GetMetadataLoadError(bucket) is not { } bucketError)
                {
                    continue;
                }

                errors.Add(new RenameListLoadError(DescribeUserMessage(bucketError, bucket), bucketError.Message));
            }

            return errors;
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

            if (requirement.HasFlag(RenameListMetadataRequirement.TagLib))
            {
                return "This file could not be read as audio or media metadata.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.ImageProperties))
            {
                return "This file could not be read as image or EXIF metadata.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.Pdf))
            {
                return "This file could not be read as PDF document Info.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.Epub))
            {
                return "This file could not be read as EPUB document Info.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.Office))
            {
                return "This file could not be read as Office document Info.";
            }

            if (requirement.HasFlag(RenameListMetadataRequirement.GeoNames))
            {
                // GeoNamesClient messages are already truncated, user-facing text (rate limits, HTTP, XML).
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    return error.Message.Trim();
                }

                return "Nearby place data could not be loaded from GeoNames.";
            }

            return "This field could not be loaded from disk.";
        }
    }
}

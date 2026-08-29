using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads rename-row metadata buckets for the Rename List grid and Auto-Sort.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Grid and Auto-Sort hydrate via <c>RenameList.EnsureMetadataLoaded</c>. Filters still call per-field ensure on preview.
    /// </para>
    /// </remarks>
    public static class RenameListMetadataLoader
    {
        /// <summary>
        /// Ensures rename-row metadata is loaded for each requirement in <paramref name="metadataRequirement"/>.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="metadataRequirement">Combined metadata requirements for visible columns.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListMetadataRequirement metadataRequirement)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (metadataRequirement.HasFlag(RenameListMetadataRequirement.TagLib))
            {
                _TryEnsureTagLibLoaded(item);
            }

            if (metadataRequirement.HasFlag(RenameListMetadataRequirement.ImageProperties))
            {
                _TryEnsureImagePropertiesLoaded(item);
            }
        }

        /// <summary>
        /// Ensures metadata is loaded for one field key.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="key">Catalog field key being resolved.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(item);
            TryEnsureLoaded(item, RenameListFieldCatalog.GetMetadataRequirement(key));
        }

        /// <summary>
        /// Returns whether <paramref name="item"/> already attempted every load required by <paramref name="requirement"/>.
        /// </summary>
        /// <param name="item">Rename row to inspect.</param>
        /// <param name="requirement">Combined metadata requirement flags.</param>
        /// <returns><see langword="true"/> when no further disk reads are needed for the requirement.</returns>
        public static bool IsRequirementSatisfied(RenameItem item, RenameListMetadataRequirement requirement)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (requirement.HasFlag(RenameListMetadataRequirement.TagLib) && !item.TagLibLoadAttempted)
            {
                return false;
            }

            if (
                requirement.HasFlag(RenameListMetadataRequirement.ImageProperties) && !item.ImagePropertiesLoadAttempted
            )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether any row still needs disk reads for <paramref name="requirement"/>.
        /// </summary>
        /// <param name="items">Rename rows to inspect.</param>
        /// <param name="requirement">Combined metadata requirement flags.</param>
        /// <returns><see langword="true"/> when at least one row still needs loading.</returns>
        public static bool AnyItemNeedsLoad(IEnumerable<RenameItem> items, RenameListMetadataRequirement requirement)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (requirement == RenameListMetadataRequirement.None)
            {
                return false;
            }

            return items.Any(item => !IsRequirementSatisfied(item, requirement));
        }

        private static void _TryEnsureTagLibLoaded(RenameItem item)
        {
            if (item.TagLibLoadAttempted || item.Original.Attributes.IsDirectory())
            {
                return;
            }

            try
            {
                item.EnsureTagLibLoaded();
            }
            catch (Exception ex) when (_IsMetadataReadFailure(ex))
            {
                item.SetTagLibMetadataLoadError(ex);
            }
        }

        private static void _TryEnsureImagePropertiesLoaded(RenameItem item)
        {
            if (item.ImagePropertiesLoadAttempted || item.Original.Attributes.IsDirectory())
            {
                return;
            }

            try
            {
                item.EnsureImagePropertiesLoaded();
            }
            catch (Exception ex) when (_IsMetadataReadFailure(ex))
            {
                item.SetImagePropertiesLoadError(ex);
            }
        }

        private static bool _IsMetadataReadFailure(Exception ex)
        {
            if (ex is InvalidOperationException or IOException or ArgumentException or UnauthorizedAccessException)
            {
                return true;
            }

            // TagLib / MetadataExtractor exceptions without taking a Filters package reference on those libraries.
            var typeName = ex.GetType().Name;
            return typeName is "UnsupportedFormatException" or "CorruptFileException" or "ImageProcessingException";
        }
    }
}

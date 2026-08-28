using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazy-loads rename-row metadata for Rename List grid columns (original-only Phase 7a fields).
    /// </summary>
    public static class RenameListLazyMetadataLoader
    {
        /// <summary>
        /// Ensures lazy rename-row metadata is loaded for each requirement in <paramref name="metadataRequirement"/>.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="metadataRequirement">Combined metadata requirements for visible columns.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListMetadataRequirement metadataRequirement)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (metadataRequirement.HasFlag(RenameListMetadataRequirement.EmbeddedAudioTags))
            {
                _TryEnsureEmbeddedTagsLoaded(item);
            }

            if (metadataRequirement.HasFlag(RenameListMetadataRequirement.MediaProperties))
            {
                _TryEnsureMediaPropertiesLoaded(item);
            }

            if (metadataRequirement.HasFlag(RenameListMetadataRequirement.ImageProperties))
            {
                _TryEnsureImagePropertiesLoaded(item);
            }
        }

        /// <summary>
        /// Ensures embedded audio tags are loaded for one field key.
        /// </summary>
        /// <param name="item">Rename row to hydrate.</param>
        /// <param name="key">Catalog field key being resolved.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(item);
            TryEnsureLoaded(item, RenameListFieldCatalog.GetMetadataRequirement(key));
        }

        private static void _TryEnsureEmbeddedTagsLoaded(RenameItem item)
        {
            if (item.EmbeddedTagsLoadAttempted || item.Original.Attributes.IsDirectory())
            {
                return;
            }

            try
            {
                item.EnsureEmbeddedTagsLoaded();
            }
            catch (Exception ex) when (_IsMetadataReadFailure(ex))
            {
                // Grid cells show empty when tags cannot be read (missing file, unsupported format).
            }
        }

        private static void _TryEnsureMediaPropertiesLoaded(RenameItem item)
        {
            if (item.MediaPropertiesLoadAttempted || item.Original.Attributes.IsDirectory())
            {
                return;
            }

            try
            {
                item.EnsureMediaPropertiesLoaded();
            }
            catch (Exception ex) when (_IsMetadataReadFailure(ex))
            {
                // Grid cells show empty when media properties cannot be read.
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
                // Grid cells show empty when image metadata cannot be read.
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

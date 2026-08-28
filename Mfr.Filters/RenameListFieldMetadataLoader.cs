using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads lazy rename-row metadata for Rename List grid columns (original-only Phase 7a fields).
    /// </summary>
    public static class RenameListFieldMetadataLoader
    {
        /// <summary>
        /// Ensures embedded audio tags are loaded when <paramref name="metadataLoad"/> includes
        /// <see cref="RenameListFieldMetadataLoad.EmbeddedAudioTags"/>.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="metadataLoad">Combined metadata-load flags for visible columns.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListFieldMetadataLoad metadataLoad)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (metadataLoad.HasFlag(RenameListFieldMetadataLoad.EmbeddedAudioTags))
            {
                _TryEnsureEmbeddedTagsLoaded(item);
            }

            if (metadataLoad.HasFlag(RenameListFieldMetadataLoad.ImageProperties))
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
            TryEnsureLoaded(item, RenameListFieldCatalog.GetMetadataLoad(key));
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
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                // Grid cells show empty when tags cannot be read (folder row, missing file, unsupported format).
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
            catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException)
            {
                // Grid cells show empty when image metadata cannot be read.
            }
        }
    }
}

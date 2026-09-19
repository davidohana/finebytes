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
        private static readonly (RenameListMetadataRequirement Bucket, Action<RenameItem> Ensure)[] _BucketEnsures =
        [
            (RenameListMetadataRequirement.TagLib, static item => item.EnsureTagLibLoaded()),
            (RenameListMetadataRequirement.ImageProperties, static item => item.EnsureImagePropertiesLoaded()),
            (RenameListMetadataRequirement.Pdf, static item => item.EnsurePdfLoaded()),
            (RenameListMetadataRequirement.Epub, static item => item.EnsureEpubLoaded()),
            (RenameListMetadataRequirement.Office, static item => item.EnsureOfficeLoaded()),
            (RenameListMetadataRequirement.GeoNames, static item => item.EnsureGeoNamesLoaded()),
        ];

        /// <summary>
        /// Ensures rename-row metadata is loaded for each requirement in <paramref name="metadataRequirement"/>.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="metadataRequirement">Combined metadata requirements for visible columns.</param>
        public static void TryEnsureLoaded(RenameItem item, RenameListMetadataRequirement metadataRequirement)
        {
            ArgumentNullException.ThrowIfNull(item);

            foreach (var (bucket, ensure) in _BucketEnsures)
            {
                if (metadataRequirement.HasFlag(bucket))
                {
                    _TryEnsureBucket(item, bucket, ensure);
                }
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

            foreach (var bucket in RenameListMetadataBuckets.All)
            {
                if (requirement.HasFlag(bucket) && !item.WasMetadataLoadAttempted(bucket))
                {
                    return false;
                }
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

        private static void _TryEnsureBucket(
            RenameItem item,
            RenameListMetadataRequirement bucket,
            Action<RenameItem> ensure
        )
        {
            if (
                item.WasMetadataLoadAttempted(bucket)
                || item.Original.Attributes.IsDirectory()
                || RenameListDiskPaths.IsMissingFromDisk(item)
            )
            {
                return;
            }

            try
            {
                ensure(item);
            }
            catch (Exception ex) when (_IsMetadataReadFailure(ex))
            {
                item.SetMetadataLoadError(bucket, ex);
            }
        }

        private static bool _IsMetadataReadFailure(Exception ex)
        {
            if (
                ex
                is InvalidOperationException
                    or IOException
                    or InvalidDataException
                    or ArgumentException
                    or UnauthorizedAccessException
            )
            {
                return true;
            }

            // TagLib / MetadataExtractor / PdfPig / VersOne.Epub / OpenXml without a Filters package reference.
            // Walk bases so VersOne concretes (EpubPackageException, …) match EpubReaderException.
            for (var type = ex.GetType(); type is not null && type != typeof(object); type = type.BaseType)
            {
                if (
                    type.Name
                    is "UnsupportedFormatException"
                        or "CorruptFileException"
                        or "ImageProcessingException"
                        or "PdfDocumentFormatException"
                        or "PdfDocumentEncryptedException"
                        or "EpubReaderException"
                        or "OpenXmlPackageException"
                        or "FileFormatException"
                )
                {
                    return true;
                }
            }

            return false;
        }
    }
}

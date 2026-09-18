using System.Diagnostics;
using Mfr.Models.RenameList;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// Single-flag disk metadata bucket load state on <see cref="RenameItem"/>.
    /// </summary>
    public sealed partial class RenameItem
    {
        /// <summary>
        /// Returns whether the given disk metadata bucket was already load-attempted this cycle.
        /// </summary>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        /// <returns><see langword="true"/> when a load was already attempted for the bucket.</returns>
        internal bool WasMetadataLoadAttempted(RenameListMetadataRequirement bucket)
        {
            RenameListMetadataBuckets.RequireSingle(bucket);
            return bucket switch
            {
                RenameListMetadataRequirement.TagLib => TagLibLoadAttempted,
                RenameListMetadataRequirement.ImageProperties => ImagePropertiesLoadAttempted,
                RenameListMetadataRequirement.Pdf => PdfLoadAttempted,
                RenameListMetadataRequirement.Epub => EpubLoadAttempted,
                RenameListMetadataRequirement.Office => OfficeLoadAttempted,
                RenameListMetadataRequirement.None => throw new UnreachableException(),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Marks the given disk metadata bucket as load-attempted for this preview cycle.
        /// </summary>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        internal void MarkMetadataLoadAttempted(RenameListMetadataRequirement bucket)
        {
            RenameListMetadataBuckets.RequireSingle(bucket);
            switch (bucket)
            {
                case RenameListMetadataRequirement.TagLib:
                    TagLibLoadAttempted = true;
                    break;
                case RenameListMetadataRequirement.ImageProperties:
                    ImagePropertiesLoadAttempted = true;
                    break;
                case RenameListMetadataRequirement.Pdf:
                    PdfLoadAttempted = true;
                    break;
                case RenameListMetadataRequirement.Epub:
                    EpubLoadAttempted = true;
                    break;
                case RenameListMetadataRequirement.Office:
                    OfficeLoadAttempted = true;
                    break;
                case RenameListMetadataRequirement.None:
                default:
                    throw new UnreachableException();
            }
        }

        /// <summary>
        /// Returns the soft load failure stored for the given disk metadata bucket, when present.
        /// </summary>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        /// <returns>Stored exception, or <see langword="null"/>.</returns>
        internal Exception? GetMetadataLoadError(RenameListMetadataRequirement bucket)
        {
            RenameListMetadataBuckets.RequireSingle(bucket);
            return bucket switch
            {
                RenameListMetadataRequirement.TagLib => TagLibMetadataLoadError,
                RenameListMetadataRequirement.ImageProperties => ImagePropertiesLoadError,
                RenameListMetadataRequirement.Pdf => PdfLoadError,
                RenameListMetadataRequirement.Epub => EpubLoadError,
                RenameListMetadataRequirement.Office => OfficeLoadError,
                RenameListMetadataRequirement.None => throw new UnreachableException(),
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Records a soft load failure for the given disk metadata bucket (Rename List grid path).
        /// </summary>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        /// <param name="ex">Failure from the bucket reader.</param>
        internal void SetMetadataLoadError(RenameListMetadataRequirement bucket, Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            RenameListMetadataBuckets.RequireSingle(bucket);
            switch (bucket)
            {
                case RenameListMetadataRequirement.TagLib:
                    TagLibMetadataLoadError = ex;
                    break;
                case RenameListMetadataRequirement.ImageProperties:
                    ImagePropertiesLoadError = ex;
                    break;
                case RenameListMetadataRequirement.Pdf:
                    PdfLoadError = ex;
                    break;
                case RenameListMetadataRequirement.Epub:
                    EpubLoadError = ex;
                    break;
                case RenameListMetadataRequirement.Office:
                    OfficeLoadError = ex;
                    break;
                case RenameListMetadataRequirement.None:
                default:
                    throw new UnreachableException();
            }
        }

        /// <summary>
        /// Clears attempt and soft-error state for the given disk metadata bucket (not the snapshot DTO).
        /// </summary>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        internal void ClearMetadataLoadState(RenameListMetadataRequirement bucket)
        {
            RenameListMetadataBuckets.RequireSingle(bucket);
            switch (bucket)
            {
                case RenameListMetadataRequirement.TagLib:
                    TagLibLoadAttempted = false;
                    TagLibMetadataLoadError = null;
                    break;
                case RenameListMetadataRequirement.ImageProperties:
                    ImagePropertiesLoadAttempted = false;
                    ImagePropertiesLoadError = null;
                    break;
                case RenameListMetadataRequirement.Pdf:
                    PdfLoadAttempted = false;
                    PdfLoadError = null;
                    break;
                case RenameListMetadataRequirement.Epub:
                    EpubLoadAttempted = false;
                    EpubLoadError = null;
                    break;
                case RenameListMetadataRequirement.Office:
                    OfficeLoadAttempted = false;
                    OfficeLoadError = null;
                    break;
                case RenameListMetadataRequirement.None:
                default:
                    throw new UnreachableException();
            }
        }
    }
}

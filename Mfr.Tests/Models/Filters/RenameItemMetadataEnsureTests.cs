using Mfr.Filters;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Soft-error rethrow behavior shared by all Ensure* metadata buckets.
    /// </summary>
    public sealed class RenameItemMetadataEnsureTests
    {
        [Theory]
        [InlineData(RenameListMetadataRequirement.TagLib)]
        [InlineData(RenameListMetadataRequirement.ImageProperties)]
        [InlineData(RenameListMetadataRequirement.Pdf)]
        [InlineData(RenameListMetadataRequirement.Epub)]
        [InlineData(RenameListMetadataRequirement.Office)]
        [InlineData(RenameListMetadataRequirement.GeoNames)]
        public void Ensure_rethrows_stored_soft_load_error(RenameListMetadataRequirement bucket)
        {
            var item = FilterTestHelpers.CreateRenameItem();
            _ClearBucket(item, bucket);
            item.MarkMetadataLoadAttempted(bucket);
            item.SetMetadataLoadError(bucket, new InvalidOperationException("soft fail for " + bucket));

            var ex = Assert.Throws<InvalidOperationException>(() => _Ensure(item, bucket));
            Assert.Equal("soft fail for " + bucket, ex.Message);
        }

        [Theory]
        [InlineData(RenameListMetadataRequirement.TagLib)]
        [InlineData(RenameListMetadataRequirement.ImageProperties)]
        [InlineData(RenameListMetadataRequirement.Pdf)]
        [InlineData(RenameListMetadataRequirement.Epub)]
        [InlineData(RenameListMetadataRequirement.Office)]
        [InlineData(RenameListMetadataRequirement.GeoNames)]
        public void Ensure_second_call_after_failure_rethrows_same_error(RenameListMetadataRequirement bucket)
        {
            var item = _UnmarkedMissingFileItem();
            var first = Assert.ThrowsAny<Exception>(() => _Ensure(item, bucket));
            Assert.True(item.WasMetadataLoadAttempted(bucket));
            Assert.Same(first, item.GetMetadataLoadError(bucket));

            var second = Assert.ThrowsAny<Exception>(() => _Ensure(item, bucket));
            Assert.Same(first, second);
        }

        private static void _Ensure(RenameItem item, RenameListMetadataRequirement bucket)
        {
            switch (bucket)
            {
                case RenameListMetadataRequirement.TagLib:
                    item.EnsureTagLibLoaded();
                    break;
                case RenameListMetadataRequirement.ImageProperties:
                    item.EnsureImagePropertiesLoaded();
                    break;
                case RenameListMetadataRequirement.Pdf:
                    item.EnsurePdfLoaded();
                    break;
                case RenameListMetadataRequirement.Epub:
                    item.EnsureEpubLoaded();
                    break;
                case RenameListMetadataRequirement.Office:
                    item.EnsureOfficeLoaded();
                    break;
                case RenameListMetadataRequirement.GeoNames:
                    item.EnsureGeoNamesLoaded();
                    break;
                case RenameListMetadataRequirement.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null);
            }
        }

        private static void _ClearBucket(RenameItem item, RenameListMetadataRequirement bucket)
        {
            switch (bucket)
            {
                case RenameListMetadataRequirement.TagLib:
                    item.ClearEmbeddedTagsCache();
                    item.ClearMediaPropertiesCache();
                    break;
                case RenameListMetadataRequirement.ImageProperties:
                    item.ClearImagePropertiesCache();
                    break;
                case RenameListMetadataRequirement.Pdf:
                    item.ClearPdfCache();
                    break;
                case RenameListMetadataRequirement.Epub:
                    item.ClearEpubCache();
                    break;
                case RenameListMetadataRequirement.Office:
                    item.ClearOfficeCache();
                    break;
                case RenameListMetadataRequirement.GeoNames:
                    item.ClearGeoNamesCache();
                    break;
                case RenameListMetadataRequirement.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null);
            }
        }

        private static RenameItem _UnmarkedMissingFileItem()
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("Missing"),
                fileName: "gone",
                extension: "jpg",
                attributes: FileAttributes.Normal,
                creationTime: DateTime.UnixEpoch,
                lastWriteTime: DateTime.UnixEpoch,
                lastAccessTime: DateTime.UnixEpoch,
                fileSize: 0,
                renameListTotalCount: 1,
                renameListFolderSiblingCount: 1
            );
            return new RenameItem(meta);
        }
    }
}

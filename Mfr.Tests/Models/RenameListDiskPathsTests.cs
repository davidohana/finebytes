using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Snapshot missing-from-disk flag for Rename List rows.
    /// </summary>
    public sealed class RenameListDiskPathsTests
    {
        /// <summary>
        /// Verifies a newly constructed item is not missing until add/refresh says so.
        /// </summary>
        [Fact]
        public void IsMissingFromDisk_false_by_default()
        {
            var item = new RenameItem(_FileMeta("present.txt"));

            Assert.False(RenameListDiskPaths.IsMissingFromDisk(item));
        }

        /// <summary>
        /// Verifies catalog missing-state follows the snapshot flag, not a live path check.
        /// </summary>
        [Fact]
        public void IsMissingFromDisk_reads_snapshot_flag()
        {
            var item = new RenameItem(_FileMeta("gone.txt"));
            item.SetMissingFromDisk(true);

            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));
        }

        /// <summary>
        /// Verifies missing rows sort by field values and stay in normal order (not forced last).
        /// </summary>
        [Fact]
        public void CompareForSort_missing_rows_sort_by_field_not_forced_last()
        {
            var present = new RenameItem(_FileMeta("zzz.txt"));
            var missing = new RenameItem(_FileMeta("aaa.txt"));
            missing.SetMissingFromDisk(true);
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.True(RenameListFieldCatalog.CompareForSort(missing, key, present) < 0);
            Assert.True(RenameListFieldCatalog.CompareForSort(present, key, missing) > 0);
        }

        /// <summary>
        /// Verifies Show Error Details includes missing rows.
        /// </summary>
        [Fact]
        public void ListLoadErrors_includes_missing_from_disk()
        {
            var item = new RenameItem(_FileMeta("gone.txt"));
            item.SetMissingFromDisk(true);

            var error = Assert.Single(RenameListFieldCatalog.ListLoadErrors(item));

            Assert.Equal(RenameListDiskPaths.MissingUserExplanation, error.UserExplanation);
            Assert.Equal(item.Original.FullPath, error.TechnicalDetails);
            Assert.True(RenameListFieldCatalog.HasAnyLoadError(item));
        }

        /// <summary>
        /// Verifies missing rows do not use per-cell load-error styling.
        /// </summary>
        [Fact]
        public void HasLoadError_false_when_missing_even_with_stored_exception()
        {
            var item = new RenameItem(_FileMeta("tagged.wav"));
            item.SetTagLibMetadataLoadError(new IOException("failed"));
            item.SetMissingFromDisk(true);

            var titleKey = RenameListFieldKey.Original("AudioTag", "Title");

            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));
            Assert.False(RenameListFieldCatalog.HasLoadError(item, titleKey));
        }

        private static FileMeta _FileMeta(string fileName)
        {
            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: @"D:\tmp",
                prefix: Path.GetFileNameWithoutExtension(fileName),
                extension: Path.GetExtension(fileName)
            );
        }
    }
}

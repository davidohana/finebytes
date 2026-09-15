using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.Rename;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for <see cref="RenameListEntry.ToEntry"/> and field-key row resolution.
    /// </summary>
    public sealed class RenameListEntryMapperTests
    {
        [Fact]
        public void ToEntry_GetFieldText_matches_catalog_and_convenience_properties()
        {
            var directory = TestPaths.Absolute("Photos", "2024");
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "vacation007",
                extension: "jpg",
                directory: directory
            );
            var entry = RenameListEntry.ToEntry(item);
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );

            Assert.Equal("vacation007.jpg", entry.GetFieldText(fullNameKey));
            Assert.Equal(entry.GetFieldText(fullNameKey), entry.FullFileName);
            Assert.Equal("File", entry.FileFolder);
            Assert.Equal(Path.Combine(directory, "vacation007.jpg"), entry.FullPath);
        }

        [Fact]
        public void ToEntry_preview_field_follows_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: "txt");
            item.Preview.Prefix = "after";

            var entry = RenameListEntry.ToEntry(item);

            Assert.Equal("before.txt", entry.FullFileName);
            Assert.Equal("after.txt", entry.FullFileNamePreview);
            Assert.Equal(
                "after.txt",
                entry.GetFieldText(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                )
            );
        }

        /// <summary>
        /// Verifies status-column error priority is commit, then preview, then load/missing.
        /// </summary>
        [Fact]
        public void HighestStatusError_prefers_commit_then_preview_then_load()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "row", extension: "txt");
            var entry = RenameListEntry.ToEntry(item);

            Assert.Equal(RenameListStatusErrorKind.None, entry.HighestStatusError);
            Assert.False(entry.HasStatusError);

            item.PreviewError = new RenameItemError("preview failed");
            Assert.Equal(RenameListStatusErrorKind.Preview, entry.HighestStatusError);
            Assert.True(entry.HasStatusError);

            item.CommitError = new RenameItemError("commit failed");
            Assert.Equal(RenameListStatusErrorKind.Commit, entry.HighestStatusError);

            item.CommitError = null;
            item.PreviewError = null;
            item.SetMissingFromDisk(true);
            Assert.True(entry.HasLoadError);
            Assert.Equal(RenameListStatusErrorKind.LoadOrMissing, entry.HighestStatusError);
        }
    }
}

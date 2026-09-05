using Mfr.App.Ui.ViewModels.RenameList;
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
                extension: ".jpg",
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
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
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
    }
}

using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests for <see cref="RenameListEntry.ToEntry"/> and field-key row resolution.
    /// </summary>
    public sealed class RenameListEntryMapperTests
    {
        [Fact]
        public void ToEntry_resolves_default_visible_fields_for_file_row()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "vacation007",
                extension: ".jpg",
                directory: @"D:\Photos\2024"
            );
            var entry = RenameListEntry.ToEntry(item);

            Assert.Equal("File", entry.FileFolder);
            Assert.Equal(@"D:\Photos\2024", entry.ParentFolder);
            Assert.Equal("vacation007.jpg", entry.FullFileName);
            Assert.Equal("vacation007.jpg", entry.FullFileNamePreview);
            Assert.Equal(@"D:\Photos\2024\vacation007.jpg", entry.FullPath);

            Assert.Equal(
                "File",
                entry.GetFieldText(RenameListFieldKey.Original(BasicRenameListField.Group, BasicItemTypeField.Key))
            );
            Assert.Equal(
                "vacation007.jpg",
                entry.GetFieldText(RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key))
            );
        }

        [Fact]
        public void ToEntry_resolves_default_visible_fields_for_folder_row()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "Album",
                extension: "",
                directory: @"D:\Music",
                attributes: FileAttributes.Directory
            );
            var entry = RenameListEntry.ToEntry(item);

            Assert.Equal("Folder", entry.FileFolder);
            Assert.Equal(@"D:\Music", entry.ParentFolder);
            Assert.Equal("Album", entry.FullFileName);
            Assert.Equal("Album", entry.FullFileNamePreview);
        }

        [Fact]
        public void ToEntry_preview_field_follows_preview_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "before", extension: ".txt");
            item.Preview.Prefix = "after";

            var entry = RenameListEntry.ToEntry(item);

            Assert.Equal("before.txt", entry.FullFileName);
            Assert.Equal("after.txt", entry.FullFileNamePreview);
        }
    }
}

using Avalonia.Media;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.Rename;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        [Fact]
        public void FormatFieldError_Uses_Plain_Language_Explanation()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            try
            {
                var path = Path.Combine(dir, "PLAYLIST.M3U");
                File.WriteAllText(path, "#EXTM3U\n");
                var item = new RenameItem(
                    new FileMeta(
                        renameListIndex: 0,
                        inFolderIndex: 0,
                        directoryPath: dir,
                        prefix: "PLAYLIST",
                        extension: ".M3U",
                        fileSize: new FileInfo(path).Length
                    )
                );
                item.SetTagLibMetadataLoadError(new InvalidOperationException($"{path} (taglib/m3u)"));
                var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
                var explanation = RenameListFieldCatalog.DescribeFieldLoadError(item, titleKey);

                var hint = RenameListCellHint.FormatFieldError("Album Artists", explanation);
                Assert.Equal(2, hint.Runs.Count);
                Assert.Equal("Album Artists", hint.Runs[0].Text);
                Assert.Contains("[Field value error]", hint.Runs[1].Text, StringComparison.Ordinal);
                Assert.Contains("audio or media metadata", hint.Runs[1].Text, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies hints use bold column name, colon, then value.
        /// </summary>
        [Fact]
        public void FormatParts_Uses_Bold_Column_Name()
        {
            var hint = RenameListCellHint.FormatParts("Full File Name", "alpha.txt");
            Assert.Equal(2, hint.Runs.Count);
            Assert.Equal("Full File Name", hint.Runs[0].Text);
            Assert.Equal(FontWeight.Bold, hint.Runs[0].FontWeight);
            Assert.Equal(": alpha.txt", hint.Runs[1].Text);
            Assert.Equal("Full File Name: alpha.txt", hint.ToPlainText());
        }
    }
}

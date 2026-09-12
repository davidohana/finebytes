using Avalonia.Media;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Rename List status-bar cell hint formatting.
    /// </summary>
    public sealed class RenameListCellHintTests
    {
        [Fact]
        public void FormatLoadError_Uses_Plain_Language_Explanation()
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
                        extension: "M3U",
                        fileSize: new FileInfo(path).Length
                    )
                );
                item.SetTagLibMetadataLoadError(new InvalidOperationException($"{path} (taglib/m3u)"));
                var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
                var explanation = RenameListFieldCatalog.DescribeLoadError(item, titleKey);

                var hint = RenameListCellHint.FormatLoadError("Album Artist", explanation);
                Assert.Equal(3, hint.Runs.Count);
                Assert.Equal("Album Artist", hint.Runs[0].Text);
                Assert.Null(hint.Runs[0].ForegroundResourceKey);
                Assert.Equal(": ", hint.Runs[1].Text);
                Assert.Null(hint.Runs[1].ForegroundResourceKey);
                Assert.Contains("Could not read metadata:", hint.Runs[2].Text, StringComparison.Ordinal);
                Assert.Contains("audio or media metadata", hint.Runs[2].Text, StringComparison.Ordinal);
                Assert.Equal(StatusBarText.ErrorForegroundResourceKey, hint.Runs[2].ForegroundResourceKey);
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

        /// <summary>
        /// Verifies preview-error hints include the MFR7 item-preview-error marker in the error brush.
        /// </summary>
        [Fact]
        public void FormatPreviewError_Uses_Error_Brush_For_Marker()
        {
            var hint = RenameListCellHint.FormatPreviewError("Full File Name", "alpha.txt");
            Assert.Equal(4, hint.Runs.Count);
            Assert.Equal("Full File Name", hint.Runs[0].Text);
            Assert.Equal(FontWeight.Bold, hint.Runs[0].FontWeight);
            Assert.Null(hint.Runs[0].ForegroundResourceKey);
            Assert.Equal(": ", hint.Runs[1].Text);
            Assert.Equal(RenameListCellHint.PreviewErrorMarker, hint.Runs[2].Text);
            Assert.Equal(StatusBarText.ErrorForegroundResourceKey, hint.Runs[2].ForegroundResourceKey);
            Assert.Equal(" alpha.txt", hint.Runs[3].Text);
            Assert.Equal($"Full File Name: {RenameListCellHint.PreviewErrorMarker} alpha.txt", hint.ToPlainText());
        }
    }
}

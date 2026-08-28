using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui.FileList
{
    /// <summary>
    /// Tests Explorer-style type, size, and tile-detail text for File List rows.
    /// </summary>
    public sealed class FileListEntryDisplayTests
    {
        /// <summary>
        /// Verifies folders, Network, and extensions produce Explorer-style type labels.
        /// </summary>
        [Fact]
        public void TypeLabel_Matches_Explorer_Style()
        {
            Assert.Equal("File folder", FileListEntryDisplay.TypeLabel(_Folder(@"C:\Music")));
            Assert.Equal("TXT File", FileListEntryDisplay.TypeLabel(_File(@"C:\a.txt", "a.txt")));
            Assert.Equal("File", FileListEntryDisplay.TypeLabel(_File(@"C:\README", "README")));
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(
                    "Network location",
                    FileListEntryDisplay.TypeLabel(
                        new FileListListedItem(
                            FileListPath.NetworkPath,
                            FileListPath.NetworkDisplayName,
                            IsDirectory: true,
                            Length: null,
                            LastWriteTime: null
                        )
                    )
                );
            }
        }

        /// <summary>
        /// Verifies size labels use KB/MB/GB above 1024 bytes.
        /// </summary>
        [Fact]
        public void FormatSize_Uses_Binary_Units()
        {
            Assert.Equal("512 B", FileListEntryDisplay.FormatSize(512));
            Assert.Equal("1 KB", FileListEntryDisplay.FormatSize(1024));
            Assert.Equal("1 MB", FileListEntryDisplay.FormatSize(1024 * 1024));
        }

        /// <summary>
        /// Verifies Tiles details are type-only for folders and type plus size for files.
        /// </summary>
        [Fact]
        public void FormatDetails_Adds_Size_For_Files()
        {
            Assert.Equal("File folder", FileListEntryDisplay.FormatDetails(_Folder(@"C:\Music")));
            Assert.Equal("TXT File\n3 B", FileListEntryDisplay.FormatDetails(_File(@"C:\a.txt", "a.txt", length: 3)));
        }

        private static FileListListedItem _Folder(string path)
        {
            return new FileListListedItem(
                path,
                Path.GetFileName(path),
                IsDirectory: true,
                Length: null,
                LastWriteTime: null
            );
        }

        private static FileListListedItem _File(string path, string name, long? length = 1)
        {
            return new FileListListedItem(path, name, IsDirectory: false, Length: length, LastWriteTime: null);
        }
    }
}

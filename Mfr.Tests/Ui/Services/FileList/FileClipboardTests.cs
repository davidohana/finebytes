using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui.Services.FileList
{
    /// <summary>
    /// Tests factory defaults and recording fake call shapes for <see cref="IFileClipboard"/>.
    /// </summary>
    public sealed class FileClipboardTests
    {
        /// <summary>
        /// Verifies <see cref="FileClipboard.CreateDefault"/> matches the host OS.
        /// </summary>
        [Fact]
        public void CreateDefault_Matches_Host_Os()
        {
            var clipboard = FileClipboard.CreateDefault();

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(nameof(WindowsFileClipboard), clipboard.GetType().Name);
                return;
            }

            Assert.IsType<NullFileClipboard>(clipboard);
        }

        /// <summary>
        /// Verifies the recording fake captures Copy paths and clears cut marks.
        /// </summary>
        [Fact]
        public void Recording_Fake_SetCopy_Clears_Cut_Marks()
        {
            var clipboard = new RecordingFileClipboard();
            var paths = new[] { TestPaths.Absolute("a.txt"), TestPaths.Absolute("b") };

            clipboard.SetCut(paths);
            Assert.Contains(TestPaths.Absolute("a.txt"), clipboard.CutPaths);
            Assert.Contains(TestPaths.Absolute("b"), clipboard.CutPaths);

            clipboard.SetCopy([TestPaths.Absolute("c.txt")]);

            Assert.Empty(clipboard.CutPaths);
            Assert.Equal(paths, Assert.Single(clipboard.Cuts));
            Assert.Equal([TestPaths.Absolute("c.txt")], Assert.Single(clipboard.Copies));
            Assert.True(clipboard.TryGetPaste(out var paste));
            Assert.False(paste.PreferMove);
            Assert.Equal([TestPaths.Absolute("c.txt")], paste.Paths);
        }

        /// <summary>
        /// Verifies the recording fake captures Cut paths and PreferMove.
        /// </summary>
        [Fact]
        public void Recording_Fake_SetCut_Tracks_Cut_Paths()
        {
            var clipboard = new RecordingFileClipboard();
            var paths = new[] { TestPaths.Absolute("cut.txt") };

            clipboard.SetCut(paths);

            Assert.Equal(paths, Assert.Single(clipboard.Cuts));
            Assert.Contains(TestPaths.Absolute("cut.txt"), clipboard.CutPaths);
            Assert.True(clipboard.TryGetPaste(out var paste));
            Assert.True(paste.PreferMove);
            Assert.Equal(paths, paste.Paths);
        }

        /// <summary>
        /// Verifies CompleteMovePaste clears cut marks and PreferMove paste payloads.
        /// </summary>
        [Fact]
        public void Recording_Fake_CompleteMovePaste_Clears_Cut_Payload()
        {
            var clipboard = new RecordingFileClipboard();
            clipboard.SetCut([TestPaths.Absolute("cut.txt")]);
            Assert.True(clipboard.HasPasteableFiles);
            Assert.NotEmpty(clipboard.CutPaths);

            clipboard.CompleteMovePaste();

            Assert.Empty(clipboard.CutPaths);
            Assert.False(clipboard.HasPasteableFiles);
            Assert.False(clipboard.TryGetPaste(out _));
        }

        /// <summary>
        /// Verifies Windows SetCopy / SetCut round-trip CF_HDROP + Preferred DropEffect.
        /// </summary>
        [WindowsFact]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void Windows_SetCopy_And_SetCut_RoundTrip_Clipboard()
        {
            var clipboard = new WindowsFileClipboard();
            var dir = Path.Combine(Path.GetTempPath(), "mfr-file-clipboard-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var alpha = Path.Combine(dir, "alpha.txt");
                var beta = Path.Combine(dir, "beta.txt");
                File.WriteAllText(alpha, "a");
                File.WriteAllText(beta, "b");

                clipboard.SetCopy([alpha, beta]);
                Assert.Empty(clipboard.CutPaths);
                Assert.True(clipboard.TryGetPaste(out var copyPaste));
                Assert.False(copyPaste.PreferMove);
                Assert.Equal(2, copyPaste.Paths.Count);
                Assert.Contains(alpha, copyPaste.Paths, StringComparer.OrdinalIgnoreCase);
                Assert.Contains(beta, copyPaste.Paths, StringComparer.OrdinalIgnoreCase);

                clipboard.SetCut([alpha]);
                Assert.Contains(alpha, clipboard.CutPaths);
                Assert.True(clipboard.TryGetPaste(out var cutPaste));
                Assert.True(cutPaste.PreferMove);
                Assert.Single(cutPaste.Paths);
                Assert.Equal(alpha, cutPaste.Paths[0], StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException)
                {
                    // Best-effort cleanup when Explorer still holds a handle.
                }
            }
        }
    }
}

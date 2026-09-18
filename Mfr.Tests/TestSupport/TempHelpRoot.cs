using Mfr.App.Ui.Services.Help;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Creates a temporary Help root with optional HTML files, a recording shell opener, and a
    /// <see cref="HelpHost"/> wired to that root; deletes the directory after the body returns.
    /// </summary>
    public static class TempHelpRoot
    {
        /// <summary>
        /// Runs <paramref name="body"/> against a fresh temp help directory.
        /// </summary>
        /// <param name="body">Test body receiving the help directory, opener, and host.</param>
        /// <param name="htmlRelativePaths">
        /// Optional relative paths under the root to write as minimal
        /// <c>&lt;html&gt;&lt;/html&gt;</c> files (e.g. <c>SpaceCharacter.html</c> or
        /// <c>filters/space/SpaceCharacter.html</c>).
        /// </param>
        public static void Run(
            Action<string, RecordingFileShellOpener, HelpHost> body,
            params string[] htmlRelativePaths
        )
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                foreach (var relativePath in htmlRelativePaths)
                {
                    var fullPath = Path.Combine(helpDir, relativePath);
                    var parent = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    File.WriteAllText(fullPath, "<html></html>");
                }

                var opener = new RecordingFileShellOpener();
                var host = new HelpHost(opener, [helpDir]);
                body(helpDir, opener, host);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }
    }
}

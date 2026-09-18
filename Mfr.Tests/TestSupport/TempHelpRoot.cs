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
        /// <param name="htmlBasenames">
        /// Optional basenames to write as minimal <c>&lt;html&gt;&lt;/html&gt;</c> files under the root.
        /// </param>
        public static void Run(Action<string, RecordingFileShellOpener, HelpHost> body, params string[] htmlBasenames)
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                foreach (var basename in htmlBasenames)
                {
                    File.WriteAllText(Path.Combine(helpDir, basename), "<html></html>");
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

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Creates and tracks temporary directories for tests, then deletes them when disposed.
    /// </summary>
    public sealed class TempDirectoryFixture : IDisposable
    {
        private readonly List<string> _directories = [];

        /// <summary>
        /// Creates a new unique temporary directory and tracks it for cleanup.
        /// </summary>
        /// <returns>The full path to the created temporary directory.</returns>
        public string CreateTempDir()
        {
            Console.WriteLine("Creating temp directory: " + Path.GetTempPath());
            var dir = Path.Combine(Path.GetTempPath(), "mfr8_tests_" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(dir);
            _directories.Add(dir);
            return dir;
        }

        /// <summary>
        /// Deletes all tracked temporary directories.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Disposing of temp directories: " + string.Join(", ", _directories));
            foreach (var dir in _directories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}

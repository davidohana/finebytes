using Mfr.Utils;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Creates one temporary directory for a test and deletes it when disposed.
    /// </summary>
    public sealed class TempDirectoryFixture : IDisposable
    {
        /// <summary>
        /// Gets the full path to the temporary directory for the current fixture instance.
        /// </summary>
        public string TempDir { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempDirectoryFixture"/> class.
        /// </summary>
        public TempDirectoryFixture()
        {
            TempDir = Path.GetTempPath().CombinePath("mfr8_tests_" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(TempDir);
        }

        /// <summary>
        /// Gets the fixture's temporary directory path.
        /// </summary>
        /// <returns>The full path to the temporary directory.</returns>
        public string CreateTempDir()
        {
            return TempDir;
        }

        /// <summary>
        /// Deletes the temporary directory created by this fixture.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TempDir))
                {
                    Directory.Delete(TempDir, recursive: true);
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

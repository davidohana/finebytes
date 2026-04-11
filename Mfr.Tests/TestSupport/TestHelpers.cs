namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Provides file-system helper utilities for tests.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Creates a single file under a root directory using a slash-separated relative path.
        /// </summary>
        /// <param name="rootDirectory">Root directory where files are created.</param>
        /// <param name="relativePath">Relative file path using '/' or '\'.</param>
        /// <returns>Absolute path of the created file.</returns>
        internal static string CreateFile(string rootDirectory, string relativePath)
        {
            return _CreateFiles(rootDirectory, relativePath)[0];
        }

        /// <summary>
        /// Creates two files under a root directory and returns absolute paths as a tuple.
        /// </summary>
        /// <param name="rootDirectory">Root directory where files are created.</param>
        /// <param name="firstRelativePath">First relative file path using '/' or '\'.</param>
        /// <param name="secondRelativePath">Second relative file path using '/' or '\'.</param>
        /// <returns>Absolute paths of the created files.</returns>
        internal static (string First, string Second) CreateFiles(
            string rootDirectory,
            string firstRelativePath,
            string secondRelativePath)
        {
            var createdPaths = _CreateFiles(rootDirectory, firstRelativePath, secondRelativePath);
            return (createdPaths[0], createdPaths[1]);
        }

        /// <summary>
        /// Creates three files under a root directory and returns absolute paths as a tuple.
        /// </summary>
        /// <param name="rootDirectory">Root directory where files are created.</param>
        /// <param name="firstRelativePath">First relative file path using '/' or '\'.</param>
        /// <param name="secondRelativePath">Second relative file path using '/' or '\'.</param>
        /// <param name="thirdRelativePath">Third relative file path using '/' or '\'.</param>
        /// <returns>Absolute paths of the created files.</returns>
        internal static (string First, string Second, string Third) CreateFiles(
            string rootDirectory,
            string firstRelativePath,
            string secondRelativePath,
            string thirdRelativePath)
        {
            var createdPaths = _CreateFiles(rootDirectory, firstRelativePath, secondRelativePath, thirdRelativePath);
            return (createdPaths[0], createdPaths[1], createdPaths[2]);
        }

        private static List<string> _CreateFiles(string rootDirectory, params string[] relativePaths)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory cannot be empty.", nameof(rootDirectory));
            }

            var createdPaths = new List<string>(relativePaths.Length);
            foreach (var relativePath in relativePaths)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new ArgumentException("Relative file path cannot be empty.", nameof(relativePaths));
                }

                var normalizedRelativePath = relativePath
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, normalizedRelativePath));
                var parentDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                File.WriteAllText(fullPath, "dummy");
                createdPaths.Add(fullPath);
            }

            return createdPaths;
        }
    }
}

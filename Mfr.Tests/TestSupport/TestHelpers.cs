namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Provides file-system helper utilities for tests.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Creates multiple files under a root directory using slash-separated relative paths.
        /// </summary>
        /// <param name="rootDirectory">Root directory where files are created.</param>
        /// <param name="relativePaths">Relative file paths using '/' or '\'.</param>
        /// <returns>Absolute paths of files created, in the same order.</returns>
        internal static IReadOnlyList<string> CreateFiles(string rootDirectory, params string[] relativePaths)
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
                    _ = Directory.CreateDirectory(parentDirectory);
                }

                File.WriteAllText(fullPath, "dummy");
                createdPaths.Add(fullPath);
            }

            return createdPaths;
        }
    }
}

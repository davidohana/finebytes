using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Mfr.Core
{
    /// <summary>
    /// Resolves user-provided rename sources (files, directories, and wildcard patterns) into concrete paths.
    /// </summary>
    public static class RenameSourceResolver
    {
        /// <summary>
        /// Resolves a single source into file paths.
        /// </summary>
        /// <param name="source">The source to resolve.</param>
        /// <param name="includeFolders">Whether folder entries should be included from resolved paths.</param>
        /// <param name="recursiveDirectoryFileAdd">Whether directory-source file expansion should include subdirectories when folders are excluded.</param>
        /// <returns>Resolved file paths for the source.</returns>
        public static IEnumerable<string> Resolve(
            string source,
            bool includeFolders,
            bool recursiveDirectoryFileAdd)
        {
            var fullSource = Path.GetFullPath(source);
            if (Directory.Exists(fullSource))
            {
                if (includeFolders)
                {
                    // Directory sources resolve to the directory path itself when folders are included.
                    return [fullSource];
                }

                // When folder entries are excluded, directory sources expand to files based on recursion mode.
                var searchOption = recursiveDirectoryFileAdd
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;
                return Directory.EnumerateFiles(fullSource, "*", searchOption);
            }

            // Non-directory sources resolve relative to their parent directory.
            var parentDirectory = Path.GetDirectoryName(fullSource);
            parentDirectory = string.IsNullOrWhiteSpace(parentDirectory) ? Directory.GetCurrentDirectory() : parentDirectory;
            if (_TryResolveGlob(fullSource, out var globMatches))
            {
                return globMatches;
            }

            if (!Directory.Exists(parentDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{parentDirectory}'.");
            }

            var filePattern = Path.GetFileName(fullSource);
            if (_ContainsGlobPattern(filePattern))
            {
                // Simple file-name wildcards expand in the parent directory only.
                return Directory.EnumerateFiles(parentDirectory, filePattern, SearchOption.TopDirectoryOnly);
            }

            if (File.Exists(fullSource))
            {
                // Exact file source resolves to that single file.
                return [fullSource];
            }

            // Missing exact files are ignored (no resolved items).
            return [];
        }

        /// <summary>
        /// Tries to resolve glob sources using standard matcher syntax.
        /// </summary>
        /// <param name="fullSource">The full source path to inspect.</param>
        /// <param name="resolvedPaths">Resolved file paths when glob syntax is recognized.</param>
        /// <returns><c>true</c> if glob syntax was detected; otherwise <c>false</c>.</returns>
        private static bool _TryResolveGlob(string fullSource, out IEnumerable<string> resolvedPaths)
        {
            resolvedPaths = [];
            if (!_ContainsGlobPattern(fullSource))
            {
                return false;
            }

            var normalizedSource = fullSource.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var root = Path.GetPathRoot(normalizedSource) ?? Directory.GetCurrentDirectory();
            var relativePath = normalizedSource[root.Length..];
            var segments = relativePath.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);

            var baseSegments = new List<string>();
            var globSegments = new List<string>();
            var foundGlob = false;
            foreach (var segment in segments)
            {
                if (!foundGlob && !_ContainsGlobPattern(segment))
                {
                    baseSegments.Add(segment);
                    continue;
                }

                foundGlob = true;
                globSegments.Add(segment);
            }

            if (!foundGlob)
            {
                return false;
            }

            if (globSegments.Count == 1)
            {
                // Single-segment patterns are handled by Directory.EnumerateFiles in the caller.
                return false;
            }

            var baseDirectory = root;
            foreach (var segment in baseSegments)
            {
                baseDirectory = Path.Combine(baseDirectory, segment);
            }

            if (!Directory.Exists(baseDirectory))
            {
                throw new UserException($"Directory for source does not exist: '{baseDirectory}'.");
            }

            var includePattern = globSegments.Count == 0 ? "*" : string.Join('/', globSegments);
            var matcher = new Matcher(
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
            matcher.AddInclude(includePattern);
            var matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory)));

            resolvedPaths = matchResult.Files
                .Select(match => Path.GetFullPath(
                    Path.Combine(baseDirectory, match.Path.Replace('/', Path.DirectorySeparatorChar))));
            return true;
        }

        /// <summary>
        /// Determines whether the value contains wildcard characters.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns><c>true</c> when wildcard characters are present; otherwise <c>false</c>.</returns>
        private static bool _ContainsGlobPattern(string value)
        {
            return value.Contains('*') || value.Contains('?');
        }
    }
}

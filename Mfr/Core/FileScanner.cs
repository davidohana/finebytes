using Mfr.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mfr.Core
{
    /// <summary>
    /// Scans source paths and expands them into normalized file entries for renaming.
    /// </summary>
    public static class FileScanner
    {
        /// <summary>
        /// Scans the provided <paramref name="sources"/> (files, folders, and top-level wildcards)
        /// and returns a distinct, ordered list of file entries.
        /// </summary>
        /// <param name="sources">Paths to files/folders or wildcard expressions.</param>
        /// <param name="includeHidden">If <c>true</c>, includes hidden/system files.</param>
        /// <returns>A list of files represented as <see cref="FileEntryLite"/>.</returns>
        public static IReadOnlyList<FileEntryLite> ScanSources(
            IEnumerable<string> sources,
            bool includeHidden)
        {
            var results = new List<string>();
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            var cwd = Path.GetFullPath(Directory.GetCurrentDirectory());

            static string ToMatcherPath(string path)
            {
                return path.Replace('\\', '/');
            }

            string ToRelativeMatcherPath(string path)
            {
                var relativePath = Path.GetRelativePath(cwd, path);
                return ToMatcherPath(relativePath);
            }

            foreach (var rawSrc in sources)
            {
                Console.WriteLine($"Raw source: {rawSrc}");
                var src = rawSrc.Trim();
                if (src.Length == 0)
                {
                    continue;
                }

                var fullSrc = Path.GetFullPath(src);
                if (Directory.Exists(fullSrc))
                {
                    var relativeDirectoryPath = ToRelativeMatcherPath(fullSrc);
                    var includePattern = relativeDirectoryPath == "." ? "*" : $"{relativeDirectoryPath}/*";
                    _ = matcher.AddInclude(includePattern);
                    continue;
                }

                var parentDir = Path.GetDirectoryName(fullSrc);
                parentDir = string.IsNullOrWhiteSpace(parentDir) ? Directory.GetCurrentDirectory() : parentDir;
                if (!Directory.Exists(parentDir))
                {
                    throw new UserException($"Directory for source does not exist: '{parentDir}'.");
                }

                _ = matcher.AddInclude(ToRelativeMatcherPath(fullSrc));
            }

            results.AddRange(matcher.GetResultsInFullPath(cwd));

            results = [.. results.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
            return ToFileEntryLiteList(results, includeHidden);
        }

        /// <summary>
        /// Converts full file paths into normalized <see cref="FileEntryLite"/> entries.
        /// </summary>
        /// <param name="results">Ordered file paths to convert.</param>
        /// <param name="includeHidden">If <c>true</c>, includes hidden/system files.</param>
        /// <returns>The converted file entry list.</returns>
        public static List<FileEntryLite> ToFileEntryLiteList(
            List<string> results,
            bool includeHidden)
        {
            var folderCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<FileEntryLite>(results.Count);

            for (var i = 0; i < results.Count; i++)
            {
                var fullPath = results[i];
                var attrs = File.GetAttributes(fullPath);
                if (!includeHidden &&
                    (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System)))
                {
                    continue;
                }

                var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
                var prefix = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath); // includes leading '.'

                var folderOccurrence = folderCounts.GetValueOrDefault(directoryPath);
                folderCounts[directoryPath] = folderOccurrence + 1;

                entries.Add(new FileEntryLite(
                    GlobalIndex: i,
                    FolderOccurrenceIndex: folderOccurrence,
                    FullPath: fullPath,
                    DirectoryPath: directoryPath,
                    Prefix: prefix,
                    Extension: extension));
            }

            return entries;
        }
    }

}

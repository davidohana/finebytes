namespace Mfr8.Core
{
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
            IEnumerable<String> sources,
            Boolean includeHidden)
        {
            var results = new List<String>();

            foreach (String raw in sources)
            {
                String src = raw.Trim();
                if (src.Length == 0)
                {
                    continue;
                }

                if (src.Contains('*') || src.Contains('?'))
                {
                    String? dir = Path.GetDirectoryName(src);
                    String pattern = Path.GetFileName(src);
                    dir = String.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir!;

                    if (!Directory.Exists(dir))
                    {
                        throw new DirectoryNotFoundException($"Directory for wildcard does not exist: '{dir}'.");
                    }

                    results.AddRange(Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly));
                    continue;
                }

                if (Directory.Exists(src))
                {
                    results.AddRange(Directory.EnumerateFiles(src, "*", SearchOption.TopDirectoryOnly));
                    continue;
                }

                if (File.Exists(src))
                {
                    results.Add(src);
                    continue;
                }

                throw new FileNotFoundException($"Source not found: '{src}'.");
            }

            results = [.. results.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];

            var folderCounts = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<FileEntryLite>(results.Count);

            for (Int32 i = 0; i < results.Count; i++)
            {
                String fullPath = results[i];
                FileAttributes attrs = File.GetAttributes(fullPath);
                if (!includeHidden)
                {
                    if (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System))
                    {
                        continue;
                    }
                }

                String directoryPath = Path.GetDirectoryName(fullPath) ?? "";
                String prefix = Path.GetFileNameWithoutExtension(fullPath);
                String extension = Path.GetExtension(fullPath); // includes leading '.'

                String folderKey = directoryPath;
                _ = folderCounts.TryGetValue(folderKey, out Int32 folderOccurrence);
                folderCounts[folderKey] = folderOccurrence + 1;

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

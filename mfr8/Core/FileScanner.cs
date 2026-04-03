namespace Mfr8.Core;

public static class FileScanner
{
    public static IReadOnlyList<FileEntryLite> ScanSources(
        IEnumerable<string> sources,
        bool includeHidden)
    {
        var results = new List<string>();

        foreach (var raw in sources)
        {
            var src = raw.Trim();
            if (src.Length == 0) continue;

            if (src.Contains('*') || src.Contains('?'))
            {
                var dir = Path.GetDirectoryName(src);
                var pattern = Path.GetFileName(src);
                dir = string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir!;

                if (!Directory.Exists(dir))
                    throw new DirectoryNotFoundException($"Directory for wildcard does not exist: '{dir}'.");

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

        results = results.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var folderCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<FileEntryLite>(results.Count);

        for (var i = 0; i < results.Count; i++)
        {
            var fullPath = results[i];
            var attrs = File.GetAttributes(fullPath);
            if (!includeHidden)
            {
                if (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System))
                    continue;
            }

            var directoryPath = Path.GetDirectoryName(fullPath) ?? "";
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath); // includes leading '.'

            var folderKey = directoryPath;
            folderCounts.TryGetValue(folderKey, out var folderOccurrence);
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


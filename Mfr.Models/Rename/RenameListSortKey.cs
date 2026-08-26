namespace Mfr.Models.Rename
{
    /// <summary>
    /// Rename List columns that can participate in Auto-Sort (original fields only).
    /// </summary>
    public enum RenameListSortColumn
    {
        /// <summary>
        /// File vs Folder (<c>File</c> / <c>Folder</c>).
        /// </summary>
        FileFolder = 0,

        /// <summary>
        /// Parent directory path.
        /// </summary>
        ParentFolder = 1,

        /// <summary>
        /// File name including extension.
        /// </summary>
        FullFileName = 2,

        /// <summary>
        /// Absolute full path (default secondary key; not a visible grid column yet).
        /// </summary>
        FullPath = 3,
    }

    /// <summary>
    /// One Auto-Sort key: column plus ascending/descending.
    /// </summary>
    /// <param name="Column">Column to compare.</param>
    /// <param name="Descending">When <see langword="true"/>, reverse that column's order.</param>
    public readonly record struct RenameListSortKey(RenameListSortColumn Column, bool Descending = false)
    {
        /// <summary>
        /// Default Auto-Sort keys: File/Folder then Full Path (MFR7). Empty session value disables Auto-Sort.
        /// </summary>
        public const string Default = "FileFolder,FullPath";

        /// <summary>
        /// Parses <paramref name="encoded"/> into sort keys.
        /// </summary>
        /// <param name="encoded">Comma-separated <c>Column</c> or <c>Column:desc</c> tokens; empty is off.</param>
        /// <returns>Sort keys in priority order; empty when Auto-Sort is off.</returns>
        public static IReadOnlyList<RenameListSortKey> Parse(string? encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return [];
            }

            var keys = new List<RenameListSortKey>();
            var parts = encoded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var descending = false;
                var name = part;
                var colon = part.IndexOf(':');
                if (colon >= 0)
                {
                    name = part[..colon].Trim();
                    var suffix = part[(colon + 1)..].Trim();
                    if (!suffix.Equals("desc", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    descending = true;
                }

                if (!Enum.TryParse(name, ignoreCase: true, out RenameListSortColumn column) || !Enum.IsDefined(column))
                {
                    continue;
                }

                keys.Add(new RenameListSortKey(column, descending));
            }

            return keys;
        }

        /// <summary>
        /// Formats <paramref name="keys"/> as a session string.
        /// </summary>
        /// <param name="keys">Sort keys; empty yields an empty string (Auto-Sort off).</param>
        /// <returns>Encoded session string.</returns>
        public static string Format(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            return string.Join(',', keys.Select(key => key.Descending ? $"{key.Column}:desc" : key.Column.ToString()));
        }
    }
}

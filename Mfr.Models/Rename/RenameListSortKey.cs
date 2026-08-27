using Mfr.Models.Config;

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
        public static IReadOnlyList<RenameListSortKey> DefaultKeys { get; } =
        [
            new RenameListSortKey(RenameListSortColumn.FileFolder),
            new RenameListSortKey(RenameListSortColumn.FullPath),
        ];

        /// <summary>
        /// Default Auto-Sort keys as session fields.
        /// </summary>
        public static IReadOnlyList<SessionStateRenameListSortField> DefaultSessionFields { get; } =
            ToSessionFields(DefaultKeys);

        /// <summary>
        /// Converts persisted session fields into sort keys.
        /// </summary>
        /// <param name="fields">Session fields in priority order.</param>
        /// <returns>Sort keys; empty when Auto-Sort is off.</returns>
        public static IReadOnlyList<RenameListSortKey> FromSessionFields(IReadOnlyList<SessionStateRenameListSortField> fields)
        {
            ArgumentNullException.ThrowIfNull(fields);
            if (fields.Count == 0)
            {
                return [];
            }

            return [.. fields.Select(field => new RenameListSortKey(field.Column, field.Descending))];
        }

        /// <summary>
        /// Converts sort keys into persisted session fields.
        /// </summary>
        /// <param name="keys">Sort keys in priority order.</param>
        /// <returns>Session fields; empty when Auto-Sort is off.</returns>
        public static List<SessionStateRenameListSortField> ToSessionFields(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            return [.. keys.Select(key => new SessionStateRenameListSortField(key.Column, key.Descending))];
        }
    }
}

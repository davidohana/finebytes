using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One selectable column in the sort editor dropdown.
    /// </summary>
    /// <param name="Column">Sort column.</param>
    public sealed record RenameListSortColumnOption(RenameListSortColumn Column)
    {
        /// <summary>
        /// Gets the user-visible column label.
        /// </summary>
        public string Label => RenameListSortDisplay.GetColumnLabel(Column);
    }

    /// <summary>
    /// Human-readable labels and tooltip text for Rename List Auto-Sort keys.
    /// </summary>
    public static class RenameListSortDisplay
    {
        /// <summary>
        /// Columns available in the sort editor dropdown (includes hidden Full Path).
        /// </summary>
        public static IReadOnlyList<RenameListSortColumn> EditorColumns { get; } =
        [
            RenameListSortColumn.FileFolder,
            RenameListSortColumn.ParentFolder,
            RenameListSortColumn.FullFileName,
            RenameListSortColumn.FullPath,
        ];

        /// <summary>
        /// Dropdown options for <see cref="EditorColumns"/>.
        /// </summary>
        public static IReadOnlyList<RenameListSortColumnOption> EditorColumnOptions { get; } =
        [.. EditorColumns.Select(column => new RenameListSortColumnOption(column))];

        /// <summary>
        /// Tooltip when Auto-Sort is off.
        /// </summary>
        public const string AutoSortOffSummary = "Auto-Sort off. Push to activate.";

        /// <summary>
        /// Gets the grid column label for a sort column.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <returns>User-visible column name.</returns>
        public static string GetColumnLabel(RenameListSortColumn column)
        {
            return column switch
            {
                RenameListSortColumn.FileFolder => "File/Folder",
                RenameListSortColumn.ParentFolder => "Parent Folder",
                RenameListSortColumn.FullFileName => "Full File Name",
                RenameListSortColumn.FullPath => "Full Path",
                _ => column.ToString(),
            };
        }

        /// <summary>
        /// Builds header glyph state from active sort keys, keyed by column.
        /// </summary>
        /// <param name="keys">Active sort keys in priority order.</param>
        /// <returns>Lookup of priority and direction for each key's column.</returns>
        public static RenameListColumnSortStates BuildColumnSortStates(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var columnToState = new Dictionary<RenameListSortColumn, RenameListColumnSortState>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                columnToState.TryAdd(key.Column, new RenameListColumnSortState(i + 1, key.Descending));
            }

            return new RenameListColumnSortStates(columnToState);
        }

        /// <summary>
        /// Formats active sort keys for the Auto-Sort tooltip, or the off message when empty.
        /// </summary>
        /// <param name="keys">Active sort keys in priority order.</param>
        /// <returns>Single- or multi-line tooltip text.</returns>
        public static string FormatSummary(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            if (keys.Count == 0)
            {
                return AutoSortOffSummary;
            }

            var lines = new string[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var arrow = key.Descending ? "↓" : "↑";
                lines[i] = $"{i + 1}. {GetColumnLabel(key.Column)} {arrow}";
            }

            return string.Join('\n', lines);
        }
    }
}

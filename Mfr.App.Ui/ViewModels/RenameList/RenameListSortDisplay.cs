using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Human-readable labels and tooltip text for Rename List Auto-Sort keys.
    /// </summary>
    public static class RenameListSortDisplay
    {
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
        /// Builds header glyph state for visible Rename List columns from active sort keys.
        /// </summary>
        /// <param name="keys">Active sort keys in priority order.</param>
        /// <returns>Per-column priority and direction for header templates.</returns>
        public static RenameListColumnSortStates BuildColumnSortStates(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            return new RenameListColumnSortStates(
                _FindColumnSortState(keys, RenameListSortColumn.FileFolder),
                _FindColumnSortState(keys, RenameListSortColumn.ParentFolder),
                _FindColumnSortState(keys, RenameListSortColumn.FullFileName)
            );
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

        private static RenameListColumnSortState _FindColumnSortState(
            IReadOnlyList<RenameListSortKey> keys,
            RenameListSortColumn column
        )
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key.Column == column)
                {
                    return new RenameListColumnSortState(i + 1, key.Descending);
                }
            }

            return default;
        }
    }
}

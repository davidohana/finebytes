using Mfr.Models.RenameList;

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
        /// Gets the catalog display name for a sort column.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <returns>User-visible field name, or the enum name when unmapped.</returns>
        public static string GetColumnLabel(RenameListSortColumn column)
        {
            if (!RenameListFieldCatalog.TryMapSortColumn(column, out var key))
            {
                return column.ToString();
            }

            return RenameListFieldCatalog.GetField(key).DisplayName;
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

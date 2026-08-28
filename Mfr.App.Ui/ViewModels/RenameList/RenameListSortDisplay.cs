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
        /// Gets the catalog display name for a sort field key.
        /// </summary>
        /// <param name="fieldKey">Original field key.</param>
        /// <returns>User-visible field name.</returns>
        public static string GetFieldLabel(RenameListFieldKey fieldKey)
        {
            return RenameListFieldCatalog.GetField(fieldKey).DisplayName;
        }

        /// <summary>
        /// Builds header glyph state from active sort keys, keyed by original field key.
        /// </summary>
        /// <param name="keys">Active sort keys in priority order.</param>
        /// <returns>Lookup of priority and direction for each key's field.</returns>
        public static RenameListColumnSortStates BuildColumnSortStates(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var fieldKeyToState = new Dictionary<RenameListFieldKey, RenameListColumnSortState>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                fieldKeyToState.TryAdd(key.FieldKey, new RenameListColumnSortState(i + 1, key.Descending));
            }

            return new RenameListColumnSortStates(fieldKeyToState);
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
                lines[i] = $"{i + 1}. {GetFieldLabel(key.FieldKey)} {arrow}";
            }

            return string.Join('\n', lines);
        }
    }
}

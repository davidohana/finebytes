using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Sort header state keyed by column. Columns absent from the sort list are inactive.
    /// </summary>
    public sealed class RenameListColumnSortStates
    {
        private readonly IReadOnlyDictionary<RenameListSortColumn, RenameListColumnSortState> _columnToState;

        /// <summary>
        /// Initializes a lookup from sort column to header glyph state.
        /// </summary>
        /// <param name="columnToState">Active columns and their glyph state.</param>
        public RenameListColumnSortStates(
            IReadOnlyDictionary<RenameListSortColumn, RenameListColumnSortState> columnToState
        )
        {
            ArgumentNullException.ThrowIfNull(columnToState);
            _columnToState = columnToState;
        }

        /// <summary>
        /// Empty lookup (Auto-Sort off).
        /// </summary>
        public static RenameListColumnSortStates Inactive { get; } =
            new(new Dictionary<RenameListSortColumn, RenameListColumnSortState>());

        /// <summary>
        /// Gets header glyph state for a sort column, or inactive when that column is not a sort key.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <returns>Priority and direction, or default when inactive.</returns>
        public RenameListColumnSortState this[RenameListSortColumn column] => _columnToState.GetValueOrDefault(column);

        /// <summary>
        /// Gets header glyph state by enum name for XAML indexer bindings (e.g. <c>ColumnSortStates[FileFolder]</c>).
        /// </summary>
        /// <param name="columnName"><see cref="RenameListSortColumn"/> member name.</param>
        /// <returns>Priority and direction, or default when the name is unknown or inactive.</returns>
        public RenameListColumnSortState this[string columnName]
        {
            get
            {
                if (!Enum.TryParse<RenameListSortColumn>(columnName, out var column))
                {
                    return default;
                }

                return this[column];
            }
        }
    }
}

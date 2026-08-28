using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Sort header state keyed by field. Fields absent from the sort list are inactive.
    /// </summary>
    public sealed class RenameListColumnSortStates
    {
        private readonly IReadOnlyDictionary<RenameListFieldKey, RenameListColumnSortState> _fieldKeyToState;

        /// <summary>
        /// Initializes a lookup from field key to header glyph state.
        /// </summary>
        /// <param name="fieldKeyToState">Active fields and their glyph state.</param>
        public RenameListColumnSortStates(
            IReadOnlyDictionary<RenameListFieldKey, RenameListColumnSortState> fieldKeyToState
        )
        {
            ArgumentNullException.ThrowIfNull(fieldKeyToState);
            _fieldKeyToState = fieldKeyToState;
        }

        /// <summary>
        /// Empty lookup (Auto-Sort off).
        /// </summary>
        public static RenameListColumnSortStates Inactive { get; } =
            new(new Dictionary<RenameListFieldKey, RenameListColumnSortState>());

        /// <summary>
        /// Gets header glyph state for a field key, or inactive when that field is not a sort key.
        /// </summary>
        /// <param name="fieldKey">Original field key.</param>
        /// <returns>Priority and direction, or default when inactive.</returns>
        public RenameListColumnSortState this[RenameListFieldKey fieldKey] =>
            _fieldKeyToState.GetValueOrDefault(fieldKey);
    }
}

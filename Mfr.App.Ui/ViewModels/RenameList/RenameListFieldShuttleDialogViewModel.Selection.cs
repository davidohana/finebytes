using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    public sealed partial class RenameListFieldShuttleDialogViewModel
    {
        private IReadOnlyList<int> _selectedColumnRowIndices = [];
        private IReadOnlyList<int> _selectedSortRowIndices = [];
        private IReadOnlyList<RenameListField> _selectedAvailableOriginalFields = [];
        private IReadOnlyList<RenameListField> _selectedAvailablePreviewFields = [];
        private IReadOnlyList<RenameListField> _selectedAvailableSortFields = [];

        /// <summary>
        /// Gets selected row indices in the selected-columns list.
        /// </summary>
        public IReadOnlyList<int> SelectedColumnRowIndices => _selectedColumnRowIndices;

        /// <summary>
        /// Gets selected row indices in the selected-sort list.
        /// </summary>
        public IReadOnlyList<int> SelectedSortRowIndices => _selectedSortRowIndices;

        /// <summary>
        /// Sets multi-selection for the selected-columns list.
        /// </summary>
        /// <param name="indices">Selected row indices in list order.</param>
        /// <param name="anchorIndex">Primary selected row used for insert-below and direction toggles.</param>
        public void SetSelectedColumnRows(IReadOnlyList<int> indices, int anchorIndex)
        {
            ArgumentNullException.ThrowIfNull(indices);

            if (_suppressSelectionSync)
            {
                return;
            }

            _AssignColumnSelection(indices, anchorIndex);
            OnPropertyChanged(nameof(SelectedColumnRowIndices));
            _NotifyColumnSelectionIndexChanged();
        }

        /// <summary>
        /// Sets multi-selection for the selected-sort list.
        /// </summary>
        /// <param name="indices">Selected row indices in list order.</param>
        /// <param name="anchorIndex">Primary selected row used for insert-below and direction toggles.</param>
        public void SetSelectedSortRows(IReadOnlyList<int> indices, int anchorIndex)
        {
            ArgumentNullException.ThrowIfNull(indices);

            if (_suppressSelectionSync)
            {
                return;
            }

            _AssignSortSelection(indices, anchorIndex);
            OnPropertyChanged(nameof(SelectedSortRowIndices));
            _NotifySortSelectionIndexChanged();
        }

        /// <summary>
        /// Sets multi-selection for available original fields on the Columns tab.
        /// </summary>
        /// <param name="fields">Selected catalog fields in list order.</param>
        /// <param name="anchorField">Primary selected field for add commands.</param>
        public void SetSelectedAvailableOriginalFields(
            IReadOnlyList<RenameListField> fields,
            RenameListField? anchorField
        )
        {
            ArgumentNullException.ThrowIfNull(fields);

            if (_suppressSelectionSync)
            {
                return;
            }

            _selectedAvailableOriginalFields = [.. fields];
            SelectedAvailableOriginalField = anchorField ?? _LastOrNull(fields);
            OnPropertyChanged(nameof(SelectedAvailableOriginalFields));
            AddSelectedOriginalFieldCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Sets multi-selection for available preview fields on the Columns tab.
        /// </summary>
        /// <param name="fields">Selected catalog fields in list order.</param>
        /// <param name="anchorField">Primary selected field for add commands.</param>
        public void SetSelectedAvailablePreviewFields(
            IReadOnlyList<RenameListField> fields,
            RenameListField? anchorField
        )
        {
            ArgumentNullException.ThrowIfNull(fields);

            if (_suppressSelectionSync)
            {
                return;
            }

            _selectedAvailablePreviewFields = [.. fields];
            SelectedAvailablePreviewField = anchorField ?? _LastOrNull(fields);
            OnPropertyChanged(nameof(SelectedAvailablePreviewFields));
            AddSelectedPreviewFieldCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Sets multi-selection for available sort fields on the Sort tab.
        /// </summary>
        /// <param name="fields">Selected catalog fields in list order.</param>
        /// <param name="anchorField">Primary selected field for add commands.</param>
        public void SetSelectedAvailableSortFields(IReadOnlyList<RenameListField> fields, RenameListField? anchorField)
        {
            ArgumentNullException.ThrowIfNull(fields);

            if (_suppressSelectionSync)
            {
                return;
            }

            _selectedAvailableSortFields = [.. fields];
            SelectedAvailableSortField = anchorField ?? _LastOrNull(fields);
            OnPropertyChanged(nameof(SelectedAvailableSortFields));
            AddSelectedSortFieldCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Gets selected available original fields on the Columns tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailableOriginalFields => _selectedAvailableOriginalFields;

        /// <summary>
        /// Gets selected available preview fields on the Columns tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailablePreviewFields => _selectedAvailablePreviewFields;

        /// <summary>
        /// Gets selected available sort fields on the Sort tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailableSortFields => _selectedAvailableSortFields;

        /// <summary>
        /// Stores column multi-selection and keeps the draft anchor on a selected row.
        /// </summary>
        private void _AssignColumnSelection(IReadOnlyList<int> indices, int anchorIndex)
        {
            _selectedColumnRowIndices = _NormalizeIndices(indices, _columns.Items.Count);
            _columns.SelectedIndex = _ResolveAnchor(_selectedColumnRowIndices, anchorIndex);
        }

        /// <summary>
        /// Stores sort multi-selection and keeps the draft anchor on a selected row.
        /// </summary>
        private void _AssignSortSelection(IReadOnlyList<int> indices, int anchorIndex)
        {
            _selectedSortRowIndices = _NormalizeIndices(indices, _sortKeys.Items.Count);
            _sortKeys.SelectedIndex = _ResolveAnchor(_selectedSortRowIndices, anchorIndex);
        }

        private IReadOnlyList<int> _GetColumnMoveIndices()
        {
            return _selectedColumnRowIndices;
        }

        private IReadOnlyList<int> _GetSortMoveIndices()
        {
            return _selectedSortRowIndices;
        }

        private bool _IsSingleColumnSelection(int index)
        {
            return _columns.SelectedIndex == index && _IsSingleIndexSelection(_selectedColumnRowIndices, index);
        }

        private bool _IsSingleSortSelection(int index)
        {
            return _sortKeys.SelectedIndex == index && _IsSingleIndexSelection(_selectedSortRowIndices, index);
        }

        private static bool _IsSingleIndexSelection(IReadOnlyList<int> indices, int index)
        {
            if (index < 0)
            {
                return indices.Count == 0;
            }

            return indices.Count == 1 && indices[0] == index;
        }

        /// <summary>
        /// Gets the insert index below the last selected row, or the list end when nothing is selected.
        /// </summary>
        private static int _GetInsertIndexBelow(IReadOnlyList<int> selectedIndices, int selectedIndex, int count)
        {
            if (selectedIndices.Count > 0)
            {
                return Math.Min(selectedIndices[^1] + 1, count);
            }

            return selectedIndex >= 0 ? selectedIndex + 1 : count;
        }

        private static bool _CanMoveToward(IReadOnlyList<int> indices, int itemCount, int offset)
        {
            if (indices.Count == 0 || itemCount == 0)
            {
                return false;
            }

            var indexToIsSelected = indices.ToHashSet();
            foreach (var index in indices)
            {
                var neighborIndex = index + offset;
                var neighborIsInRange = neighborIndex >= 0 && neighborIndex < itemCount;
                if (neighborIsInRange && !indexToIsSelected.Contains(neighborIndex))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<int> _NormalizeIndices(IReadOnlyList<int> indices, int itemCount)
        {
            return [.. indices.Where(index => index >= 0 && index < itemCount).Distinct().OrderBy(index => index)];
        }

        private static int _ResolveAnchor(IReadOnlyList<int> indices, int anchorIndex)
        {
            if (indices.Count == 0)
            {
                return -1;
            }

            if (indices.Contains(anchorIndex))
            {
                return anchorIndex;
            }

            return indices[^1];
        }

        private static RenameListField? _LastOrNull(IReadOnlyList<RenameListField> fields)
        {
            return fields.Count == 0 ? null : fields[^1];
        }
    }
}

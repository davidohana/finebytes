using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    public sealed partial class RenameListFieldShuttleDialogViewModel
    {

        /// <summary>
        /// Gets selected row indices in the selected-columns list.
        /// </summary>
        public IReadOnlyList<int> SelectedColumnRowIndices => _columns.SelectedIndices;

        /// <summary>
        /// Gets selected row indices in the selected-sort list.
        /// </summary>
        public IReadOnlyList<int> SelectedSortRowIndices => _sortKeys.SelectedIndices;

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

            _columns.SetSelection(indices, anchorIndex);
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

            _sortKeys.SetSelection(indices, anchorIndex);
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

            SelectedAvailableOriginalFields = [.. fields];
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

            SelectedAvailablePreviewFields = [.. fields];
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

            SelectedAvailableSortFields = [.. fields];
            SelectedAvailableSortField = anchorField ?? _LastOrNull(fields);
            OnPropertyChanged(nameof(SelectedAvailableSortFields));
            AddSelectedSortFieldCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Gets selected available original fields on the Columns tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailableOriginalFields { get; private set; } = [];

        /// <summary>
        /// Gets selected available preview fields on the Columns tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailablePreviewFields { get; private set; } = [];

        /// <summary>
        /// Gets selected available sort fields on the Sort tab.
        /// </summary>
        public IReadOnlyList<RenameListField> SelectedAvailableSortFields { get; private set; } = [];

        private bool _IsSingleColumnSelection(int index)
        {
            return _columns.SelectedIndex == index && _IsSingleIndexSelection(_columns.SelectedIndices, index);
        }

        private bool _IsSingleSortSelection(int index)
        {
            return _sortKeys.SelectedIndex == index && _IsSingleIndexSelection(_sortKeys.SelectedIndices, index);
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
        /// Aligns an available-field list with a newly assigned anchor: empty when cleared, otherwise
        /// keep the current multi-selection when the anchor is already in it.
        /// </summary>
        /// <param name="current">Current available-field multi-selection.</param>
        /// <param name="anchor">New primary selected field, or <see langword="null"/> to clear.</param>
        /// <returns>The existing list when it already contains <paramref name="anchor"/>; otherwise a one-item list.</returns>
        private static IReadOnlyList<RenameListField> _AvailableListForAnchor(
            IReadOnlyList<RenameListField> current,
            RenameListField? anchor
        )
        {
            if (anchor is null)
            {
                return [];
            }

            if (current.Contains(anchor))
            {
                return current;
            }

            return [anchor];
        }

        private static RenameListField? _LastOrNull(IReadOnlyList<RenameListField> fields)
        {
            return fields.Count == 0 ? null : fields[^1];
        }
    }
}

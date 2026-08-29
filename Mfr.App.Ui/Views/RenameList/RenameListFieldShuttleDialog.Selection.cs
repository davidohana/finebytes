using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    public partial class RenameListFieldShuttleDialog
    {
        private bool _isSyncingSelection;
        private bool _restoreQueued;

        private void _WireSelectionHandlers()
        {
            AvailableOriginalFieldsList.SelectionChanged += (_, _) => _OnAvailableOriginalSelectionChanged();
            AvailablePreviewFieldsList.SelectionChanged += (_, _) => _OnAvailablePreviewSelectionChanged();
            AvailableSortFieldsList.SelectionChanged += (_, _) => _OnAvailableSortSelectionChanged();
            SelectedColumnsList.SelectionChanged += (_, _) => _OnSelectedColumnsSelectionChanged();
            SelectedSortList.SelectionChanged += (_, _) => _OnSelectedSortSelectionChanged();
            _WireShuttleListKeyDown(AvailableOriginalFieldsList);
            _WireShuttleListKeyDown(AvailablePreviewFieldsList);
            _WireShuttleListKeyDown(AvailableSortFieldsList);
            _WireShuttleListKeyDown(SelectedColumnsList);
            _WireShuttleListKeyDown(SelectedSortList);
        }

        private static void _WireShuttleListKeyDown(ListBox listBox)
        {
            listBox.KeyDown += _OnShuttleListKeyDown;
        }

        private static void _OnShuttleListKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not ListBox listBox || e.Key is not (Key.Home or Key.End))
            {
                return;
            }

            if (e.KeyModifiers is not KeyModifiers.None)
            {
                return;
            }

            var itemCount = listBox.ItemCount;
            if (itemCount == 0)
            {
                e.Handled = true;
                return;
            }

            var index = e.Key == Key.Home ? 0 : itemCount - 1;
            listBox.Selection.Clear();
            listBox.Selection.Select(index);
            listBox.ScrollIntoView(index);
            e.Handled = true;
        }

        private void _OnAvailableOriginalSelectionChanged()
        {
            if (_ViewModel is null || _isSyncingSelection || _TryKeepMultiSelectionForDrag(AvailableOriginalFieldsList))
            {
                return;
            }

            var selected = _ReadSelectedFields(AvailableOriginalFieldsList);
            _ViewModel.SetSelectedAvailableOriginalFields(
                selected,
                AvailableOriginalFieldsList.SelectedItem as RenameListField
            );
        }

        private void _OnAvailablePreviewSelectionChanged()
        {
            if (_ViewModel is null || _isSyncingSelection || _TryKeepMultiSelectionForDrag(AvailablePreviewFieldsList))
            {
                return;
            }

            var selected = _ReadSelectedFields(AvailablePreviewFieldsList);
            _ViewModel.SetSelectedAvailablePreviewFields(
                selected,
                AvailablePreviewFieldsList.SelectedItem as RenameListField
            );
        }

        private void _OnAvailableSortSelectionChanged()
        {
            if (_ViewModel is null || _isSyncingSelection || _TryKeepMultiSelectionForDrag(AvailableSortFieldsList))
            {
                return;
            }

            var selected = _ReadSelectedFields(AvailableSortFieldsList);
            _ViewModel.SetSelectedAvailableSortFields(
                selected,
                AvailableSortFieldsList.SelectedItem as RenameListField
            );
        }

        private void _OnSelectedColumnsSelectionChanged()
        {
            if (_ViewModel is null || _isSyncingSelection || _TryKeepMultiSelectionForDrag(SelectedColumnsList))
            {
                return;
            }

            _ViewModel.SetSelectedColumnRows(
                _ReadSelectedIndices(SelectedColumnsList),
                SelectedColumnsList.SelectedIndex
            );
        }

        private void _OnSelectedSortSelectionChanged()
        {
            if (_ViewModel is null || _isSyncingSelection || _TryKeepMultiSelectionForDrag(SelectedSortList))
            {
                return;
            }

            _ViewModel.SetSelectedSortRows(_ReadSelectedIndices(SelectedSortList), SelectedSortList.SelectedIndex);
        }

        /// <summary>
        /// Undoes Avalonia's press collapse of a multi-selection before the next paint.
        /// </summary>
        private bool _TryKeepMultiSelectionForDrag(ListBox listBox)
        {
            if (
                !ReferenceEquals(listBox, _dragSession.SourceList)
                || _dragSession.SelectionSnapshot is not { Count: > 0 } snapshot
            )
            {
                return false;
            }

            var anchor = _dragSession.HitIndex is int hit && snapshot.Contains(hit) ? hit : snapshot[^1];
            _RestoreListSelection(listBox, snapshot, anchor);
            return true;
        }

        /// <summary>
        /// Restores ListBox selection from the view model after an items-source rebuild.
        /// </summary>
        private void _QueueRestoreListSelections()
        {
            if (_restoreQueued)
            {
                return;
            }

            _restoreQueued = true;
            _isSyncingSelection = true;
            Dispatcher.UIThread.Post(_FlushRestoreListSelections, DispatcherPriority.Loaded);
        }

        private void _FlushRestoreListSelections()
        {
            _restoreQueued = false;
            try
            {
                _RestoreAllListSelections();
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void _RestoreAllListSelections()
        {
            if (_ViewModel is null)
            {
                return;
            }

            _RestoreListSelection(
                SelectedColumnsList,
                _ViewModel.SelectedColumnRowIndices,
                _ViewModel.SelectedColumnRowIndex
            );
            _RestoreListSelection(SelectedSortList, _ViewModel.SelectedSortRowIndices, _ViewModel.SelectedSortRowIndex);
            _RestoreAvailableSelection(
                AvailableOriginalFieldsList,
                _ViewModel.SelectedAvailableOriginalFields,
                _ViewModel.SelectedAvailableOriginalField
            );
            _RestoreAvailableSelection(
                AvailablePreviewFieldsList,
                _ViewModel.SelectedAvailablePreviewFields,
                _ViewModel.SelectedAvailablePreviewField
            );
            _RestoreAvailableSelection(
                AvailableSortFieldsList,
                _ViewModel.SelectedAvailableSortFields,
                _ViewModel.SelectedAvailableSortField
            );
        }

        private void _RestoreAvailableSelection(
            ListBox listBox,
            IReadOnlyList<RenameListField> fields,
            RenameListField? anchor
        )
        {
            if (listBox.ItemsSource is not IEnumerable rowsEnumerable)
            {
                return;
            }

            var rows = rowsEnumerable.Cast<object>().ToList();
            var indices = fields.Select(field => rows.IndexOf(field)).Where(index => index >= 0).ToList();
            var anchorIndex = anchor is null ? -1 : rows.IndexOf(anchor);
            _RestoreListSelection(listBox, indices, anchorIndex);
        }

        private static IReadOnlyList<int> _ReadSelectedIndices(ListBox listBox)
        {
            return ListBoxDrag.ReadSelectedIndices(listBox);
        }

        private static IReadOnlyList<RenameListField> _ReadSelectedFields(ListBox listBox)
        {
            if (listBox.ItemsSource is not IEnumerable source)
            {
                return [];
            }

            var rows = source.Cast<RenameListField>().ToList();
            return [.. _ReadSelectedIndices(listBox).Where(index => index < rows.Count).Select(index => rows[index])];
        }

        /// <summary>
        /// Applies <paramref name="indices"/> to <paramref name="listBox"/> without collapsing to the anchor row.
        /// </summary>
        private void _RestoreListSelection(ListBox? listBox, IReadOnlyList<int> indices, int anchorIndex)
        {
            if (listBox is null)
            {
                return;
            }

            var wasSyncing = _isSyncingSelection;
            _isSyncingSelection = true;
            try
            {
                ListBoxDrag.RestoreSelection(listBox, indices, anchorIndex);
            }
            finally
            {
                _isSyncingSelection = wasSyncing;
            }
        }

        private void _SyncViewModelFromListBox(ListBox listBox)
        {
            if (_ViewModel is null)
            {
                return;
            }

            if (ReferenceEquals(listBox, SelectedColumnsList))
            {
                _ViewModel.SetSelectedColumnRows(_ReadSelectedIndices(listBox), listBox.SelectedIndex);
                return;
            }

            if (ReferenceEquals(listBox, SelectedSortList))
            {
                _ViewModel.SetSelectedSortRows(_ReadSelectedIndices(listBox), listBox.SelectedIndex);
                return;
            }

            if (ReferenceEquals(listBox, AvailableOriginalFieldsList))
            {
                _ViewModel.SetSelectedAvailableOriginalFields(
                    _ReadSelectedFields(listBox),
                    listBox.SelectedItem as RenameListField
                );
                return;
            }

            if (ReferenceEquals(listBox, AvailablePreviewFieldsList))
            {
                _ViewModel.SetSelectedAvailablePreviewFields(
                    _ReadSelectedFields(listBox),
                    listBox.SelectedItem as RenameListField
                );
                return;
            }

            if (ReferenceEquals(listBox, AvailableSortFieldsList))
            {
                _ViewModel.SetSelectedAvailableSortFields(
                    _ReadSelectedFields(listBox),
                    listBox.SelectedItem as RenameListField
                );
            }
        }
    }
}

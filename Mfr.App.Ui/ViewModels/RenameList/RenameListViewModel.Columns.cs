using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Config;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Visible grid columns for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        private List<RenameListVisibleColumn> _visibleColumns = [.. RenameListVisibleColumn.CreateDefaults()];

        /// <summary>
        /// Gets visible grid columns in left-to-right order.
        /// </summary>
        public IReadOnlyList<RenameListVisibleColumn> VisibleColumns => _visibleColumns;

        /// <summary>
        /// Raised when the view should open the unified field shuttle dialog.
        /// </summary>
        public event EventHandler<RenameListFieldShuttleTab>? FieldShuttleRequested;

        /// <summary>
        /// Replaces the visible column list.
        /// </summary>
        /// <param name="columns">New columns in grid order; at least one required.</param>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="columns"/> is empty or contains an unknown field key.
        /// </exception>
        public void SetVisibleColumns(IReadOnlyList<RenameListVisibleColumn> columns)
        {
            ArgumentNullException.ThrowIfNull(columns);
            if (columns.Count == 0)
            {
                throw new ArgumentException("At least one visible column is required.", nameof(columns));
            }

            foreach (var column in columns)
            {
                if (!RenameListFieldCatalog.TryGetField(column.Key, out _))
                {
                    throw new ArgumentException(
                        $"Unknown Rename List field '{column.Key.GroupId}/{column.Key.PropertyKey}'.",
                        nameof(columns)
                    );
                }
            }

            _visibleColumns = [.. columns];
            OnPropertyChanged(nameof(VisibleColumns));
        }

        /// <summary>
        /// Reorders visible columns to match a new left-to-right key sequence.
        /// </summary>
        /// <param name="orderedKeys">Field keys in grid order; must match the current visible set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="orderedKeys"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="orderedKeys"/> is empty, duplicates a key, or does not match the current visible columns.
        /// </exception>
        public void ReorderVisibleColumns(IReadOnlyList<RenameListFieldKey> orderedKeys)
        {
            ArgumentNullException.ThrowIfNull(orderedKeys);
            if (orderedKeys.Count == 0)
            {
                throw new ArgumentException("At least one visible column is required.", nameof(orderedKeys));
            }

            if (orderedKeys.Count != _visibleColumns.Count)
            {
                throw new ArgumentException(
                    "Reordered keys must include every currently visible column.",
                    nameof(orderedKeys)
                );
            }

            var keyToColumn = _visibleColumns.ToDictionary(column => column.Key);
            if (orderedKeys.Any(key => !keyToColumn.ContainsKey(key)))
            {
                throw new ArgumentException(
                    "Reordered keys must match the currently visible columns.",
                    nameof(orderedKeys)
                );
            }

            if (orderedKeys.SequenceEqual(_visibleColumns.Select(column => column.Key)))
            {
                return;
            }

            _visibleColumns = [.. orderedKeys.Select(key => keyToColumn[key])];
            OnPropertyChanged(nameof(VisibleColumns));
        }

        /// <summary>
        /// Removes one visible column by field key.
        /// </summary>
        /// <param name="key">Field key to hide.</param>
        /// <remarks>
        /// <para>No-op when the key is absent or hiding would leave zero columns.</para>
        /// </remarks>
        public void HideColumn(RenameListFieldKey key)
        {
            if (_visibleColumns.Count <= 1)
            {
                return;
            }

            var index = _visibleColumns.FindIndex(column => column.Key == key);
            if (index < 0)
            {
                return;
            }

            var columns = _visibleColumns.ToList();
            columns.RemoveAt(index);
            SetVisibleColumns(columns);
        }

        /// <summary>
        /// Opens the unified field shuttle dialog on the Columns tab (context menu and toolbar).
        /// </summary>
        [RelayCommand]
        public void OpenFieldShuttle()
        {
            _RequestFieldShuttle(RenameListFieldShuttleTab.Columns);
        }

        /// <summary>
        /// Opens the unified field shuttle dialog on the Sort tab (context and main menus).
        /// </summary>
        [RelayCommand]
        public void OpenEditSortFields()
        {
            _RequestFieldShuttle(RenameListFieldShuttleTab.Sort);
        }

        /// <summary>
        /// Opens the unified field shuttle dialog on the requested tab.
        /// </summary>
        /// <param name="tab">Initial tab (Columns or Sort).</param>
        public void OpenFieldShuttle(RenameListFieldShuttleTab tab)
        {
            _RequestFieldShuttle(tab);
        }

        private void _RequestFieldShuttle(RenameListFieldShuttleTab tab)
        {
            FieldShuttleRequested?.Invoke(this, tab);
        }

        /// <summary>
        /// Restores visible columns from session data.
        /// </summary>
        /// <param name="columns">
        /// Saved columns in grid order, or <see langword="null"/> for MFR7 defaults.
        /// </param>
        internal void ApplyVisibleColumns(IReadOnlyList<RenameListVisibleColumn>? columns)
        {
            if (columns is null)
            {
                _visibleColumns = [.. RenameListVisibleColumn.CreateDefaults()];
                OnPropertyChanged(nameof(VisibleColumns));
                return;
            }

            var validColumns = columns.Where(column => RenameListFieldCatalog.TryGetField(column.Key, out _)).ToList();
            if (validColumns.Count == 0)
            {
                _visibleColumns = [.. RenameListVisibleColumn.CreateDefaults()];
                OnPropertyChanged(nameof(VisibleColumns));
                return;
            }

            SetVisibleColumns(validColumns);
        }

        /// <summary>
        /// Captures the current visible columns for session save.
        /// </summary>
        /// <returns>Visible columns in grid order.</returns>
        internal IReadOnlyList<RenameListVisibleColumn> CaptureVisibleColumns()
        {
            return [.. _visibleColumns];
        }

        /// <summary>
        /// Restores visible columns from persisted session fields.
        /// </summary>
        /// <param name="columns">
        /// Saved columns in grid order, or <see langword="null"/> for MFR7 defaults.
        /// </param>
        internal void ApplyVisibleColumnsFromSession(IReadOnlyList<SessionStateRenameListColumn>? columns)
        {
            if (columns is null)
            {
                ApplyVisibleColumns(null);
                return;
            }

            var visibleColumns = columns.Select(column => new RenameListVisibleColumn(
                column.Key,
                column.Width ?? RenameListVisibleColumn.UseCatalogDefaultWidth
            ));

            ApplyVisibleColumns([.. visibleColumns]);
        }

        /// <summary>
        /// Captures the current visible columns for session save.
        /// </summary>
        /// <returns>Session column fields in grid order.</returns>
        internal IReadOnlyList<SessionStateRenameListColumn> CaptureVisibleColumnsForSession()
        {
            return
            [
                .. _visibleColumns.Select(column => new SessionStateRenameListColumn(
                    column.Key,
                    column.Width == RenameListVisibleColumn.UseCatalogDefaultWidth ? null : column.Width
                )),
            ];
        }

        /// <summary>
        /// Updates the pixel width for one visible column after a grid resize.
        /// </summary>
        /// <param name="key">Field key for the resized column.</param>
        /// <param name="width">New width in pixels.</param>
        /// <remarks>
        /// <para>Does not raise <see cref="VisibleColumns"/> change notifications to avoid rebuilding columns mid-resize.</para>
        /// </remarks>
        internal void UpdateVisibleColumnWidth(RenameListFieldKey key, int width)
        {
            var index = _visibleColumns.FindIndex(column => column.Key == key);
            if (index < 0)
            {
                return;
            }

            var column = _visibleColumns[index];
            if (column.Width == width)
            {
                return;
            }

            var updated = _visibleColumns.ToList();
            updated[index] = column with { Width = width };
            _visibleColumns = updated;
        }
    }

    /// <summary>
    /// Initial tab for the unified Rename List field shuttle dialog.
    /// </summary>
    public enum RenameListFieldShuttleTab
    {
        /// <summary>
        /// Visible column selection and ordering.
        /// </summary>
        Columns = 0,

        /// <summary>
        /// Auto-Sort key selection and ordering.
        /// </summary>
        Sort = 1,
    }
}

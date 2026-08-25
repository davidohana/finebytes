using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mfr.App.Ui.Collections
{
    /// <summary>
    /// Observable collection that adds or replaces many items with one change notification.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <remarks>
    /// <para>
    /// Each call to Add raises <see cref="INotifyCollectionChanged.CollectionChanged"/> once per item.
    /// Bound Avalonia lists handle each notification separately, so syncing thousands of rows after a
    /// bulk rename-list add would stall the UI even when filesystem work already finished on a background
    /// thread. DataGrid also ignores <see cref="NotifyCollectionChangedAction.Move"/>, so reordering uses
    /// <see cref="ReplaceAll"/>.
    /// </para>
    /// </remarks>
    public sealed class RangeObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Appends many items and notifies bound views once.
        /// </summary>
        /// <param name="items">Items to append.</param>
        /// <remarks>
        /// <para>
        /// Updates the backing list in one pass, then raises a single
        /// <see cref="NotifyCollectionChangedAction.Reset"/> so the grid refreshes once for the whole batch.
        /// </para>
        /// </remarks>
        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var batch = items as IReadOnlyList<T> ?? [.. items];
            if (batch.Count == 0)
            {
                return;
            }

            CheckReentrancy();
            _AddItems(batch);
            _NotifyReset();
        }

        /// <summary>
        /// Replaces all items and notifies bound views once.
        /// </summary>
        /// <param name="items">New contents in display order.</param>
        /// <remarks>
        /// <para>
        /// Avalonia DataGrid ignores <see cref="NotifyCollectionChangedAction.Move"/>, so reordering
        /// must go through Reset (or Remove/Add) for the grid to refresh.
        /// </para>
        /// </remarks>
        public void ReplaceAll(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var batch = items as IReadOnlyList<T> ?? [.. items];
            CheckReentrancy();
            Items.Clear();
            _AddItems(batch);
            _NotifyReset();
        }

        private void _AddItems(IReadOnlyList<T> batch)
        {
            foreach (var item in batch)
            {
                Items.Add(item);
            }
        }

        private void _NotifyReset()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

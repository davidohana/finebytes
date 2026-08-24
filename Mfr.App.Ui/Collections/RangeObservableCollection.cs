using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mfr.App.Ui.Collections
{
    /// <summary>
    /// Observable collection that appends many items with one change notification.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <para>
    /// Each call to Add raises <see cref="INotifyCollectionChanged.CollectionChanged"/> once per item.
    /// Bound Avalonia lists handle each notification separately, so syncing thousands of rows after a
    /// bulk rename-list add would stall the UI even when filesystem work already finished on a background
    /// thread.
    /// </para>
    public sealed class RangeObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Appends many items and notifies bound views once.
        /// </summary>
        /// <param name="items">Items to append.</param>
        /// <para>
        /// Updates the backing list in one pass, then raises a single
        /// <see cref="NotifyCollectionChangedAction.Reset"/> so the grid refreshes once for the whole batch.
        /// </para>
        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var batch = items as IReadOnlyList<T> ?? [.. items];
            if (batch.Count == 0)
            {
                return;
            }

            CheckReentrancy();
            foreach (var item in batch)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

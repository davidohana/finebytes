using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Shared ▲/▼ Rename List item index chrome (Visual Trim Helper, Format Token Preview).
    /// </summary>
    public abstract partial class RenameListItemNavigatorViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the 1-based item index label, or empty when unbound / list empty.
        /// </summary>
        [ObservableProperty]
        private string _itemIndexLabel = string.Empty;

        /// <summary>
        /// Gets whether Previous is enabled.
        /// </summary>
        public bool CanGoPrevious => RenameItemCount > 0 && ItemIndex > 0;

        /// <summary>
        /// Gets whether Next is enabled.
        /// </summary>
        public bool CanGoNext => RenameItemCount > 0 && ItemIndex < RenameItemCount - 1;

        /// <summary>
        /// Gets the current Rename List snapshot used for navigation.
        /// </summary>
        protected IReadOnlyList<RenameItem> RenameItems { get; private set; } = [];

        /// <summary>
        /// Gets the item count of <see cref="RenameItems"/>.
        /// </summary>
        protected int RenameItemCount => RenameItems.Count;

        /// <summary>
        /// Gets the current 0-based index, or <c>-1</c> when the sample is not bound to a list row.
        /// </summary>
        protected int ItemIndex { get; private set; }

        /// <summary>
        /// Moves to the previous Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        public void GoPrevious()
        {
            PrepareNavigation();
            if (!CanGoPrevious)
            {
                NotifyNavigationChanged();
                return;
            }

            ItemIndex--;
            ApplyCurrentItem();
        }

        /// <summary>
        /// Moves to the next Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        public void GoNext()
        {
            PrepareNavigation();
            if (!CanGoNext)
            {
                NotifyNavigationChanged();
                return;
            }

            ItemIndex++;
            ApplyCurrentItem();
        }

        /// <summary>
        /// Replaces the navigated item list without applying the current item.
        /// </summary>
        /// <param name="items">Current Rename List engine items.</param>
        protected void ReplaceRenameItems(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            RenameItems = items;
        }

        /// <summary>
        /// Sets the current 0-based index (or <c>-1</c> when unbound) without applying.
        /// </summary>
        /// <param name="index">New index.</param>
        protected void SetItemIndex(int index)
        {
            ItemIndex = index;
        }

        /// <summary>
        /// Updates <see cref="ItemIndexLabel"/> from the current index.
        /// </summary>
        protected void SyncItemIndexLabel()
        {
            if (RenameItemCount == 0 || ItemIndex < 0)
            {
                ItemIndexLabel = string.Empty;
                return;
            }

            ItemIndexLabel = (ItemIndex + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Raises CanGo* property and command can-execute changes.
        /// </summary>
        protected void NotifyNavigationChanged()
        {
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            GoPreviousCommand.NotifyCanExecuteChanged();
            GoNextCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Optional hook before can-execute checks (e.g. reload a live Rename List).
        /// </summary>
        protected virtual void PrepareNavigation() { }

        /// <summary>
        /// Applies the item at <see cref="ItemIndex"/> to sample / preview chrome.
        /// </summary>
        protected abstract void ApplyCurrentItem();
    }
}

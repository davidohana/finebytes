using System.Collections.ObjectModel;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List pane: hosts the preview grid for items queued to rename.
    /// </summary>
    public sealed class RenameListViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the rows shown in the Rename List grid.
        /// </summary>
        public ObservableCollection<RenameListEntry> Entries { get; } = [];
    }
}

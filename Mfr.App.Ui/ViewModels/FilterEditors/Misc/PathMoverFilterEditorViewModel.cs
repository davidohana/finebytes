using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Misc;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Misc
{
    /// <summary>
    /// Filter Configuration editor for <see cref="PathMoverFilter"/>.
    /// </summary>
    internal sealed partial class PathMoverFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public PathMoverFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the folder picker used by Browse (wired by the view via <see cref="Services.FolderPicker"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Argument is the current root for the dialog start location; return <see langword="null"/> when cancelled.
        /// </para>
        /// </remarks>
        internal Func<string?, CancellationToken, Task<string?>>? PickRootFolderAsync { get; set; }

        /// <summary>
        /// Gets or sets the absolute destination root folder.
        /// </summary>
        [ObservableProperty]
        private string _rootFolder = @"C:\";

        /// <summary>
        /// Gets or sets the optional sub-folder template (may include formatter tokens and <c>\</c> levels).
        /// </summary>
        [ObservableProperty]
        private string _subFolder = "MFR";

        partial void OnRootFolderChanged(string value) => _ApplyOptions();

        partial void OnSubFolderChanged(string value) => _ApplyOptions();

        /// <summary>
        /// Opens a folder picker and applies the chosen path as <see cref="RootFolder"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the picker closes.</returns>
        [RelayCommand]
        public async Task BrowseRootFolderAsync(CancellationToken cancellationToken)
        {
            if (PickRootFolderAsync is null)
            {
                return;
            }

            var picked = await PickRootFolderAsync(RootFolder, cancellationToken).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(picked))
            {
                return;
            }

            RootFolder = picked;
        }

        private void _SyncFromFilter()
        {
            if (Step.Filter is not PathMoverFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                RootFolder = filter.Options.RootFolder;
                SubFolder = filter.Options.SubFolder;
            });
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not PathMoverFilter filter)
            {
                return;
            }

            var options = new PathMoverOptions(RootFolder: RootFolder, SubFolder: SubFolder);
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}

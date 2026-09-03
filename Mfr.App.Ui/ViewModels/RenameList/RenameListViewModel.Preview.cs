using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Filter-chain preview for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Count of items whose preview differs from the original after the last preview pass.
        /// </summary>
        [ObservableProperty]
        private int _changeCount;

        /// <summary>
        /// Count of items with a preview error after the last preview pass.
        /// </summary>
        [ObservableProperty]
        private int _previewErrorCount;

        /// <summary>
        /// Runs the filter chain over every Rename List item and refreshes preview columns.
        /// </summary>
        /// <param name="chain">Live Applied Filters chain.</param>
        public void Preview(FilterChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);

            if (Entries.Count == 0)
            {
                ChangeCount = 0;
                PreviewErrorCount = 0;
                return;
            }

            var plan = _renameList.Preview(chain);
            ChangeCount = plan.ChangedCount;
            PreviewErrorCount = plan.ErrorCount;
            _RefreshFieldDisplay();
        }
    }
}

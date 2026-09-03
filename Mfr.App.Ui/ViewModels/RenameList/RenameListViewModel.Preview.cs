using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;
using Mfr.Models.Rename;

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

            _ = _renameList.Preview(chain);

            var changed = 0;
            var errors = 0;
            foreach (var item in _renameList.RenameItems)
            {
                if (item.Status == RenameStatus.PreviewError)
                {
                    errors++;
                    continue;
                }

                if (item.Status == RenameStatus.PreviewOk && item.HasPreviewChanges())
                {
                    changed++;
                }
            }

            ChangeCount = changed;
            PreviewErrorCount = errors;
            _RefreshFieldDisplay();
        }
    }
}

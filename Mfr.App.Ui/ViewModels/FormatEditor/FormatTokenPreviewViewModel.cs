using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// MFR7-style Preview band for <see cref="Views.FormatEditor.FormatTokenEditorDialog"/>:
    /// cycles Rename List items and evaluates the resulting token string against the current item.
    /// </summary>
    public sealed partial class FormatTokenPreviewViewModel : RenameListItemNavigatorViewModel
    {
        /// <summary>
        /// Sample text when the Rename List snapshot is empty (MFR7 parity).
        /// </summary>
        public const string EmptyListSampleText = "<Rename list is empty>";

        /// <summary>
        /// Preview result when the Rename List snapshot is empty (MFR7 parity).
        /// </summary>
        public const string PreviewUnavailableText = "<Preview N/A>";

        /// <summary>
        /// Prefix for evaluation failures (MFR7 parity).
        /// </summary>
        public const string ErrorPrefix = "ERROR: ";

        private string _resultingFormatString = string.Empty;

        /// <summary>
        /// Initializes preview state from a Rename List snapshot.
        /// </summary>
        /// <param name="renameItems">Items to cycle; empty when the list is unavailable.</param>
        public FormatTokenPreviewViewModel(IReadOnlyList<RenameItem>? renameItems = null)
        {
            ReplaceRenameItems(renameItems ?? []);
            ApplyCurrentItem();
        }

        /// <summary>
        /// Gets the read-only sample file name for the current Rename List item.
        /// </summary>
        [ObservableProperty]
        private string _sampleText = EmptyListSampleText;

        /// <summary>
        /// Gets the evaluated preview result for the current item and format string.
        /// </summary>
        [ObservableProperty]
        private string _previewResult = PreviewUnavailableText;

        /// <summary>
        /// Re-evaluates preview against <paramref name="resultingFormatString"/>.
        /// </summary>
        /// <param name="resultingFormatString">Full <c>&lt;token…&gt;</c> text from the editor.</param>
        public void Refresh(string resultingFormatString)
        {
            _resultingFormatString = resultingFormatString ?? string.Empty;
            _UpdatePreviewResult();
        }

        /// <summary>
        /// Updates sample / index chrome and re-evaluates the preview result.
        /// </summary>
        protected override void ApplyCurrentItem()
        {
            if (RenameItemCount == 0)
            {
                SampleText = EmptyListSampleText;
                SyncItemIndexLabel();
                PreviewResult = PreviewUnavailableText;
                NotifyNavigationChanged();
                return;
            }

            var item = RenameItems[ItemIndex];
            SampleText = item.Original.FullFileName;
            SyncItemIndexLabel();
            _UpdatePreviewResult();
            NotifyNavigationChanged();
        }

        /// <summary>
        /// Evaluates the current resulting format string against the current item.
        /// </summary>
        private void _UpdatePreviewResult()
        {
            if (RenameItemCount == 0)
            {
                PreviewResult = PreviewUnavailableText;
                return;
            }

            var item = RenameItems[ItemIndex];
            if (FormatStringSyntax.TryEvaluate(_resultingFormatString, item, out var result, out var error))
            {
                PreviewResult = result;
                return;
            }

            PreviewResult = ErrorPrefix + error;
        }
    }
}

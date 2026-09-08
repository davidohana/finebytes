using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// MFR7-style Preview band for <see cref="Views.FormatEditor.FormatTokenEditorDialog"/>:
    /// cycles Rename List items and evaluates the resulting token string against the current item.
    /// </summary>
    public sealed partial class FormatTokenPreviewViewModel : ViewModelBase
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

        private readonly IReadOnlyList<RenameItem> _renameItems;
        private string _resultingFormatString = string.Empty;
        private int _itemIndex;

        /// <summary>
        /// Initializes preview state from a Rename List snapshot.
        /// </summary>
        /// <param name="renameItems">Items to cycle; empty when the list is unavailable.</param>
        public FormatTokenPreviewViewModel(IReadOnlyList<RenameItem>? renameItems = null)
        {
            _renameItems = renameItems ?? [];
            _ApplyCurrentItem();
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
        /// Gets the 1-based item index label, or empty when the Rename List is empty.
        /// </summary>
        [ObservableProperty]
        private string _itemIndexLabel = string.Empty;

        /// <summary>
        /// Gets whether Previous is enabled.
        /// </summary>
        public bool CanGoPrevious => _renameItems.Count > 0 && _itemIndex > 0;

        /// <summary>
        /// Gets whether Next is enabled.
        /// </summary>
        public bool CanGoNext => _renameItems.Count > 0 && _itemIndex < _renameItems.Count - 1;

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
        /// Moves to the previous Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        public void GoPrevious()
        {
            if (!CanGoPrevious)
            {
                return;
            }

            _itemIndex--;
            _ApplyCurrentItem();
        }

        /// <summary>
        /// Moves to the next Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        public void GoNext()
        {
            if (!CanGoNext)
            {
                return;
            }

            _itemIndex++;
            _ApplyCurrentItem();
        }

        /// <summary>
        /// Updates sample / index chrome and re-evaluates the preview result.
        /// </summary>
        private void _ApplyCurrentItem()
        {
            if (_renameItems.Count == 0)
            {
                SampleText = EmptyListSampleText;
                ItemIndexLabel = string.Empty;
                PreviewResult = PreviewUnavailableText;
                _NotifyNavigationChanged();
                return;
            }

            var item = _renameItems[_itemIndex];
            SampleText = item.Original.FullFileName;
            ItemIndexLabel = (_itemIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            _UpdatePreviewResult();
            _NotifyNavigationChanged();
        }

        /// <summary>
        /// Evaluates the current resulting format string against the current item.
        /// </summary>
        private void _UpdatePreviewResult()
        {
            if (_renameItems.Count == 0)
            {
                PreviewResult = PreviewUnavailableText;
                return;
            }

            var item = _renameItems[_itemIndex];
            if (FormatStringSyntax.TryEvaluate(_resultingFormatString, item, out var result, out var error))
            {
                PreviewResult = result;
                return;
            }

            PreviewResult = ErrorPrefix + error;
        }

        /// <summary>
        /// Raises navigation property and command can-execute changes.
        /// </summary>
        private void _NotifyNavigationChanged()
        {
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            GoPreviousCommand.NotifyCanExecuteChanged();
            GoNextCommand.NotifyCanExecuteChanged();
        }
    }
}

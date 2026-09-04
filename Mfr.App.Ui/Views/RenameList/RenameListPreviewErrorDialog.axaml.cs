using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List Show Preview Error.
    /// </summary>
    public partial class RenameListPreviewErrorDialog : Window
    {
        private readonly string _copyText;

        /// <summary>
        /// Initializes the dialog with the selected row's preview failure.
        /// </summary>
        /// <param name="content">File path and preview error text.</param>
        public RenameListPreviewErrorDialog(RenameListPreviewErrorDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            InitializeComponent();
            SummaryText.Text = RenameListPreviewErrorDisplay.Summary;
            FilePathText.Text = content.FilePath;
            DetailsText.Text = RenameListPreviewErrorDisplay.FormatDetailsText(content);
            _copyText = RenameListPreviewErrorDisplay.FormatCopyText(content);
        }

        /// <inheritdoc />
        public RenameListPreviewErrorDialog()
        {
            InitializeComponent();
            _copyText = string.Empty;
        }

        private async void _OnCopyDetailsClick(object? sender, RoutedEventArgs e)
        {
            if (Clipboard is null || string.IsNullOrEmpty(_copyText))
            {
                return;
            }

            await Clipboard.SetTextAsync(_copyText);
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

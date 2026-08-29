using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List original metadata load errors (Show Load Errors).
    /// </summary>
    public partial class RenameListLoadErrorsDialog : Window
    {
        private readonly string _copyText;

        /// <summary>
        /// Initializes the dialog with all reader failures on the selected row.
        /// </summary>
        /// <param name="content">File path and stored load errors.</param>
        public RenameListLoadErrorsDialog(RenameListLoadErrorsDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            InitializeComponent();
            SummaryText.Text = RenameListLoadErrorDisplay.Summary;
            FilePathText.Text = content.FilePath;
            DetailsText.Text = RenameListLoadErrorDisplay.FormatDetailsText(content);
            _copyText = RenameListLoadErrorDisplay.FormatCopyText(content);
        }

        /// <inheritdoc />
        public RenameListLoadErrorsDialog()
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

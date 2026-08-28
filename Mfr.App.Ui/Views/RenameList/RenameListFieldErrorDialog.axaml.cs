using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List original metadata load errors (MFR7 Show Field Error).
    /// </summary>
    public partial class RenameListFieldErrorDialog : Window
    {
        private readonly string _copyText;

        /// <summary>
        /// Initializes the dialog with all reader failures on the selected row.
        /// </summary>
        /// <param name="content">File path and stored load errors.</param>
        public RenameListFieldErrorDialog(RenameListFieldErrorDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            InitializeComponent();
            FilePathText.Text = content.FilePath;
            DetailsText.Text = RenameListFieldErrorDisplay.FormatDetailsText(content);
            _copyText = RenameListFieldErrorDisplay.FormatCopyText(content);
        }

        /// <inheritdoc />
        public RenameListFieldErrorDialog()
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

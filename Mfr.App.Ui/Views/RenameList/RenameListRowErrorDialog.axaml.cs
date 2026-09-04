using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List row errors (load, preview, and later apply).
    /// </summary>
    public partial class RenameListRowErrorDialog : Window
    {
        private readonly string _copyText;

        /// <summary>
        /// Initializes the dialog with shared row-error content.
        /// </summary>
        /// <param name="content">Title, summary, path, user message, and optional technical details.</param>
        public RenameListRowErrorDialog(RenameListRowErrorDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            InitializeComponent();
            Title = content.Title;
            SummaryText.Text = content.Summary;
            FilePathText.Text = content.FilePath;
            UserMessageText.Text = content.UserMessage;

            var hasTechnicalDetails = !string.IsNullOrWhiteSpace(content.TechnicalDetails);
            TechnicalDetailsExpander.IsVisible = hasTechnicalDetails;
            if (hasTechnicalDetails)
            {
                TechnicalDetailsText.Text = content.TechnicalDetails;
            }

            _copyText = RenameListRowErrorDisplay.FormatCopyText(content);
        }

        /// <inheritdoc />
        public RenameListRowErrorDialog()
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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List original field-load errors (MFR7 Show Field Error).
    /// </summary>
    public partial class RenameListFieldErrorDialog : Window
    {
        private readonly string _copyText;

        /// <summary>
        /// Initializes the dialog with field error content.
        /// </summary>
        /// <param name="content">Field label and stored exception message.</param>
        public RenameListFieldErrorDialog(RenameListFieldErrorDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            InitializeComponent();
            ExplanationText.Text = content.UserExplanation;
            DetailsText.Text = content.TechnicalDetails;
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

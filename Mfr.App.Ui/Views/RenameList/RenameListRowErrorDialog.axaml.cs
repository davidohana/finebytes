using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Mfr.App.Ui.Services.Session;
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
            : this(_CopyTextFrom(content))
        {
            Title = content.Title;
            SummaryText.Text = content.Summary;
            PrimaryDetailsText.Text = RenameListRowErrorDisplay.FormatPrimaryDetails(
                content.FilePath,
                content.UserMessage
            );

            var hasTechnicalDetails = !string.IsNullOrWhiteSpace(content.TechnicalDetails);
            TechnicalDetailsExpander.IsVisible = hasTechnicalDetails;
            if (hasTechnicalDetails)
            {
                TechnicalDetailsText.Text = content.TechnicalDetails;
            }
        }

        /// <inheritdoc />
        public RenameListRowErrorDialog()
            : this(string.Empty) { }

        private RenameListRowErrorDialog(string copyText)
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            DialogSession.Attach(this, DialogIds.RenameListRowError, DialogGeometryMode.WidthAndPosition);
            _copyText = copyText;
        }

        /// <summary>
        /// Validates <paramref name="content"/> and builds the clipboard payload for the private ctor.
        /// </summary>
        private static string _CopyTextFrom(RenameListRowErrorDialogContent content)
        {
            ArgumentNullException.ThrowIfNull(content);
            return RenameListRowErrorDisplay.FormatCopyText(content);
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

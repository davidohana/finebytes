using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mfr.App.Ui.Views.Controls.FormatEditor
{
    /// <summary>
    /// Simple OK-only message dialog used by FormatEditor warnings and error details.
    /// </summary>
    public partial class FormatEditorMessageDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatEditorMessageDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the dialog with a title and message body.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="message">Message body.</param>
        public FormatEditorMessageDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

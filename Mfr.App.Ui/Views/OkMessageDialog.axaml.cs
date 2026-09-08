using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared OK-only message dialog (title + body) for simple warnings and error details.
    /// </summary>
    public partial class OkMessageDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public OkMessageDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with a title and message body.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="message">Message body.</param>
        public OkMessageDialog(string title, string message)
            : this()
        {
            Title = title;
            MessageText.Text = message;
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

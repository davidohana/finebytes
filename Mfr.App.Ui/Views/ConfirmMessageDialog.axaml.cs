using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared OK/Cancel confirmation dialog (title + body).
    /// <para>Closes with <see langword="true"/> for OK and <see langword="false"/> for Cancel or Escape.</para>
    /// </summary>
    public partial class ConfirmMessageDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public ConfirmMessageDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with a title and message body.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="message">Message body.</param>
        public ConfirmMessageDialog(string title, string message)
            : this()
        {
            Title = title;
            MessageText.Text = message;
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}

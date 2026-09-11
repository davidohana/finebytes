using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Modal prompt for a single text value (OK returns the string; Cancel / Escape returns null).
    /// </summary>
    public partial class TextInputDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public TextInputDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with title, prompt, and default text.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="prompt">Prompt above the text box.</param>
        /// <param name="defaultValue">Initial text box contents.</param>
        public TextInputDialog(string title, string prompt, string defaultValue)
            : this()
        {
            Title = title;
            PromptText.Text = prompt;
            ValueBox.Text = defaultValue;
        }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            ValueBox.Focus();
            ValueBox.SelectAll();
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close(ValueBox.Text ?? string.Empty);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}

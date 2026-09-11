using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Modal prompt for a single text value (OK returns the string; Cancel / Escape returns null).
    /// </summary>
    public partial class TextInputDialog : Window
    {
        private readonly TextInputPrompt? _prompt;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public TextInputDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with styled prompt content and default text.
        /// </summary>
        /// <param name="prompt">Title, prompt/note runs, and default text box value.</param>
        public TextInputDialog(TextInputPrompt prompt)
            : this()
        {
            ArgumentNullException.ThrowIfNull(prompt);
            _prompt = prompt;
            Title = prompt.Title;
            ValueBox.Text = prompt.DefaultValue;
            NoteText.IsVisible = prompt.Note is { IsEmpty: false };
            if (prompt.Multiline)
            {
                ValueBox.AcceptsReturn = true;
                ValueBox.TextWrapping = TextWrapping.Wrap;
                ValueBox.MinHeight = 80;
                ValueBox.Height = 80;
            }
        }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_prompt is not null)
            {
                StyledTextInlines.Apply(this, PromptText, _prompt.Prompt);
                if (_prompt.Note is { IsEmpty: false } note)
                {
                    StyledTextInlines.Apply(this, NoteText, note);
                }
            }

            ModalDialogTextFocus.FocusAndSelectAll(ValueBox);
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

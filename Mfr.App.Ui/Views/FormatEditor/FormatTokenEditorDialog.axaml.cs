using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FormatEditor;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Modal host for a format-token parameter editor (title, body, live resulting string, OK/Cancel).
    /// </summary>
    /// <remarks>
    /// Opens height-to-content, then locks height so only width remains resizable.
    /// </remarks>
    public partial class FormatTokenEditorDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatTokenEditorDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            ModalDialogHorizontalResize.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with a token editor view-model and matching body control.
        /// </summary>
        /// <param name="editor">Token parameter editor.</param>
        public FormatTokenEditorDialog(IFormatTokenEditorViewModel editor)
            : this()
        {
            ArgumentNullException.ThrowIfNull(editor);
            DataContext = editor;
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

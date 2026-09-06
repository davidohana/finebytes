using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Controls.FormatEditor;

namespace Mfr.App.Ui.Views.Controls.FormatEditor
{
    /// <summary>
    /// Modal host for a format-token parameter editor (title, body, live resulting string, OK/Cancel).
    /// </summary>
    public partial class FormatTokenEditorDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatTokenEditorDialog()
        {
            InitializeComponent();
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

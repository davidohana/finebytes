using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.FormatEditor;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Modal host for a format-token parameter editor (title, body, live resulting string, OK/Cancel).
    /// </summary>
    /// <remarks>
    /// Opens height-to-content, then locks height so only width remains resizable. Relocks when
    /// <see cref="IFormatTokenEditorViewModel.ResultingFormatString"/> changes (nested FormatEditor
    /// auto-grow / option edits).
    /// </remarks>
    public partial class FormatTokenEditorDialog : Window
    {
        private INotifyPropertyChanged? _heightRelockSource;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatTokenEditorDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            ModalDialogHorizontalResize.Attach(this);
            DataContextChanged += _OnDataContextChanged;
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

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            _heightRelockSource?.PropertyChanged -= _OnViewModelPropertyChanged;
            _heightRelockSource = DataContext as INotifyPropertyChanged;
            _heightRelockSource?.PropertyChanged += _OnViewModelPropertyChanged;
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not nameof(IFormatTokenEditorViewModel.ResultingFormatString))
            {
                return;
            }

            Dispatcher.UIThread.Post(
                () =>
                {
                    UpdateLayout();
                    ModalDialogHorizontalResize.LockHeightToContent(this);
                },
                DispatcherPriority.Loaded
            );
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

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Modal host for a format-token parameter editor (title, body, preview, live resulting string, OK/Cancel).
    /// </summary>
    /// <remarks>
    /// Opens height-to-content, then locks height so only width remains resizable. Relocks when
    /// <see cref="IFormatTokenEditorViewModel.ResultingFormatString"/> changes (nested FormatEditor
    /// auto-grow / option edits).
    /// </remarks>
    public partial class FormatTokenEditorDialog : Window
    {
        private readonly FormatTokenPreviewViewModel? _preview;
        private readonly IFormatTokenEditorViewModel? _editor;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatTokenEditorDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            ModalDialogHorizontalResize.Attach(this);
            ModalDialogHorizontalResize.RelockOnDataContextProperties(
                this,
                nameof(IFormatTokenEditorViewModel.ResultingFormatString)
            );
        }

        /// <summary>
        /// Initializes the dialog with a token editor view-model and matching body control.
        /// </summary>
        /// <param name="editor">Token parameter editor.</param>
        /// <param name="renameItems">Rename List snapshot for Preview; empty when unavailable.</param>
        public FormatTokenEditorDialog(
            IFormatTokenEditorViewModel editor,
            IReadOnlyList<RenameItem>? renameItems = null
        )
            : this()
        {
            ArgumentNullException.ThrowIfNull(editor);
            _editor = editor;
            PreviewRenameItems = renameItems ?? [];
            DataContext = editor;

            _preview = new FormatTokenPreviewViewModel(PreviewRenameItems);
            PreviewRow.DataContext = _preview;
            _preview.Refresh(editor.ResultingFormatString);

            if (editor is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += _OnEditorPropertyChanged;
            }

            Closed += _OnClosed;
        }

        /// <summary>
        /// Gets the Rename List snapshot used by this dialog's Preview (for nested token editors).
        /// </summary>
        internal IReadOnlyList<RenameItem> PreviewRenameItems { get; } = [];

        /// <summary>
        /// Resolves Rename List items for Preview from <paramref name="start"/> or its owner chain:
        /// a hosting <see cref="FormatTokenEditorDialog"/> snapshot, else <see cref="MainWindowViewModel"/>.
        /// </summary>
        /// <param name="start">Window that owns the FormatEditor (main window or a token dialog).</param>
        /// <returns>Rename items for Preview, or empty when none are available.</returns>
        internal static IReadOnlyList<RenameItem> ResolvePreviewRenameItems(Window? start)
        {
            for (Window? window = start; window is not null; window = window.Owner as Window)
            {
                if (window is FormatTokenEditorDialog { PreviewRenameItems.Count: > 0 } host)
                {
                    return host.PreviewRenameItems;
                }

                if (window.DataContext is MainWindowViewModel main)
                {
                    return [.. main.RenameListViewModel.Entries.Select(entry => entry.EngineItem)];
                }
            }

            return [];
        }

        private void _OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_editor is null || _preview is null)
            {
                return;
            }

            if (e.PropertyName is null or nameof(IFormatTokenEditorViewModel.ResultingFormatString))
            {
                _preview.Refresh(_editor.ResultingFormatString);
            }
        }

        private void _OnClosed(object? sender, EventArgs e)
        {
            if (_editor is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= _OnEditorPropertyChanged;
            }

            Closed -= _OnClosed;
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

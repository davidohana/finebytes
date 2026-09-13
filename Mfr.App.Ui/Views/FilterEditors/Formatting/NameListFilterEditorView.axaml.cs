using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.Filters;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.Views.FilterEditors.Formatting
{
    /// <summary>
    /// Option editor for <see cref="NameListFilter"/>.
    /// </summary>
    public partial class NameListFilterEditorView : UserControl
    {
        /// <summary>
        /// Ceiling for the entries editor so a large paste cannot expand Filter Configuration
        /// to the full document height (the pane host is an unconstrained <c>ScrollViewer</c>).
        /// </summary>
        public const double EntriesMaxHeight = 240;

        private bool _suppressTextSync;
        private NameListFilterEditorViewModel? _viewModel;

        /// <summary>
        /// Initializes the Name List option editor.
        /// </summary>
        public NameListFilterEditorView()
        {
            InitializeComponent();
            _ConfigureEntriesEditor();
            DataContextChanged += (_, _) => _AttachViewModel(DataContext as NameListFilterEditorViewModel);
            _AttachViewModel(DataContext as NameListFilterEditorViewModel);
        }

        /// <summary>
        /// Turns off hyperlink click handling and listens for document changes.
        /// </summary>
        private void _ConfigureEntriesEditor()
        {
            EntriesBox.Options.AllowScrollBelowDocument = false;
            EntriesBox.Options.EnableEmailHyperlinks = false;
            EntriesBox.Options.EnableHyperlinks = false;
            EntriesBox.Options.EnableImeSupport = true;
            EntriesBox.TextChanged += _OnEntriesTextChanged;
        }

        /// <summary>
        /// Binds the AvaloniaEdit document to <see cref="NameListFilterEditorViewModel.EntriesText"/>.
        /// </summary>
        private void _AttachViewModel(NameListFilterEditorViewModel? viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
            {
                return;
            }

            _viewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _viewModel = viewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _SetEditorText(_viewModel.EntriesText);
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(NameListFilterEditorViewModel.EntriesText) || _viewModel is null)
            {
                return;
            }

            _SetEditorText(_viewModel.EntriesText);
        }

        private void _OnEntriesTextChanged(object? sender, EventArgs e)
        {
            if (_suppressTextSync)
            {
                return;
            }

            var text = EntriesBox.Text ?? string.Empty;
            if (text.Length > ListEntryLength.DefaultEditorTextMaxLength)
            {
                text = text[..ListEntryLength.DefaultEditorTextMaxLength];
                _suppressTextSync = true;
                _TruncateEntriesDocument();
                _suppressTextSync = false;
            }

            if (_viewModel is not null && !string.Equals(_viewModel.EntriesText, text, StringComparison.Ordinal))
            {
                _viewModel.EntriesText = text;
            }

            _UpdateWatermarkVisibility();
        }

        /// <summary>
        /// Copies <paramref name="text"/> into AvaloniaEdit when it differs from the document.
        /// </summary>
        private void _SetEditorText(string text)
        {
            if (string.Equals(EntriesBox.Text, text, StringComparison.Ordinal))
            {
                _UpdateWatermarkVisibility();
                return;
            }

            _suppressTextSync = true;
            EntriesBox.Text = text;
            _suppressTextSync = false;
            _UpdateWatermarkVisibility();
        }

        /// <summary>
        /// Removes characters past the editor paste budget without assigning <see cref="TextEditor.Text"/>
        /// (that setter clears undo and throws while a paste undo group is open).
        /// </summary>
        private void _TruncateEntriesDocument()
        {
            var document = EntriesBox.Document;
            var maxLength = ListEntryLength.DefaultEditorTextMaxLength;
            if (document is null || document.TextLength <= maxLength)
            {
                return;
            }

            document.Remove(maxLength, document.TextLength - maxLength);
        }

        private void _UpdateWatermarkVisibility()
        {
            EntriesWatermark.IsVisible = string.IsNullOrEmpty(EntriesBox.Text);
        }
    }
}

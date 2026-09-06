using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.Controls.FormatEditor;
using Mfr.Filters.Formatting;

namespace Mfr.App.Ui.Views.Controls.FormatEditor
{
    /// <summary>
    /// Shared format-string editor: text box, searchable insert picker, edit stub, inline parse errors.
    /// </summary>
    public partial class FormatEditor : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<FormatEditor, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
        );

        /// <summary>
        /// Defines the <see cref="Watermark"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> WatermarkProperty = AvaloniaProperty.Register<
            FormatEditor,
            string?
        >(nameof(Watermark));

        /// <summary>
        /// Gets the control view-model (picker / error state).
        /// </summary>
        public FormatEditorViewModel ViewModel { get; }

        /// <summary>
        /// Initializes the FormatEditor control.
        /// </summary>
        public FormatEditor()
        {
            InitializeComponent();
            ViewModel = new FormatEditorViewModel(
                insertText: InsertTextAtCaret,
                jumpToError: JumpToError,
                editUnderCaret: _EditUnderCaret
            );
            ChromeRoot.DataContext = ViewModel;
            TemplateBox.AddHandler(DoubleTappedEvent, _OnTemplateDoubleTapped, RoutingStrategies.Bubble);
            TemplateBox.AddHandler(PointerPressedEvent, _OnTemplatePointerPressed, RoutingStrategies.Tunnel);
            InsertList.AddHandler(TappedEvent, _OnInsertItemTapped, RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the empty-field watermark.
        /// </summary>
        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        private bool _suppressTextSync;

        /// <summary>
        /// Inserts <paramref name="insertText"/> at the caret, replacing any selection.
        /// </summary>
        /// <param name="insertText">Text to insert (typically a catalog <c>InsertText</c>).</param>
        public void InsertTextAtCaret(string insertText)
        {
            ArgumentNullException.ThrowIfNull(insertText);

            _InsertFlyoutHide();
            var box = TemplateBox;
            var current = box.Text ?? string.Empty;
            var start = Math.Clamp(box.SelectionStart, 0, current.Length);
            var end = Math.Clamp(box.SelectionEnd, 0, current.Length);
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var next = current[..start] + insertText + current[end..];
            var caret = start + insertText.Length;
            _SetTextPreservingBinding(next);
            box.CaretIndex = caret;
            box.SelectionStart = caret;
            box.SelectionEnd = caret;
            box.Focus();
        }

        /// <summary>
        /// Focuses the text box and selects the last validation error span when present.
        /// </summary>
        public void JumpToError()
        {
            var result = ViewModel.LastParseResult;
            if (result is null || result.Success)
            {
                return;
            }

            var box = TemplateBox;
            var length = (box.Text ?? string.Empty).Length;
            var start = Math.Clamp(result.ErrorPosition, 0, length);
            var end = Math.Clamp(start + Math.Max(result.ErrorLength, 0), start, length);
            box.Focus();
            box.SelectionStart = start;
            box.SelectionEnd = end;

            // Details dialog only when the inline row is truncated.
            if (
                !string.IsNullOrEmpty(ViewModel.FullErrorMessage)
                && !string.Equals(ViewModel.ErrorMessage, ViewModel.FullErrorMessage, StringComparison.Ordinal)
            )
            {
                _ = _ShowMessageAsync("Format string Error", ViewModel.FullErrorMessage);
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property != TextProperty || _suppressTextSync)
            {
                return;
            }

            var text = change.GetNewValue<string>() ?? string.Empty;
            if (!string.Equals(TemplateBox.Text, text, StringComparison.Ordinal))
            {
                _suppressTextSync = true;
                TemplateBox.Text = text;
                _suppressTextSync = false;
            }

            ViewModel.Validate(text);
        }

        private void _InsertFlyoutHide()
        {
            if (InsertButton.Flyout is Flyout flyout)
            {
                flyout.Hide();
            }
        }

        private void _OnTemplateTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_suppressTextSync)
            {
                return;
            }

            var text = TemplateBox.Text ?? string.Empty;
            if (!string.Equals(Text, text, StringComparison.Ordinal))
            {
                _suppressTextSync = true;
                Text = text;
                _suppressTextSync = false;
            }

            ViewModel.Validate(text);
        }

        /// <summary>
        /// Inserts the tapped catalog row (pointer/touch). Keyboard highlight alone must not insert.
        /// </summary>
        private void _OnInsertItemTapped(object? sender, TappedEventArgs e)
        {
            if (e.Source is not Visual source)
            {
                return;
            }

            var item = source as ListBoxItem ?? source.FindAncestorOfType<ListBoxItem>();
            if (item?.DataContext is not FormatTokenCatalogEntry entry)
            {
                return;
            }

            ViewModel.InsertEntryCommand.Execute(entry);
        }

        /// <summary>
        /// Writes text to the box and <see cref="Text"/> without re-entrant sync, then re-validates.
        /// </summary>
        private void _SetTextPreservingBinding(string next)
        {
            _suppressTextSync = true;
            TemplateBox.Text = next;
            Text = next;
            _suppressTextSync = false;
            ViewModel.Validate(next);
        }

        private void _OnTemplateDoubleTapped(object? sender, TappedEventArgs e)
        {
            var span = _FindSpanAtCaret();
            if (span is null)
            {
                return;
            }

            TemplateBox.SelectionStart = span.Start;
            TemplateBox.SelectionEnd = span.Start + span.Length;
            e.Handled = true;
        }

        private void _OnTemplatePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(TemplateBox).Properties.IsRightButtonPressed)
            {
                return;
            }

            // Defer edit until after caret moves with the click.
            Avalonia.Threading.Dispatcher.UIThread.Post(_EditUnderCaret);
        }

        /// <summary>
        /// Opens the token editor (stub in PR B) or a warning when the caret is not on a token.
        /// </summary>
        private void _EditUnderCaret()
        {
            var span = _FindSpanAtCaret();
            if (span is null)
            {
                _ = _ShowMessageAsync(
                    "Formatting Parameter Editor",
                    "Cursor must be positioned on a formatting parameter in order to edit it.\n"
                        + "You can also right click on a formatting parameter to edit it."
                );
                return;
            }

            // Param editors land in F6 PR C; stub until the registry is wired.
            _ = _ShowMessageAsync(
                "Formatting Parameter Editor",
                $"Editing '<{span.CanonicalName}>' options is not available yet."
            );
        }

        /// <summary>
        /// Resolves the validated token span under the caret, re-validating when the last parse failed.
        /// </summary>
        private FormatTokenSpan? _FindSpanAtCaret()
        {
            var result = ViewModel.LastParseResult;
            if (result is null || !result.Success)
            {
                ViewModel.Validate(Text ?? string.Empty);
                result = ViewModel.LastParseResult;
            }

            if (result is null || !result.Success)
            {
                return null;
            }

            var caret = TemplateBox.CaretIndex;
            return result.Tokens.FirstOrDefault(t => caret >= t.Start && caret <= t.Start + t.Length);
        }

        private async Task _ShowMessageAsync(string title, string message)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new FormatEditorMessageDialog(title, message);
            await dialog.ShowDialog(owner);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Shared format-string editor: text box, searchable insert picker, token Edit, inline parse errors.
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
            InsertList.AddHandler(TreeViewItem.ExpandedEvent, _OnInsertGroupExpanded);
            InsertList.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
            InsertSearchBox.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
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

        /// <summary>
        /// Replaces a validated token span with <paramref name="newInsertText"/> (typically <c>&lt;…&gt;</c>).
        /// </summary>
        /// <param name="span">Token span from the last successful parse.</param>
        /// <param name="newInsertText">Replacement text including angle brackets.</param>
        public void ReplaceTokenSpan(FormatTokenSpan span, string newInsertText)
        {
            ArgumentNullException.ThrowIfNull(span);
            ArgumentNullException.ThrowIfNull(newInsertText);

            var current = Text ?? string.Empty;
            var start = Math.Clamp(span.Start, 0, current.Length);
            var end = Math.Clamp(span.Start + span.Length, start, current.Length);
            var next = current[..start] + newInsertText + current[end..];
            var caret = start + newInsertText.Length;
            _SetTextPreservingBinding(next);
            TemplateBox.CaretIndex = caret;
            TemplateBox.SelectionStart = caret;
            TemplateBox.SelectionEnd = caret;
            TemplateBox.Focus();
        }

        /// <summary>
        /// Test hook for Edit under caret: creates the token editor and optionally replaces the span.
        /// </summary>
        /// <param name="accept">When <see langword="true"/>, replaces the span with the editor result.</param>
        /// <param name="mutate">Optional mutation applied to the editor before building the result.</param>
        /// <returns><see langword="true"/> when a registered editor was created for the caret token.</returns>
        public bool EditUnderCaretForTests(bool accept, Action<IFormatTokenEditorViewModel>? mutate = null)
        {
            return EditTokenAtIndexForTests(TemplateBox.CaretIndex, accept, mutate);
        }

        /// <summary>
        /// Test hook for Edit at a character index: selects the token, creates its editor, and optionally replaces.
        /// </summary>
        /// <param name="index">Character index inside the token (click or caret).</param>
        /// <param name="accept">When <see langword="true"/>, replaces the span with the editor result.</param>
        /// <param name="mutate">Optional mutation applied to the editor before building the result.</param>
        /// <returns><see langword="true"/> when a registered editor was created for the token at
        /// <paramref name="index"/>.</returns>
        public bool EditTokenAtIndexForTests(int index, bool accept, Action<IFormatTokenEditorViewModel>? mutate = null)
        {
            var span = _FindSpanAtIndex(index);
            if (
                span is null
                || !FormatTokenEditorRegistry.TryCreate(span.CanonicalName, span.Args, out var editor)
                || editor is null
            )
            {
                return false;
            }

            _SelectSpan(span);
            mutate?.Invoke(editor);
            if (accept)
            {
                ReplaceTokenSpan(span, editor.ResultingFormatString);
            }

            return true;
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

        /// <summary>
        /// Focuses the insert search box when the picker flyout opens (after Avalonia's default popup focus).
        /// </summary>
        private void _OnInsertFlyoutOpened(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() => InsertSearchBox.Focus(), DispatcherPriority.Input);
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
        /// Inserts the tapped catalog leaf, or expands/collapses a group folder (pointer/touch).
        /// Keyboard highlight alone must not insert.
        /// </summary>
        private void _OnInsertItemTapped(object? sender, TappedEventArgs e)
        {
            if (e.Source is not Visual source)
            {
                return;
            }

            var item = source as TreeViewItem ?? source.FindAncestorOfType<TreeViewItem>();
            if (item?.DataContext is not FormatInsertPickerNode node)
            {
                return;
            }

            if (node.IsGroup)
            {
                if (!_OriginatedFromExpandChevron(source, item))
                {
                    item.IsExpanded = !item.IsExpanded;
                    e.Handled = true;
                }

                return;
            }

            if (node.Entry is not { } entry)
            {
                return;
            }

            e.Handled = true;
            ViewModel.InsertEntryCommand.Execute(entry);
        }

        /// <summary>
        /// True when the tap started on this item's expand/collapse chevron (already toggles
        /// <see cref="TreeViewItem.IsExpanded"/>).
        /// </summary>
        private static bool _OriginatedFromExpandChevron(Visual source, TreeViewItem item)
        {
            Visual? current = source;
            while (current is not null && !ReferenceEquals(current, item))
            {
                if (current is ToggleButton)
                {
                    return true;
                }

                current = current.GetVisualParent();
            }

            return false;
        }

        /// <summary>
        /// Accordion: opening a folder collapses sibling folders (tap, chevron, or keyboard).
        /// </summary>
        private void _OnInsertGroupExpanded(object? sender, RoutedEventArgs e)
        {
            if (e.Source is not TreeViewItem expanded)
            {
                return;
            }

            _CollapseSiblingGroups(expanded);
        }

        /// <summary>
        /// Collapses other expanded folders that share <paramref name="expanded"/>'s parent.
        /// </summary>
        private static void _CollapseSiblingGroups(TreeViewItem expanded)
        {
            var parent = ItemsControl.ItemsControlFromItemContainer(expanded);
            if (parent is null)
            {
                return;
            }

            for (var i = 0; i < parent.ItemCount; i++)
            {
                if (parent.ContainerFromIndex(i) is not TreeViewItem sibling)
                {
                    continue;
                }

                if (ReferenceEquals(sibling, expanded) || !sibling.IsExpanded)
                {
                    continue;
                }

                sibling.IsExpanded = false;
            }
        }

        /// <summary>
        /// Enter inserts the highlighted catalog leaf from search or list focus (not SelectionChanged).
        /// </summary>
        private void _OnInsertPickerKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !_TryInsertHighlighted())
            {
                return;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Inserts <see cref="TreeView.SelectedItem"/> when it is a catalog leaf.
        /// </summary>
        /// <returns><see langword="true"/> when a leaf was inserted.</returns>
        private bool _TryInsertHighlighted()
        {
            if (InsertList.SelectedItem is not FormatInsertPickerNode { Entry: { } entry })
            {
                return false;
            }

            ViewModel.InsertEntryCommand.Execute(entry);
            return true;
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
            var span = _FindSpanAtIndex(TemplateBox.CaretIndex);
            if (span is null)
            {
                return;
            }

            _SelectSpan(span);
            e.Handled = true;
        }

        private void _OnTemplatePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(TemplateBox).Properties.IsRightButtonPressed)
            {
                return;
            }

            if (_TryGetCharacterIndexAt(e.GetPosition(TemplateBox)) is not { } index)
            {
                return;
            }

            var span = _FindSpanAtIndex(index);
            if (span is null)
            {
                return;
            }

            _SelectSpan(span);
            e.Handled = true;
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsLoaded)
                {
                    return;
                }

                _ = _EditSpanAsync(span);
            });
        }

        /// <summary>
        /// Opens the token parameter editor for the caret span, or a warning when unavailable.
        /// </summary>
        private void _EditUnderCaret()
        {
            _ = _EditUnderCaretAsync();
        }

        private async Task _EditUnderCaretAsync()
        {
            var span = _FindSpanAtIndex(TemplateBox.CaretIndex);
            if (span is null)
            {
                await _ShowMessageAsync(
                    "Formatting Parameter Editor",
                    "Place the cursor on a formatting parameter to edit it, or right-click one."
                );
                return;
            }

            await _EditSpanAsync(span);
        }

        /// <summary>
        /// Selects <paramref name="span"/> and opens its parameter editor, or a warning when unavailable.
        /// </summary>
        private async Task _EditSpanAsync(FormatTokenSpan span)
        {
            _SelectSpan(span);
            if (!FormatTokenEditorRegistry.TryCreate(span.CanonicalName, span.Args, out var editor) || editor is null)
            {
                await _ShowMessageAsync(
                    "Formatting Parameter Editor",
                    $"The formatting parameter \"{_TokenDisplayName(span)}\" has no editable options."
                );
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new FormatTokenEditorDialog(editor);
            var accepted = await dialog.ShowDialog<bool>(owner);
            if (!accepted)
            {
                return;
            }

            ReplaceTokenSpan(span, editor.ResultingFormatString);
        }

        /// <summary>
        /// Highlights the full token span (MFR7 <c>SelectFormattingParam</c>).
        /// </summary>
        private void _SelectSpan(FormatTokenSpan span)
        {
            TemplateBox.Focus();
            TemplateBox.SelectionStart = span.Start;
            TemplateBox.SelectionEnd = span.Start + span.Length;
        }

        /// <summary>
        /// Maps a point in <see cref="TemplateBox"/> coordinates to the glyph under the pointer
        /// (<see cref="Avalonia.Media.CharacterHit.FirstCharacterIndex"/>), not the trailing-edge caret
        /// (<c>TextHitTestResult.TextPosition</c>).
        /// </summary>
        private int? _TryGetCharacterIndexAt(Point boxPoint)
        {
            if (TemplateBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault() is not { } presenter)
            {
                return null;
            }

            if (TemplateBox.TranslatePoint(boxPoint, presenter) is not { } presenterPoint)
            {
                return null;
            }

            var hit = presenter.TextLayout.HitTestPoint(presenterPoint);
            var length = (TemplateBox.Text ?? string.Empty).Length;
            return Math.Clamp(hit.CharacterHit.FirstCharacterIndex, 0, length);
        }

        /// <summary>
        /// Resolves the token whose half-open range <c>[Start, Start+Length)</c> contains
        /// <paramref name="index"/> (caret or click). Re-validates when the last parse failed.
        /// </summary>
        private FormatTokenSpan? _FindSpanAtIndex(int index)
        {
            var tokens = _TryGetParsedTokens();
            if (tokens is null)
            {
                return null;
            }

            return tokens.FirstOrDefault(t => index >= t.Start && index < t.Start + t.Length);
        }

        /// <summary>
        /// Last successful parse tokens, re-validating when the previous parse failed.
        /// </summary>
        private IReadOnlyList<FormatTokenSpan>? _TryGetParsedTokens()
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

            return result.Tokens;
        }

        /// <summary>
        /// Catalog display name for <paramref name="span"/>, or the canonical token name.
        /// </summary>
        private static string _TokenDisplayName(FormatTokenSpan span)
        {
            var entry = FormatTokenCatalog.Entries.FirstOrDefault(e =>
                string.Equals(e.CanonicalName, span.CanonicalName, StringComparison.OrdinalIgnoreCase)
            );
            return entry?.DisplayName ?? span.CanonicalName;
        }

        private async Task _ShowMessageAsync(string title, string message)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new OkMessageDialog(title, message);
            await dialog.ShowDialog(owner);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Rendering;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Shared format-string editor: AvaloniaEdit field with token highlight, searchable insert picker,
    /// token Edit, capped auto-grow, expand dialog, and inline parse errors.
    /// </summary>
    public partial class FormatEditor : UserControl
    {
        /// <summary>
        /// Multiline field floor height before auto-grow.
        /// </summary>
        public const double MultilineMinHeight = 64;

        /// <summary>
        /// Multiline field ceiling; content scrolls after this (~5–6 wrapped lines).
        /// </summary>
        public const double MultilineMaxHeight = 168;

        /// <summary>
        /// Single-line field height (no auto-grow).
        /// </summary>
        public const double SingleLineMinHeight = 26;

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
        /// Defines the <see cref="ValidationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<FormatStringValidationMode> ValidationModeProperty =
            AvaloniaProperty.Register<FormatEditor, FormatStringValidationMode>(
                nameof(ValidationMode),
                defaultValue: FormatStringValidationMode.Always
            );

        /// <summary>
        /// Defines the <see cref="AcceptsReturn"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AcceptsReturnProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(AcceptsReturn), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="ShowRightClickHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowRightClickHintProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(ShowRightClickHint), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="ShowExpandButton"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowExpandButtonProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(ShowExpandButton), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="MaxLength"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<FormatEditor, int>(
            nameof(MaxLength),
            defaultValue: 0
        );

        private readonly FormatTokenColorizingTransformer _colorizer = new();
        private readonly FormatTokenBackgroundRenderer _backgroundRenderer = new();

        private bool _suppressTextSync;
        private bool _templateHooksAttached;
        private double? _fixedEditorHeight;

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
            InsertList.AddHandler(TappedEvent, _OnInsertItemTapped, RoutingStrategies.Bubble);
            InsertList.AddHandler(TreeViewItem.ExpandedEvent, _OnInsertGroupExpanded);
            InsertList.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
            InsertSearchBox.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
            ViewModel.ValidationMode = ValidationMode;
            _ConfigureTemplateEditor();
            _ApplyAcceptsReturnLayout(AcceptsReturn);
            _SyncTemplateFromTextProperty(Text ?? string.Empty);
            ViewModel.Validate(Text ?? string.Empty);
            _RefreshHighlight();
            _UpdateWatermarkVisibility();
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

        /// <summary>
        /// Gets or sets whether validation always runs or only when likely tokens are present.
        /// </summary>
        public FormatStringValidationMode ValidationMode
        {
            get => GetValue(ValidationModeProperty);
            set => SetValue(ValidationModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the text box accepts multi-line input (Formatter default).
        /// </summary>
        public bool AcceptsReturn
        {
            get => GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the right-click hint under the field is shown.
        /// </summary>
        public bool ShowRightClickHint
        {
            get => GetValue(ShowRightClickHintProperty);
            set => SetValue(ShowRightClickHintProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the expand (larger editor dialog) tool button is shown.
        /// </summary>
        public bool ShowExpandButton
        {
            get => GetValue(ShowExpandButtonProperty);
            set => SetValue(ShowExpandButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum character count for the text box (<c>0</c> = unlimited).
        /// </summary>
        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        /// <summary>
        /// Disables capped auto-grow and forces a fixed editor height (expand dialog).
        /// </summary>
        /// <param name="height">Target editor height in device-independent pixels.</param>
        public void UseFixedEditorHeight(double height)
        {
            _fixedEditorHeight = height;
            _ApplyAcceptsReturnLayout(AcceptsReturn);
        }

        /// <summary>
        /// Test hook: builds the expand dialog without showing it.
        /// </summary>
        /// <returns>A dialog bound to this editor's <see cref="Text"/>.</returns>
        public FormatEditorExpandDialog CreateExpandDialogForTests()
        {
            return new FormatEditorExpandDialog(this);
        }

        /// <summary>
        /// Test hook: remeasures capped auto-grow height from the current document.
        /// </summary>
        public void UpdateAutoGrowHeightForTests()
        {
            _UpdateAutoGrowHeight();
        }

        /// <summary>
        /// Inserts <paramref name="insertText"/> at the caret, replacing any selection.
        /// </summary>
        /// <param name="insertText">Text to insert (typically a catalog <c>InsertText</c>).</param>
        public void InsertTextAtCaret(string insertText)
        {
            ArgumentNullException.ThrowIfNull(insertText);

            _InsertFlyoutHide();
            var current = TemplateBox.Text ?? string.Empty;
            var start = Math.Clamp(TemplateBox.SelectionStart, 0, current.Length);
            var end = Math.Clamp(start + TemplateBox.SelectionLength, start, current.Length);
            var next = current[..start] + insertText + current[end..];
            next = _ClampToMaxLength(next);
            var caret = Math.Min(start + insertText.Length, next.Length);
            _SetTextPreservingBinding(next);
            _SetCaret(caret);
            TemplateBox.Focus();
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

            var length = (TemplateBox.Text ?? string.Empty).Length;
            var start = Math.Clamp(result.ErrorPosition, 0, length);
            var end = Math.Clamp(start + Math.Max(result.ErrorLength, 0), start, length);
            TemplateBox.Focus();
            _SelectRange(start, end - start);

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
            var next = _ClampToMaxLength(current[..start] + newInsertText + current[end..]);
            var caret = Math.Min(start + newInsertText.Length, next.Length);
            _SetTextPreservingBinding(next);
            _SetCaret(caret);
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
            return EditTokenAtIndexForTests(TemplateBox.CaretOffset, accept, mutate);
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

            if (change.Property == AcceptsReturnProperty)
            {
                _ApplyAcceptsReturnLayout(change.GetNewValue<bool>());
                return;
            }

            if (change.Property == ValidationModeProperty)
            {
                ViewModel.ValidationMode = change.GetNewValue<FormatStringValidationMode>();
                ViewModel.Validate(Text ?? string.Empty);
                _RefreshHighlight();
                return;
            }

            if (change.Property == WatermarkProperty)
            {
                _UpdateWatermarkVisibility();
                return;
            }

            if (change.Property == MaxLengthProperty)
            {
                var clamped = _ClampToMaxLength(Text ?? string.Empty);
                if (!string.Equals(clamped, Text ?? string.Empty, StringComparison.Ordinal))
                {
                    Text = clamped;
                }

                return;
            }

            if (change.Property != TextProperty || _suppressTextSync)
            {
                return;
            }

            var text = _ClampToMaxLength(change.GetNewValue<string>() ?? string.Empty);
            if (!string.Equals(text, change.GetNewValue<string>() ?? string.Empty, StringComparison.Ordinal))
            {
                _suppressTextSync = true;
                Text = text;
                _suppressTextSync = false;
            }

            _SyncTemplateFromTextProperty(text);
            ViewModel.Validate(text);
            _RefreshHighlight();
            _UpdateWatermarkVisibility();
            _UpdateAutoGrowHeight();
        }

        /// <summary>
        /// Configures AvaloniaEdit options, colorizer, and input hooks once.
        /// </summary>
        private void _ConfigureTemplateEditor()
        {
            TemplateBox.Options.AllowScrollBelowDocument = false;
            TemplateBox.Options.EnableEmailHyperlinks = false;
            TemplateBox.Options.EnableHyperlinks = false;
            TemplateBox.Options.EnableImeSupport = true;
            _ApplySelectionChrome();

            if (_templateHooksAttached)
            {
                return;
            }

            _templateHooksAttached = true;
            TemplateBox.TextArea.TextView.LineTransformers.Add(_colorizer);
            TemplateBox.TextArea.TextView.BackgroundRenderers.Add(_backgroundRenderer);
            TemplateBox.TextChanged += _OnTemplateTextChanged;
            // AvaloniaEdit focuses TextArea (TemplateBox.Focusable is false), so tip visibility
            // must listen here as well as GotFocus/LostFocus on TemplateBox in AXAML.
            TemplateBox.TextArea.GotFocus += _OnTemplateFocusChanged;
            TemplateBox.TextArea.LostFocus += _OnTemplateFocusChanged;
            // Selection changes often only invalidate the Selection layer; refresh Background so
            // yellow wash holes under the selection stay in sync while dragging.
            TemplateBox.TextArea.Caret.PositionChanged += _OnTemplateCaretPositionChanged;
            TemplateBox.TextArea.TextView.VisualLinesChanged += _OnTemplateVisualLinesChanged;
            TemplateBox.AddHandler(DoubleTappedEvent, _OnTemplateDoubleTapped, RoutingStrategies.Bubble);
            TemplateBox.TextArea.AddHandler(PointerPressedEvent, _OnTemplatePointerPressed, RoutingStrategies.Tunnel);
            TemplateBox.TextArea.AddHandler(KeyDownEvent, _OnTemplateKeyDown, RoutingStrategies.Tunnel);
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            _UpdateAutoGrowHeight();
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Ctor may run before app theme resources resolve; never leave SelectionBrush null
            // (that clears AvaloniaEdit's themed TextAreaSelectionBrush and hides selection).
            _ApplySelectionChrome();
        }

        /// <summary>
        /// Applies selection brushes when resolved — does not assign null over the control theme.
        /// </summary>
        private void _ApplySelectionChrome()
        {
            if (_ResolveBrush("TextSelectionBrush") is { } selectionBrush)
            {
                TemplateBox.TextArea.SelectionBrush = selectionBrush;
            }

            if (_ResolveBrush("TextSelectionForegroundBrush") is { } selectionForeground)
            {
                TemplateBox.TextArea.SelectionForeground = selectionForeground;
            }
        }

        /// <summary>
        /// Invalidates the token wash layer when the caret (and usually selection) moves.
        /// </summary>
        private void _OnTemplateCaretPositionChanged(object? sender, EventArgs e)
        {
            TemplateBox.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        /// <summary>
        /// Adjusts editor height and wrapping for single-line vs multi-line hosts (or a fixed expand height).
        /// </summary>
        private void _ApplyAcceptsReturnLayout(bool acceptsReturn)
        {
            if (_fixedEditorHeight is { } fixedHeight)
            {
                TemplateBox.MinHeight = fixedHeight;
                TemplateBox.MaxHeight = fixedHeight;
                TemplateBox.Height = fixedHeight;
                // Wrap in the expand dialog even for single-line hosts; Enter still rejected when
                // AcceptsReturn is false.
                TemplateBox.WordWrap = true;
                TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                return;
            }

            if (acceptsReturn)
            {
                TemplateBox.ClearValue(HeightProperty);
                TemplateBox.MinHeight = MultilineMinHeight;
                TemplateBox.MaxHeight = MultilineMaxHeight;
                TemplateBox.WordWrap = true;
                TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                _UpdateAutoGrowHeight();
                return;
            }

            TemplateBox.ClearValue(HeightProperty);
            TemplateBox.ClearValue(MaxHeightProperty);
            TemplateBox.MinHeight = SingleLineMinHeight;
            TemplateBox.WordWrap = false;
            TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        /// <summary>
        /// Grows the multiline editor with wrapped content up to <see cref="MultilineMaxHeight"/>.
        /// </summary>
        private void _UpdateAutoGrowHeight()
        {
            if (!AcceptsReturn || _fixedEditorHeight is not null)
            {
                return;
            }

            var textView = TemplateBox.TextArea.TextView;
            if (textView.Bounds.Width <= 0)
            {
                return;
            }

            textView.EnsureVisualLines();
            var contentHeight = textView.DocumentHeight;
            if (double.IsNaN(contentHeight) || contentHeight <= 0)
            {
                contentHeight = Math.Max(textView.DefaultLineHeight, 1);
            }

            var chrome =
                TemplateBox.Padding.Top
                + TemplateBox.Padding.Bottom
                + TemplateBox.BorderThickness.Top
                + TemplateBox.BorderThickness.Bottom;
            var desired = contentHeight + chrome;
            var height = Math.Clamp(desired, MultilineMinHeight, MultilineMaxHeight);
            if (!double.IsNaN(TemplateBox.Height) && Math.Abs(TemplateBox.Height - height) < 0.5)
            {
                return;
            }

            TemplateBox.Height = height;
        }

        /// <summary>
        /// Remeasures auto-grow when wrap layout rebuilds visual lines.
        /// </summary>
        private void _OnTemplateVisualLinesChanged(object? sender, EventArgs e)
        {
            _UpdateAutoGrowHeight();
        }

        /// <summary>
        /// Opens the expand dialog for a larger editing surface.
        /// </summary>
        private void _OnExpandClick(object? sender, RoutedEventArgs e)
        {
            _ = _ShowExpandDialogAsync();
        }

        private async Task _ShowExpandDialogAsync()
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new FormatEditorExpandDialog(this);
            await dialog.ShowDialog(owner);
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

        private void _OnTemplateTextChanged(object? sender, EventArgs e)
        {
            if (_suppressTextSync)
            {
                return;
            }

            var text = TemplateBox.Text ?? string.Empty;
            if (MaxLength > 0 && text.Length > MaxLength)
            {
                text = text[..MaxLength];
                _suppressTextSync = true;
                TemplateBox.Text = text;
                _suppressTextSync = false;
                _SetCaret(Math.Min(TemplateBox.CaretOffset, text.Length));
            }

            if (!string.Equals(Text, text, StringComparison.Ordinal))
            {
                _suppressTextSync = true;
                Text = text;
                _suppressTextSync = false;
            }

            ViewModel.Validate(text);
            _RefreshHighlight();
            _UpdateWatermarkVisibility();
            _UpdateAutoGrowHeight();
        }

        private void _OnTemplateKeyDown(object? sender, KeyEventArgs e)
        {
            if (AcceptsReturn || (e.Key != Key.Enter && e.Key != Key.Return))
            {
                return;
            }

            e.Handled = true;
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
            var current = source;
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
        /// Writes text to the editor and <see cref="Text"/> without re-entrant sync, then re-validates.
        /// </summary>
        private void _SetTextPreservingBinding(string next)
        {
            _suppressTextSync = true;
            TemplateBox.Text = next;
            Text = next;
            _suppressTextSync = false;
            ViewModel.Validate(next);
            _RefreshHighlight();
            _UpdateWatermarkVisibility();
            _UpdateAutoGrowHeight();
        }

        /// <summary>
        /// Copies <see cref="Text"/> into the editor when the DP changed from outside.
        /// </summary>
        private void _SyncTemplateFromTextProperty(string text)
        {
            if (string.Equals(TemplateBox.Text, text, StringComparison.Ordinal))
            {
                return;
            }

            _suppressTextSync = true;
            TemplateBox.Text = text;
            _suppressTextSync = false;
        }

        private void _OnTemplateDoubleTapped(object? sender, TappedEventArgs e)
        {
            var span = _FindSpanAtIndex(TemplateBox.CaretOffset);
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
            var span = _FindSpanAtIndex(TemplateBox.CaretOffset);
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
            _SelectRange(span.Start, span.Length);
        }

        /// <summary>
        /// Sets caret and empty selection at <paramref name="offset"/>.
        /// </summary>
        private void _SetCaret(int offset)
        {
            var length = TemplateBox.Document?.TextLength ?? 0;
            var caret = Math.Clamp(offset, 0, length);
            TemplateBox.CaretOffset = caret;
            TemplateBox.Select(caret, 0);
        }

        /// <summary>
        /// Selects <paramref name="length"/> characters starting at <paramref name="start"/>.
        /// </summary>
        private void _SelectRange(int start, int length)
        {
            var documentLength = TemplateBox.Document?.TextLength ?? 0;
            var clampedStart = Math.Clamp(start, 0, documentLength);
            var clampedLength = Math.Clamp(length, 0, documentLength - clampedStart);
            TemplateBox.Select(clampedStart, clampedLength);
            TemplateBox.CaretOffset = clampedStart + clampedLength;
        }

        /// <summary>
        /// Maps a point in <see cref="TemplateBox"/> coordinates to the glyph under the pointer.
        /// </summary>
        private int? _TryGetCharacterIndexAt(Point boxPoint)
        {
            var textView = TemplateBox.TextArea.TextView;
            if (TemplateBox.TranslatePoint(boxPoint, textView) is not { } viewPoint)
            {
                return null;
            }

            textView.EnsureVisualLines();
            var position = textView.GetPosition(viewPoint + textView.ScrollOffset);
            if (position is null)
            {
                return null;
            }

            var document = TemplateBox.Document;
            if (document is null)
            {
                return null;
            }

            var offset = document.GetOffset(position.Value.Location);
            // Prefer the glyph under the pointer (exclusive end at next token's '<').
            if (offset > 0 && !position.Value.IsAtEndOfLine)
            {
                var visual = textView.GetVisualPosition(position.Value, VisualYPosition.LineMiddle);
                visual -= textView.ScrollOffset;
                if (viewPoint.X < visual.X)
                {
                    offset = Math.Max(0, offset - 1);
                }
            }

            return Math.Clamp(offset, 0, document.TextLength);
        }

        /// <summary>
        /// Resolves the token whose half-open range <c>[Start, Start+Length)</c> contains
        /// <paramref name="index"/> (caret or click), including prior-good spans when validation failed.
        /// </summary>
        private FormatTokenSpan? _FindSpanAtIndex(int index)
        {
            var tokens = _TryGetParsedTokens();
            if (tokens is null || tokens.Count == 0)
            {
                return null;
            }

            return tokens.FirstOrDefault(t => index >= t.Start && index < t.Start + t.Length);
        }

        /// <summary>
        /// Token spans from the latest validation (partial list when validation failed after prior tokens).
        /// </summary>
        private IReadOnlyList<FormatTokenSpan>? _TryGetParsedTokens()
        {
            var result = ViewModel.LastParseResult;
            if (result is null)
            {
                ViewModel.Validate(Text ?? string.Empty);
                result = ViewModel.LastParseResult;
            }

            return result?.Tokens;
        }

        /// <summary>
        /// Pushes the latest validation spans into highlight layers and redraws.
        /// </summary>
        private void _RefreshHighlight()
        {
            var result = ViewModel.LastParseResult;
            var tokens = result?.Tokens ?? [];
            _colorizer.Tokens = tokens;
            _backgroundRenderer.Tokens = tokens;
            if (result is { Success: false, ErrorPosition: >= 0, ErrorLength: > 0 })
            {
                _backgroundRenderer.ErrorPosition = result.ErrorPosition;
                _backgroundRenderer.ErrorLength = result.ErrorLength;
            }
            else
            {
                _backgroundRenderer.ErrorPosition = -1;
                _backgroundRenderer.ErrorLength = 0;
            }

            _backgroundRenderer.TokenBackground = _ResolveBrush("FormatTokenBackgroundBrush");
            _colorizer.TokenNameForeground = _ResolveBrush("FormatTokenNameForegroundBrush");
            _backgroundRenderer.ErrorBackground = _ResolveBrush("FormatTokenErrorBackgroundBrush");
            TemplateBox.TextArea.TextView.Redraw();
        }

        /// <summary>
        /// Resolves a themed brush from application or control resources.
        /// </summary>
        private IBrush? _ResolveBrush(string key)
        {
            if (TryGetResource(key, ActualThemeVariant, out var value) && value is IBrush brush)
            {
                return brush;
            }

            if (
                Application.Current?.TryGetResource(key, ActualThemeVariant, out value) == true
                && value is IBrush appBrush
            )
            {
                return appBrush;
            }

            return null;
        }

        private void _OnTemplateFocusChanged(object? sender, RoutedEventArgs e)
        {
            _UpdateWatermarkVisibility();
        }

        /// <summary>
        /// Shows the watermark only when the field is empty and focused (matches filter TextBox tips).
        /// </summary>
        private void _UpdateWatermarkVisibility()
        {
            var empty = string.IsNullOrEmpty(TemplateBox.Text);
            var focused = TemplateBox.IsFocused || TemplateBox.TextArea.IsFocused;
            TemplateWatermark.IsVisible = empty && focused && !string.IsNullOrEmpty(Watermark);
        }

        /// <summary>
        /// Truncates <paramref name="text"/> when <see cref="MaxLength"/> is positive.
        /// </summary>
        private string _ClampToMaxLength(string text)
        {
            if (MaxLength <= 0 || text.Length <= MaxLength)
            {
                return text;
            }

            return text[..MaxLength];
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

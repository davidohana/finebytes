using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Rendering;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Shared format-string editor: AvaloniaEdit field with token highlight, optional Insert/Edit chrome,
    /// capped wrap auto-grow, and inline parse errors. Under <see cref="FormatTokenPickerPane"/>, Insert/Edit
    /// move to the picker pane.
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
        /// Single-line field floor height; wraps and auto-grows above this without inserting newlines.
        /// </summary>
        public const double SingleLineMinHeight = 26;

        /// <summary>
        /// Horizontal padding inside the AvaloniaEdit field (matches <c>format-string-field</c> theme).
        /// </summary>
        public const double TemplateHorizontalPadding = 4;

        /// <summary>
        /// Minimum vertical padding when auto-grow sizes to content (keeps glyphs off the border).
        /// </summary>
        public const double TemplateMinVerticalPadding = 2;

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
        /// Defines the <see cref="ShowInsertButton"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowInsertButtonProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(ShowInsertButton), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="ShowEditButton"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowEditButtonProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(ShowEditButton), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="IsActiveTarget"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsActiveTargetProperty = AvaloniaProperty.Register<
            FormatEditor,
            bool
        >(nameof(IsActiveTarget));

        /// <summary>
        /// Defines the <see cref="MaxLength"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<FormatEditor, int>(
            nameof(MaxLength),
            defaultValue: 0
        );

        /// <summary>
        /// Defines the <see cref="IsReadOnly"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<FormatEditor, bool>(
            nameof(IsReadOnly)
        );

        /// <summary>
        /// Defines the <see cref="ShowsToolButtons"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatEditor, bool> ShowsToolButtonsProperty =
            AvaloniaProperty.RegisterDirect<FormatEditor, bool>(nameof(ShowsToolButtons), o => o.ShowsToolButtons);

        private readonly FormatTokenColorizingTransformer _colorizer = new();
        private readonly FormatTokenBackgroundRenderer _backgroundRenderer = new();

        private bool _suppressTextSync;
        private bool _templateHooksAttached;
        private FormatTokenPickerPane? _pickerPane;

        /// <summary>
        /// Gets the control view-model (validation / Edit chrome).
        /// </summary>
        public FormatEditorViewModel ViewModel { get; }

        /// <summary>
        /// Gets the format-token picker view-model (search + catalog).
        /// </summary>
        public FormatTokenPickerViewModel TokenPickerViewModel { get; }

        /// <summary>
        /// Initializes the FormatEditor control.
        /// </summary>
        public FormatEditor()
        {
            InitializeComponent();
            ViewModel = new FormatEditorViewModel(jumpToError: JumpToError, editUnderCaret: _EditUnderCaret);
            TokenPickerViewModel = new FormatTokenPickerViewModel(InsertTextAtCaret);
            ChromeRoot.DataContext = ViewModel;
            TokenPicker.DataContext = TokenPickerViewModel;
            ViewModel.ValidationMode = ValidationMode;
            _ConfigureTemplateEditor();
            _ApplyAcceptsReturnLayout(AcceptsReturn);
            _ApplyReadOnly(IsReadOnly);
            _SyncTemplateFromTextProperty(Text ?? string.Empty);
            ViewModel.Validate(Text ?? string.Empty);
            _RefreshHighlight();
            _UpdateWatermarkVisibility();
            _RefreshToolButtonsVisibility();
        }

        /// <summary>
        /// Gets the format-token picker control hosted in the Insert flyout (for tests).
        /// </summary>
        public FormatTokenPicker TokenPickerControl => TokenPicker;

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
        /// Gets or sets whether the per-field Insert button is shown (hidden under a picker pane).
        /// </summary>
        public bool ShowInsertButton
        {
            get => GetValue(ShowInsertButtonProperty);
            set => SetValue(ShowInsertButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the per-field Edit button is shown (hidden under a picker pane).
        /// </summary>
        public bool ShowEditButton
        {
            get => GetValue(ShowEditButtonProperty);
            set => SetValue(ShowEditButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this field is the Insert/Edit target of a hosting picker pane.
        /// </summary>
        public bool IsActiveTarget
        {
            get => GetValue(IsActiveTargetProperty);
            set => SetValue(IsActiveTargetProperty, value);
        }

        /// <summary>
        /// Gets whether any per-field tool button is visible.
        /// </summary>
        public bool ShowsToolButtons
        {
            get;
            private set => SetAndRaise(ShowsToolButtonsProperty, ref field, value);
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
        /// Gets or sets whether the field is display-only (no typing, insert, or token-edit gestures).
        /// </summary>
        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Opens the token parameter editor for the caret span, or a warning when unavailable.
        /// </summary>
        public void EditUnderCaret()
        {
            _EditUnderCaret();
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
            if (IsReadOnly)
            {
                return;
            }

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
            if (IsReadOnly)
            {
                return;
            }

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
        /// <returns><see langword="true"/> when a registered editor was created for the caret token
        /// (or the previous token to the left when the caret is not on a span).</returns>
        public bool EditUnderCaretForTests(bool accept, Action<IFormatTokenEditorViewModel>? mutate = null)
        {
            return _EditSpanForTests(_FindSpanAtOrLeftOfIndex(TemplateBox.CaretOffset), accept, mutate);
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
            return _EditSpanForTests(_FindSpanAtIndex(index), accept, mutate);
        }

        /// <summary>
        /// Shared test path for selecting a span and optionally applying an editor mutation.
        /// </summary>
        private bool _EditSpanForTests(FormatTokenSpan? span, bool accept, Action<IFormatTokenEditorViewModel>? mutate)
        {
            if (
                IsReadOnly
                || span is null
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

            if (change.Property == IsReadOnlyProperty)
            {
                _ApplyReadOnly(change.GetNewValue<bool>());
                return;
            }

            if (change.Property == ShowInsertButtonProperty || change.Property == ShowEditButtonProperty)
            {
                _RefreshToolButtonsVisibility();
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
            // ContextRequested (not PointerPressed) owns the Cut/Copy/Paste flyout; handle it so the
            // token dialog does not open under a stuck text context menu.
            TemplateBox.TextArea.AddHandler(
                ContextRequestedEvent,
                _OnTemplateContextRequested,
                RoutingStrategies.Tunnel
            );
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
            // Ctor / early Text binding may run before theme dictionaries resolve (token dialogs
            // bind Source before attach). Never leave SelectionBrush or token highlight brushes null.
            _ApplySelectionChrome();
            _RefreshHighlight();
            _pickerPane = this.FindAncestorOfType<FormatTokenPickerPane>();
            _pickerPane?.RegisterEditor(this);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _pickerPane?.UnregisterEditor(this);
            _pickerPane = null;
            base.OnDetachedFromVisualTree(e);
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
        /// Adjusts editor height and wrapping for single-line vs multi-line hosts.
        /// </summary>
        private void _ApplyAcceptsReturnLayout(bool acceptsReturn)
        {
            TemplateBox.ClearValue(HeightProperty);
            TemplateBox.MaxHeight = MultilineMaxHeight;
            TemplateBox.WordWrap = true;
            TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            // Auto only after the grow cap; otherwise AvaloniaEdit shows a thumb for tiny extent/viewport mismatch.
            TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            TemplateBox.MinHeight = acceptsReturn ? MultilineMinHeight : SingleLineMinHeight;
            if (acceptsReturn)
            {
                // Restore theme Padding (4,2) after single-line centering overwrote it.
                TemplateBox.ClearValue(PaddingProperty);
                TemplateWatermark.ClearValue(VerticalAlignmentProperty);
                TemplateWatermark.ClearValue(MarginProperty);
            }
            else
            {
                TemplateWatermark.VerticalAlignment = VerticalAlignment.Center;
                TemplateWatermark.Margin = new Thickness(TemplateHorizontalPadding, 0, TemplateHorizontalPadding, 0);
            }

            _UpdateAutoGrowHeight();
        }

        /// <summary>
        /// Syncs AvaloniaEdit read-only state (typing blocked; copy still works).
        /// </summary>
        private void _ApplyReadOnly(bool isReadOnly)
        {
            TemplateBox.IsReadOnly = isReadOnly;
            _RefreshHighlight();
        }

        /// <summary>
        /// Grows the editor with wrapped content up to <see cref="MultilineMaxHeight"/> (Enter still
        /// follows <see cref="AcceptsReturn"/>). Single-line hosts vertically center text in spare height.
        /// </summary>
        private void _UpdateAutoGrowHeight()
        {
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

            var border = TemplateBox.BorderThickness.Top + TemplateBox.BorderThickness.Bottom;
            var minHeight = AcceptsReturn ? MultilineMinHeight : SingleLineMinHeight;
            double height;
            if (AcceptsReturn)
            {
                var chrome = TemplateBox.Padding.Top + TemplateBox.Padding.Bottom + border;
                height = Math.Clamp(contentHeight + chrome, minHeight, MultilineMaxHeight);
            }
            else
            {
                // Height ignores vertical padding so we can distribute spare space equally (center text).
                var natural = contentHeight + border + (TemplateMinVerticalPadding * 2);
                height = Math.Clamp(Math.Max(natural, minHeight), minHeight, MultilineMaxHeight);
                var contentArea = Math.Max(0, height - border);
                var spare = Math.Max(0, contentArea - contentHeight);
                var top = spare / 2;
                var bottom = spare - top;
                var nextPadding = new Thickness(TemplateHorizontalPadding, top, TemplateHorizontalPadding, bottom);
                if (TemplateBox.Padding != nextPadding)
                {
                    TemplateBox.Padding = nextPadding;
                }
            }

            _ScheduleVerticalScrollBarVisibility();

            if (!double.IsNaN(TemplateBox.Height) && Math.Abs(TemplateBox.Height - height) < 0.5)
            {
                return;
            }

            TemplateBox.Height = height;
        }

        /// <summary>
        /// Defers scrollbar visibility so toggling it cannot invalidate visual lines mid-EnsureVisualLines.
        /// </summary>
        private void _ScheduleVerticalScrollBarVisibility()
        {
            Dispatcher.UIThread.Post(_ApplyVerticalScrollBarVisibility, DispatcherPriority.Render);
        }

        /// <summary>
        /// Shows a vertical scrollbar only when height is capped and document content overflows the viewport.
        /// </summary>
        private void _ApplyVerticalScrollBarVisibility()
        {
            var textView = TemplateBox.TextArea.TextView;
            var contentHeight = textView.DocumentHeight;
            if (double.IsNaN(contentHeight) || contentHeight <= 0)
            {
                contentHeight = Math.Max(textView.DefaultLineHeight, 1);
            }

            var height = TemplateBox.Height;
            if (double.IsNaN(height) || height <= 0)
            {
                height = TemplateBox.MinHeight;
            }

            var border = TemplateBox.BorderThickness.Top + TemplateBox.BorderThickness.Bottom;
            var verticalPadding = TemplateBox.Padding.Top + TemplateBox.Padding.Bottom;
            var viewport = Math.Max(0, height - border - verticalPadding);
            var atCap = height >= MultilineMaxHeight - 0.5;
            var overflows = contentHeight > viewport + 0.5;
            var visibility = atCap && overflows ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            if (TemplateBox.VerticalScrollBarVisibility == visibility)
            {
                return;
            }

            TemplateBox.VerticalScrollBarVisibility = visibility;
        }

        /// <summary>
        /// Remeasures auto-grow when wrap layout rebuilds visual lines.
        /// </summary>
        private void _OnTemplateVisualLinesChanged(object? sender, EventArgs e)
        {
            _UpdateAutoGrowHeight();
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
            Dispatcher.UIThread.Post(TokenPicker.FocusSearch, DispatcherPriority.Input);
        }

        /// <summary>
        /// Updates whether the Insert/Edit button strip is shown.
        /// </summary>
        private void _RefreshToolButtonsVisibility()
        {
            ShowsToolButtons = ShowInsertButton || ShowEditButton;
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
                // Do not assign TemplateBox.Text here: TextEditor.Text clears the undo stack and
                // throws while an undo group is still open (paste / BeginUpdate→EndUpdate).
                _suppressTextSync = true;
                _TruncateTemplateDocumentToMaxLength();
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
            if (IsReadOnly || !e.GetCurrentPoint(TemplateBox).Properties.IsRightButtonPressed)
            {
                return;
            }

            if (_TryGetTokenSpanAt(e.GetPosition(TemplateBox)) is not { } span)
            {
                return;
            }

            // Select on press so hit-tests match the pointer; open the editor from ContextRequested
            // so Avalonia's text context flyout can be cancelled with e.Handled.
            _SelectSpan(span);
            e.Handled = true;
        }

        private void _OnTemplateContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            FormatTokenSpan? span;
            if (e.TryGetPosition(TemplateBox, out var point))
            {
                span = _TryGetTokenSpanAt(point);
            }
            else
            {
                // Keyboard context-menu key: edit the caret token when present.
                span = _FindSpanAtOrLeftOfIndex(TemplateBox.CaretOffset);
            }

            if (span is null)
            {
                return;
            }

            _SelectSpan(span);
            e.Handled = true;
            _CloseTemplateContextUi();
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
            if (IsReadOnly)
            {
                return;
            }

            _ = _EditUnderCaretAsync();
        }

        private async Task _EditUnderCaretAsync()
        {
            var span = _FindSpanAtOrLeftOfIndex(TemplateBox.CaretOffset);
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
            _CloseTemplateContextUi();
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

            var dialog = new FormatTokenEditorDialog(editor, _TryGetRenameItems(owner));
            var accepted = await dialog.ShowDialog<bool>(owner);
            if (!accepted)
            {
                return;
            }

            ReplaceTokenSpan(span, editor.ResultingFormatString);
        }

        /// <summary>
        /// Resolves the live Rename List snapshot for token Preview when hosted under the main window.
        /// </summary>
        private static IReadOnlyList<RenameItem> _TryGetRenameItems(Window owner)
        {
            if (owner.DataContext is not MainWindowViewModel main)
            {
                return [];
            }

            return [.. main.RenameListViewModel.Entries.Select(entry => entry.EngineItem)];
        }

        /// <summary>
        /// Hides any text context menu/flyout on the template editor before a modal dialog.
        /// </summary>
        private void _CloseTemplateContextUi()
        {
            TemplateBox.ContextFlyout?.Hide();
            TemplateBox.TextArea.ContextFlyout?.Hide();
            TemplateBox.ContextMenu?.Close();
            TemplateBox.TextArea.ContextMenu?.Close();
        }

        /// <summary>
        /// Resolves the format-token span under <paramref name="point"/> when present.
        /// </summary>
        private FormatTokenSpan? _TryGetTokenSpanAt(Point point)
        {
            if (_TryGetCharacterIndexAt(point) is not { } index)
            {
                return null;
            }

            return _FindSpanAtIndex(index);
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
        /// Resolves the token under <paramref name="index"/>, or the rightmost token that ends at or
        /// before that index when the caret sits in literal text after a parameter.
        /// </summary>
        /// <remarks>
        /// Used only for Edit under caret. Pointer hit-testing stays exact via
        /// <see cref="_FindSpanAtIndex"/>.
        /// </remarks>
        private FormatTokenSpan? _FindSpanAtOrLeftOfIndex(int index)
        {
            var at = _FindSpanAtIndex(index);
            if (at is not null)
            {
                return at;
            }

            var tokens = _TryGetParsedTokens();
            if (tokens is null || tokens.Count == 0)
            {
                return null;
            }

            return tokens.Where(t => t.Start + t.Length <= index).OrderByDescending(t => t.Start).FirstOrDefault();
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

            _backgroundRenderer.TokenBackground = IsReadOnly ? null : _ResolveBrush("FormatTokenBackgroundBrush");
            _backgroundRenderer.TokenAltBackground = IsReadOnly ? null : _ResolveBrush("FormatTokenAltBackgroundBrush");
            _colorizer.TokenNameForeground = _ResolveBrush("FormatTokenNameForegroundBrush");
            _colorizer.TokenNumberForeground = _ResolveBrush("FormatTokenNumberForegroundBrush");
            _backgroundRenderer.ErrorBackground = IsReadOnly ? null : _ResolveBrush("FormatTokenErrorBackgroundBrush");
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
            var focused = TemplateBox.IsFocused || TemplateBox.TextArea.IsFocused;
            if (!focused)
            {
                return;
            }

            _pickerPane?.SetActiveEditor(this);
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
        /// Removes characters past <see cref="MaxLength"/> from the editor document.
        /// </summary>
        /// <remarks>
        /// Prefer this over assigning <c>TemplateBox.Text</c> from <see cref="_OnTemplateTextChanged"/>:
        /// AvaloniaEdit's Text setter calls <c>UndoStack.ClearAll()</c>, which throws when an undo group
        /// is open (for example during paste).
        /// </remarks>
        private void _TruncateTemplateDocumentToMaxLength()
        {
            if (MaxLength <= 0)
            {
                return;
            }

            var document = TemplateBox.Document;
            if (document is null || document.TextLength <= MaxLength)
            {
                return;
            }

            document.Remove(MaxLength, document.TextLength - MaxLength);
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

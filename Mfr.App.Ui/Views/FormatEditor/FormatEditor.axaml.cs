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
    /// Shared format-string editor: AvaloniaEdit field with token highlight, optional Insert/Edit chrome,
    /// and inline parse errors. Under <see cref="FormatTokenToolsHost"/>, Insert/Edit move to the tools pane.
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
        /// Defines the <see cref="ShowsToolButtons"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatEditor, bool> ShowsToolButtonsProperty =
            AvaloniaProperty.RegisterDirect<FormatEditor, bool>(nameof(ShowsToolButtons), o => o.ShowsToolButtons);

        private readonly FormatTokenColorizingTransformer _colorizer = new();
        private readonly FormatTokenBackgroundRenderer _backgroundRenderer = new();

        private bool _suppressTextSync;
        private bool _templateHooksAttached;
        private FormatTokenToolsHost? _toolsHost;

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
        /// Gets or sets whether the per-field Insert button is shown (hidden under a tools host).
        /// </summary>
        public bool ShowInsertButton
        {
            get => GetValue(ShowInsertButtonProperty);
            set => SetValue(ShowInsertButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the per-field Edit button is shown (hidden under a tools host).
        /// </summary>
        public bool ShowEditButton
        {
            get => GetValue(ShowEditButtonProperty);
            set => SetValue(ShowEditButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this field is the Insert/Edit target of a hosting tools pane.
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
        /// Opens the token parameter editor for the caret span, or a warning when unavailable.
        /// </summary>
        public void EditUnderCaret()
        {
            _EditUnderCaret();
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
            TemplateBox.AddHandler(DoubleTappedEvent, _OnTemplateDoubleTapped, RoutingStrategies.Bubble);
            TemplateBox.TextArea.AddHandler(PointerPressedEvent, _OnTemplatePointerPressed, RoutingStrategies.Tunnel);
            TemplateBox.TextArea.AddHandler(KeyDownEvent, _OnTemplateKeyDown, RoutingStrategies.Tunnel);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Ctor may run before app theme resources resolve; never leave SelectionBrush null
            // (that clears AvaloniaEdit's themed TextAreaSelectionBrush and hides selection).
            _ApplySelectionChrome();
            _toolsHost = this.FindAncestorOfType<FormatTokenToolsHost>();
            _toolsHost?.RegisterEditor(this);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _toolsHost?.UnregisterEditor(this);
            _toolsHost = null;
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
            if (acceptsReturn)
            {
                TemplateBox.MinHeight = 64;
                TemplateBox.WordWrap = true;
                TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                return;
            }

            TemplateBox.MinHeight = 26;
            TemplateBox.WordWrap = false;
            TemplateBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            TemplateBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
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
            var focused = TemplateBox.IsFocused || TemplateBox.TextArea.IsFocused;
            if (!focused)
            {
                return;
            }

            _toolsHost?.SetActiveEditor(this);
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

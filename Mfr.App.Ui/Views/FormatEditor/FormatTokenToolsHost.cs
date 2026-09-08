using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels.FormatEditor;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Wraps filter-option content with a collapsible format-token tools pane (Insert catalog + Edit).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Child content is the options body (<see cref="ContentControl.Content"/>). Descendant
    /// <see cref="FormatEditor"/> controls register on attach; Insert/Edit target the last-focused
    /// editor (defaults to the first registered field). Per-field Insert/Edit chrome is hidden while
    /// hosted here.
    /// </para>
    /// </remarks>
    public sealed class FormatTokenToolsHost : ContentControl
    {
        /// <summary>
        /// Expanded tools column: grip rail (22) + gap (4) + catalog card (~280).
        /// </summary>
        private const double ExpandedPaneWidth = 306;

        /// <summary>
        /// Collapsed tools column: grip rail (22) + trailing gap (4).
        /// </summary>
        private const double CollapsedPaneWidth = 26;

        /// <summary>
        /// Stable height for the grip rail (Edit on top, collapse centered). Token list may grow
        /// taller; the rail does not, so the expand button does not jump.
        /// </summary>
        public const double ToolsPaneMinHeight = 200;

        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<
            FormatTokenToolsHost,
            bool
        >(nameof(IsExpanded), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="HasActiveEditor"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenToolsHost, bool> HasActiveEditorProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenToolsHost, bool>(
                nameof(HasActiveEditor),
                o => o.HasActiveEditor
            );

        /// <summary>
        /// Defines the <see cref="ToolsPaneWidth"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenToolsHost, double> ToolsPaneWidthProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenToolsHost, double>(
                nameof(ToolsPaneWidth),
                o => o.ToolsPaneWidth
            );

        /// <summary>
        /// Defines the <see cref="CollapseToolTip"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenToolsHost, string> CollapseToolTipProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenToolsHost, string>(
                nameof(CollapseToolTip),
                o => o.CollapseToolTip
            );

        /// <summary>
        /// Defines the <see cref="CollapseIcon"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenToolsHost, Geometry?> CollapseIconProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenToolsHost, Geometry?>(nameof(CollapseIcon), o => o.CollapseIcon);

        private readonly List<FormatEditor> _editors = [];
        private readonly FormatTokenInsertPickerViewModel _pickerViewModel;

        private Button? _editButton;
        private Button? _collapseButton;

        static FormatTokenToolsHost()
        {
            HorizontalContentAlignmentProperty.OverrideDefaultValue<FormatTokenToolsHost>(HorizontalAlignment.Stretch);
            VerticalContentAlignmentProperty.OverrideDefaultValue<FormatTokenToolsHost>(VerticalAlignment.Stretch);
        }

        /// <summary>
        /// Initializes the tools host and shared insert-picker view-model.
        /// </summary>
        public FormatTokenToolsHost()
        {
            CollapseToolTip = "Collapse token tools";
            ToolsPaneWidth = ExpandedPaneWidth;
            _pickerViewModel = new FormatTokenInsertPickerViewModel(_InsertIntoActive);
            _RefreshChrome();
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FormatTokenToolsHost);

        /// <summary>
        /// Gets or sets whether the Insert catalog body is visible (default open).
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Gets whether an active format field is registered for Insert/Edit.
        /// </summary>
        public bool HasActiveEditor
        {
            get;
            private set => SetAndRaise(HasActiveEditorProperty, ref field, value);
        }

        /// <summary>
        /// Gets the tools pane width for expanded vs collapsed chrome.
        /// </summary>
        public double ToolsPaneWidth
        {
            get;
            private set => SetAndRaise(ToolsPaneWidthProperty, ref field, value);
        }

        /// <summary>
        /// Gets the collapse/expand button tooltip.
        /// </summary>
        public string CollapseToolTip
        {
            get;
            private set => SetAndRaise(CollapseToolTipProperty, ref field, value);
        }

        /// <summary>
        /// Gets the collapse/expand icon geometry.
        /// </summary>
        public Geometry? CollapseIcon
        {
            get;
            private set => SetAndRaise(CollapseIconProperty, ref field, value);
        }

        /// <summary>
        /// Gets the last-focused hosted <see cref="FormatEditor"/>, or <see langword="null"/>.
        /// </summary>
        public FormatEditor? ActiveEditor { get; private set; }

        /// <summary>
        /// Registers a descendant format field and hides its local Insert/Edit chrome.
        /// </summary>
        /// <param name="editor">Format editor under this host.</param>
        public void RegisterEditor(FormatEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);

            if (_editors.Contains(editor))
            {
                return;
            }

            _editors.Add(editor);
            editor.ShowInsertButton = false;
            editor.ShowEditButton = false;

            if (ActiveEditor is null)
            {
                _SetActiveEditor(editor);
            }
        }

        /// <summary>
        /// Unregisters a format field leaving the visual tree.
        /// </summary>
        /// <param name="editor">Previously registered editor.</param>
        public void UnregisterEditor(FormatEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);

            _editors.Remove(editor);
            if (!ReferenceEquals(ActiveEditor, editor))
            {
                return;
            }

            _SetActiveEditor(_editors.Count > 0 ? _editors[0] : null);
        }

        /// <summary>
        /// Marks <paramref name="editor"/> as the Insert/Edit target and updates the active-field cue.
        /// </summary>
        /// <param name="editor">Focused format editor under this host.</param>
        public void SetActiveEditor(FormatEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);

            if (!_editors.Contains(editor))
            {
                RegisterEditor(editor);
            }

            _SetActiveEditor(editor);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _editButton?.Click -= _OnEditClick;
            _collapseButton?.Click -= _OnCollapseClick;

            _editButton = e.NameScope.Find<Button>("PART_EditButton");
            _collapseButton = e.NameScope.Find<Button>("PART_CollapseButton");
            if (e.NameScope.Find<FormatTokenInsertPicker>("PART_InsertPicker") is { } insertPicker)
            {
                insertPicker.DataContext = _pickerViewModel;
            }

            _editButton?.Click += _OnEditClick;
            _collapseButton?.Click += _OnCollapseClick;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsExpandedProperty)
            {
                _RefreshChrome();
            }
        }

        private void _OnCollapseClick(object? sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void _OnEditClick(object? sender, RoutedEventArgs e)
        {
            _EditActive();
        }

        private void _InsertIntoActive(string insertText)
        {
            ActiveEditor?.InsertTextAtCaret(insertText);
        }

        private void _EditActive()
        {
            ActiveEditor?.EditUnderCaret();
        }

        private void _SetActiveEditor(FormatEditor? editor)
        {
            if (ReferenceEquals(ActiveEditor, editor))
            {
                return;
            }

            var previous = ActiveEditor;
            ActiveEditor = editor;
            if (previous is { } prior)
            {
                prior.IsActiveTarget = false;
            }

            if (ActiveEditor is { } active)
            {
                active.IsActiveTarget = true;
            }

            _RefreshChrome();
        }

        private void _RefreshChrome()
        {
            HasActiveEditor = ActiveEditor is not null;
            ToolsPaneWidth = IsExpanded ? ExpandedPaneWidth : CollapsedPaneWidth;
            CollapseToolTip = IsExpanded ? "Collapse token tools" : "Expand token tools";
            CollapseIcon = _ResolveGeometry(
                IsExpanded ? "FormatTokenToolsCollapseGeometry" : "FormatTokenToolsExpandGeometry"
            );
        }

        private Geometry? _ResolveGeometry(string key)
        {
            if (TryGetResource(key, ActualThemeVariant, out var value) && value is Geometry geometry)
            {
                return geometry;
            }

            if (
                Application.Current?.TryGetResource(key, ActualThemeVariant, out value) == true
                && value is Geometry appGeometry
            )
            {
                return appGeometry;
            }

            return null;
        }
    }
}

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
    /// Wraps filter-option content with a collapsible format-token picker pane (Insert catalog + Edit).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Child content is the options body (<see cref="ContentControl.Content"/>). Descendant
    /// <see cref="FormatEditor"/> controls register on attach; Insert/Edit target the last-focused
    /// editor (defaults to the first registered field). The active-field border cue is shown only when
    /// more than one format field is hosted (single-field panes must not look focused). Per-field
    /// Insert/Edit chrome is hidden while hosted here.
    /// </para>
    /// <para>
    /// Collapse state is driven by bound <see cref="IsExpanded"/> (Filter Configuration option editors
    /// two-way bind <see cref="ViewModels.FilterEditors.FilterOptionsEditorViewModel.FormatTokenPickerExpanded"/>).
    /// Persistence lives on <see cref="ViewModels.FilterEditors.FilterEditorViewModel"/>, not this control.
    /// </para>
    /// </remarks>
    public sealed class FormatTokenPickerPane : ContentControl
    {
        /// <summary>
        /// Expanded picker column: grip rail (22) + gap (4) + catalog card (~280).
        /// </summary>
        private const double ExpandedPaneWidth = 306;

        /// <summary>
        /// Collapsed picker column: grip rail (22) + trailing gap (4).
        /// </summary>
        private const double CollapsedPaneWidth = 26;

        /// <summary>
        /// Stable height for the grip rail (Edit on top, collapse centered). Token list may grow
        /// taller; the rail does not, so the expand button does not jump.
        /// </summary>
        public const double PaneMinHeight = 200;

        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<
            FormatTokenPickerPane,
            bool
        >(nameof(IsExpanded), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="HasActiveEditor"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenPickerPane, bool> HasActiveEditorProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, bool>(
                nameof(HasActiveEditor),
                o => o.HasActiveEditor
            );

        /// <summary>
        /// Defines the <see cref="PaneWidth"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenPickerPane, double> PaneWidthProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, double>(nameof(PaneWidth), o => o.PaneWidth);

        /// <summary>
        /// Defines the <see cref="CollapseToolTip"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenPickerPane, string> CollapseToolTipProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, string>(
                nameof(CollapseToolTip),
                o => o.CollapseToolTip
            );

        /// <summary>
        /// Defines the <see cref="CollapseIcon"/> property.
        /// </summary>
        public static readonly DirectProperty<FormatTokenPickerPane, Geometry?> CollapseIconProperty =
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, Geometry?>(
                nameof(CollapseIcon),
                o => o.CollapseIcon
            );

        private readonly List<FormatEditor> _editors = [];
        private readonly FormatTokenPickerViewModel _pickerViewModel;

        private Button? _editButton;
        private Button? _collapseButton;

        static FormatTokenPickerPane()
        {
            HorizontalContentAlignmentProperty.OverrideDefaultValue<FormatTokenPickerPane>(HorizontalAlignment.Stretch);
            VerticalContentAlignmentProperty.OverrideDefaultValue<FormatTokenPickerPane>(VerticalAlignment.Stretch);
        }

        /// <summary>
        /// Initializes the picker pane and shared insert-picker view-model.
        /// </summary>
        public FormatTokenPickerPane()
        {
            CollapseToolTip = "Collapse token picker";
            PaneWidth = ExpandedPaneWidth;
            _pickerViewModel = new FormatTokenPickerViewModel(_InsertIntoActive);
            _RefreshChrome();
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FormatTokenPickerPane);

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
        /// Gets the picker pane width for expanded vs collapsed chrome.
        /// </summary>
        public double PaneWidth
        {
            get;
            private set => SetAndRaise(PaneWidthProperty, ref field, value);
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
        /// <param name="editor">Format editor under this pane.</param>
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
                return;
            }

            // Count may have crossed into multi-field; refresh the disambiguation cue.
            _RefreshActiveTargetCues();
        }

        /// <summary>
        /// Unregisters a format field leaving the visual tree.
        /// </summary>
        /// <param name="editor">Previously registered editor.</param>
        public void UnregisterEditor(FormatEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);

            _editors.Remove(editor);
            editor.IsActiveTarget = false;
            if (!ReferenceEquals(ActiveEditor, editor))
            {
                _RefreshActiveTargetCues();
                return;
            }

            _SetActiveEditor(_editors.Count > 0 ? _editors[0] : null);
        }

        /// <summary>
        /// Marks <paramref name="editor"/> as the Insert/Edit target and updates the multi-field cue.
        /// </summary>
        /// <param name="editor">Focused format editor under this pane.</param>
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
            if (e.NameScope.Find<FormatTokenPicker>("PART_TokenPicker") is { } tokenPicker)
            {
                tokenPicker.DataContext = _pickerViewModel;
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
                _RefreshActiveTargetCues();
                return;
            }

            ActiveEditor = editor;
            _RefreshActiveTargetCues();
            _RefreshChrome();
        }

        /// <summary>
        /// Applies <see cref="FormatEditor.IsActiveTarget"/> only when several fields need disambiguation.
        /// </summary>
        private void _RefreshActiveTargetCues()
        {
            var showCue = _editors.Count > 1;
            foreach (var hosted in _editors)
            {
                hosted.IsActiveTarget = showCue && ReferenceEquals(hosted, ActiveEditor);
            }
        }

        private void _RefreshChrome()
        {
            HasActiveEditor = ActiveEditor is not null;
            PaneWidth = IsExpanded ? ExpandedPaneWidth : CollapsedPaneWidth;
            CollapseToolTip = IsExpanded ? "Collapse token picker" : "Expand token picker";
            CollapseIcon = _ResolveGeometry(
                IsExpanded ? "FormatTokenPickerCollapseGeometry" : "FormatTokenPickerExpandGeometry"
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

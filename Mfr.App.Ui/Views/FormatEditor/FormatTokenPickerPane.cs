using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Wraps filter-option content with a collapsible format-token picker pane (Insert catalog + Edit).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Child content is the options body (<see cref="ContentControl.Content"/>). Descendant
    /// <see cref="FormatEditor"/> controls register on attach; Insert/Edit target the last-focused
    /// editor (defaults to the first registered field). Per-field Insert/Edit chrome is hidden while
    /// hosted here.
    /// </para>
    /// <para>
    /// Collapse state is shared via <see cref="SessionStateFilterEditor.FormatTokenPickerExpanded"/>
    /// when the pane lives under a <see cref="MainWindowViewModel"/> with a loaded session (written
    /// when <see cref="IsExpanded"/> changes, flushed with <c>session.json</c> on main-window close).
    /// Missing session section defaults to expanded.
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
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, double>(
                nameof(PaneWidth),
                o => o.PaneWidth
            );

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
            AvaloniaProperty.RegisterDirect<FormatTokenPickerPane, Geometry?>(nameof(CollapseIcon), o => o.CollapseIcon);

        private readonly List<FormatEditor> _editors = [];
        private readonly FormatTokenPickerViewModel _pickerViewModel;
        private bool _isApplyingSessionExpanded;

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
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _ApplyExpandedFromSession();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property != IsExpandedProperty)
            {
                return;
            }

            _RefreshChrome();
            if (!_isApplyingSessionExpanded)
            {
                _PersistExpandedToSession();
            }
        }

        private void _OnCollapseClick(object? sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        /// <summary>
        /// Restores <see cref="IsExpanded"/> from the shared Filter Configuration session section.
        /// <para>No-op when there is no session document or the section is missing (stay expanded).</para>
        /// </summary>
        private void _ApplyExpandedFromSession()
        {
            var session = _TryFindSession();
            if (session?.FilterEditor is null)
            {
                return;
            }

            _isApplyingSessionExpanded = true;
            try
            {
                IsExpanded = session.FilterEditor.FormatTokenPickerExpanded;
            }
            finally
            {
                _isApplyingSessionExpanded = false;
            }
        }

        /// <summary>
        /// Writes <see cref="IsExpanded"/> into <see cref="SessionState.FilterEditor"/> for later panes.
        /// <para>Flush to disk still happens on main-window close via <c>session.json</c>.</para>
        /// </summary>
        private void _PersistExpandedToSession()
        {
            var session = _TryFindSession();
            if (session is null)
            {
                return;
            }

            session.EnsureFilterEditor().FormatTokenPickerExpanded = IsExpanded;
        }

        /// <summary>
        /// Finds the live main-window <see cref="SessionState"/> when this pane is under a session-backed shell.
        /// </summary>
        /// <returns>The session document, or <see langword="null"/> when absent.</returns>
        private SessionState? _TryFindSession()
        {
            if (VisualRoot is TopLevel { DataContext: MainWindowViewModel { Session: { } session } })
            {
                return session;
            }

            return null;
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

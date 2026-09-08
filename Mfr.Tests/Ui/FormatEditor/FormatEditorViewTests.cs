using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.App.Ui.Views.GridColumnSizing;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Headless tests for the shared <see cref="FormatEditor"/> control.
    /// </summary>
    public sealed class FormatEditorViewTests
    {
        /// <summary>
        /// Verifies insert-at-caret updates text and clears selection replacement.
        /// </summary>
        [AvaloniaFact]
        public void InsertTextAtCaret_InsertsCatalogToken()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = "pre__post" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = 3;
            box.Select(3, 2);

            editor.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("pre<file-name>post", editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies read-only mode blocks insert and uses AvaloniaEdit IsReadOnly.
        /// </summary>
        [AvaloniaFact]
        public void IsReadOnly_BlocksInsertAndMarksTemplateReadOnly()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor
            {
                Text = "<file-name>",
                IsReadOnly = true,
                ShowInsertButton = false,
                ShowEditButton = false,
            };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.True(box.IsReadOnly);

            editor.InsertTextAtCaret("<ext>");
            Assert.Equal("<file-name>", editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies a bad template shows an error and JumpToError selects the span.
        /// </summary>
        [AvaloniaFact]
        public void JumpToError_SelectsFailingSpan()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor();
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            editor.Text = "x <does-not-exist> y";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.ViewModel.HasError);
            Assert.Contains("Unknown formatter token", editor.ViewModel.ErrorMessage);

            editor.JumpToError();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(editor.Text.IndexOf('<'), box.SelectionStart);
            Assert.Equal(editor.Text.IndexOf('<') + "<does-not-exist>".Length, _SelectionEnd(box));

            window.Close();
        }

        /// <summary>
        /// Verifies tapping a filtered catalog leaf inserts its default text (keyboard highlight alone must not).
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Tapped_InsertsCatalogEntry()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            editor.TokenPickerViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = _RequireInsertList(editor);
            Assert.False(editor.TokenPickerViewModel.IsGrouped);
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            var leaf = Assert.Single(
                editor.TokenPickerViewModel.VisibleItems,
                n => n.Entry?.CanonicalName == "file-name"
            );
            list.ScrollIntoView(leaf);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = Assert.IsType<TreeViewItem>(list.ContainerFromItem(leaf));
            container.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);
            Assert.False(editor.ViewModel.HasError);
            Assert.True(editor.TokenPickerViewModel.IsGrouped);

            window.Close();
        }

        /// <summary>
        /// Verifies empty-search picker shows nested group folders and leaf tap still inserts.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Grouped_TappedLeaf_InsertsCatalogEntry()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            Assert.True(editor.TokenPickerViewModel.IsGrouped);

            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            var leaf = Assert.Single(fileNameGroup.Children, n => n.Entry?.CanonicalName == "file-name");
            var entry = leaf.Entry!;

            list.ScrollIntoView(fileNameGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var groupContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            groupContainer.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var leafContainer = Assert.IsType<TreeViewItem>(groupContainer.ContainerFromItem(leaf));
            leafContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies tapping a group folder expands/collapses it and does not insert text.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Grouped_TappedGroup_ExpandsAndDoesNotInsert()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            Assert.True(fileNameGroup.IsGroup);

            list.ScrollIntoView(fileNameGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var groupContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            Assert.False(groupContainer.IsExpanded);

            groupContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(groupContainer.IsExpanded);
            Assert.Equal(string.Empty, editor.Text);
            Assert.True(editor.TokenPickerViewModel.IsGrouped);

            groupContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(groupContainer.IsExpanded);
            Assert.Equal(string.Empty, editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies expanding a group folder collapses other folders at the same level.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Grouped_TappedGroup_CollapsesOtherGroups()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            var audioGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "Audio");

            list.ScrollIntoView(fileNameGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var fileNameContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));

            fileNameContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            list.ScrollIntoView(audioGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var audioContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(audioGroup));
            fileNameContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            Assert.True(fileNameContainer.IsExpanded);
            Assert.False(audioContainer.IsExpanded);

            audioContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            audioContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(audioGroup));
            fileNameContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            Assert.True(audioContainer.IsExpanded);
            Assert.False(fileNameContainer.IsExpanded);
            Assert.Equal(string.Empty, editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies expanding a nested folder collapses sibling folders, not the parent.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Grouped_ExpandingNested_CollapsesSiblingNotParent()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            var audioGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "Audio");
            var tagGroup = Assert.Single(audioGroup.Children, n => n.Title == "Tag");
            var mp3Group = Assert.Single(audioGroup.Children, n => n.Title == "MP3");

            list.ScrollIntoView(audioGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var audioContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(audioGroup));
            audioContainer.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var tagContainer = Assert.IsType<TreeViewItem>(audioContainer.ContainerFromItem(tagGroup));
            tagContainer.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var mp3Container = Assert.IsType<TreeViewItem>(audioContainer.ContainerFromItem(mp3Group));
            mp3Container.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            audioContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(audioGroup));
            tagContainer = Assert.IsType<TreeViewItem>(audioContainer.ContainerFromItem(tagGroup));
            mp3Container = Assert.IsType<TreeViewItem>(audioContainer.ContainerFromItem(mp3Group));
            Assert.True(audioContainer.IsExpanded);
            Assert.True(mp3Container.IsExpanded);
            Assert.False(tagContainer.IsExpanded);

            window.Close();
        }

        /// <summary>
        /// Verifies opening the insert flyout focuses the search box.
        /// </summary>
        [AvaloniaFact]
        public void InsertFlyout_Opened_FocusesSearchBox()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var search = _RequireInsertSearchBox(editor);
            Assert.True(search.IsFocused);

            window.Close();
        }

        /// <summary>
        /// Verifies the insert picker uses compact app list chrome (white panel, not tooltip flyout chrome).
        /// </summary>
        [AvaloniaFact]
        public void InsertList_UsesCompactAppChrome()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            Assert.Equal(new Thickness(0), list.BorderThickness);
            Assert.Equal(
                ScrollBarVisibility.Disabled,
                list.GetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty)
            );
            Assert.False(list.GetValue(ScrollViewer.AllowAutoHideProperty));

            var presenter = list.FindAncestorOfType<FlyoutPresenter>();
            Assert.NotNull(presenter);
            var app = Application.Current;
            Assert.NotNull(app);
            Assert.True(app.TryGetResource("AppChromeSurfaceBrush", app.ActualThemeVariant, out var rowBrush));
            var panelBrush = Assert.IsAssignableFrom<ISolidColorBrush>(rowBrush);
            var presenterBrush = Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Background);
            Assert.Equal(panelBrush.Color, presenterBrush.Color);
            var listBrush = Assert.IsAssignableFrom<ISolidColorBrush>(list.Background);
            Assert.Equal(panelBrush.Color, listBrush.Color);

            var group = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            list.ScrollIntoView(group);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var groupContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(group));
            var layoutRoot = Assert.Single(
                groupContainer.GetVisualDescendants().OfType<Border>(),
                border => border.Name == "PART_LayoutRoot"
            );
            Assert.Equal(6, layoutRoot.Padding.Left);

            window.Close();
        }

        /// <summary>
        /// Verifies group folders are SemiBold so they read as categories, not insertable tokens.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_GroupTitle_IsSemiBoldDistinctFromLeaves()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            list.ScrollIntoView(fileNameGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var groupContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            var groupTitle = Assert.Single(
                groupContainer.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "File Name"
            );
            Assert.Equal(FontWeight.SemiBold, groupTitle.FontWeight);

            groupContainer.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var leaf = Assert.Single(fileNameGroup.Children, n => n.Entry?.CanonicalName == "file-name");
            var leafContainer = Assert.IsType<TreeViewItem>(groupContainer.ContainerFromItem(leaf));
            var leafTitle = Assert.Single(
                leafContainer.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == leaf.Title
            );
            Assert.Equal(FontWeight.Normal, leafTitle.FontWeight);

            window.Close();
        }

        /// <summary>
        /// Verifies the insert picker vertical scrollbar uses expanded Fluent chrome, not overlay thumbs.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_UsesExpandedVerticalScrollbar()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            list.MaxHeight = 80;
            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            list.ScrollIntoView(fileNameGroup);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var groupContainer = Assert.IsType<TreeViewItem>(list.ContainerFromItem(fileNameGroup));
            groupContainer.IsExpanded = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var root = Assert.IsAssignableFrom<Visual>(list.GetVisualRoot());
            var scrollBar = root.GetVisualDescendants()
                .OfType<ScrollBar>()
                .First(bar => bar.Orientation == Orientation.Vertical && bar.IsVisible);
            var scrollBarSize = (double)Application.Current!.FindResource("ScrollBarSize")!;
            Assert.False(scrollBar.AllowAutoHide);
            Assert.True(scrollBar.IsExpanded);
            Assert.Equal(scrollBarSize, scrollBar.Bounds.Width, precision: 0);

            window.Close();
        }

        /// <summary>
        /// Verifies Enter on the list inserts the highlighted leaf without relying on SelectionChanged.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Enter_InsertsHighlightedLeaf()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            editor.TokenPickerViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = _RequireInsertList(editor);
            var leaf = Assert.Single(
                editor.TokenPickerViewModel.VisibleItems,
                n => n.Entry?.CanonicalName == "file-name"
            );
            var entry = leaf.Entry!;

            list.SelectedItem = leaf;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(string.Empty, editor.Text);

            _RaiseKeyDown(list, Key.Enter);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies Enter on the search box inserts the highlighted leaf.
        /// </summary>
        [AvaloniaFact]
        public void InsertSearchBox_Enter_InsertsHighlightedLeaf()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            editor.TokenPickerViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = _RequireInsertList(editor);
            var search = _RequireInsertSearchBox(editor);
            var leaf = Assert.Single(
                editor.TokenPickerViewModel.VisibleItems,
                n => n.Entry?.CanonicalName == "file-name"
            );
            var entry = leaf.Entry!;

            list.SelectedItem = leaf;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(string.Empty, editor.Text);

            _RaiseKeyDown(search, Key.Enter);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Enter on a highlighted group folder does not insert.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Enter_OnGroup_DoesNotInsert()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = _RequireInsertList(editor);
            var fileNameGroup = Assert.Single(editor.TokenPickerViewModel.VisibleItems, n => n.Title == "File Name");
            Assert.True(fileNameGroup.IsGroup);

            list.SelectedItem = fileNameGroup;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            _RaiseKeyDown(list, Key.Enter);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(string.Empty, editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies double-tap on a validated token selects that span.
        /// </summary>
        [AvaloniaFact]
        public void Template_DoubleTap_SelectsTokenSpan()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = "pre<file-name>post" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            var tokenStart = editor.Text.IndexOf('<');
            box.CaretOffset = tokenStart + 1;
            box.Select(tokenStart + 1, 0);

            box.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(tokenStart, box.SelectionStart);
            Assert.Equal(tokenStart + "<file-name>".Length, _SelectionEnd(box));

            window.Close();
        }

        /// <summary>
        /// Verifies constructing the control with catalog-backed picker does not throw.
        /// </summary>
        [AvaloniaFact]
        public void Construct_ShowsWithoutThrowing()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = FormatTokenCatalog.Entries[0].InsertText };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.NotEmpty(editor.TokenPickerViewModel.VisibleItems);
            Assert.True(editor.TokenPickerViewModel.IsGrouped);
            Assert.False(editor.ViewModel.HasError);
            Assert.Same(
                AppChromeFonts.AppChromeFixedWidthFamily,
                editor.FindControl<TextEditor>("TemplateBox")?.FontFamily
            );
            Assert.Equal(
                "Right-click a formatting parameter to customize it.",
                editor.FindControl<FilterEditorHint>("RightClickHint")?.Text
            );
            window.Close();
        }

        /// <summary>
        /// Verifies Insert and Edit are matching square glyph buttons on one horizontal row.
        /// </summary>
        [AvaloniaFact]
        public void ToolButtons_AreSameSize()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor();
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var insert = editor.FindControl<Button>("InsertButton");
            var edit = editor.FindControl<Button>("EditButton");
            Assert.NotNull(insert);
            Assert.NotNull(edit);
            Assert.Equal(insert.Bounds.Size, edit.Bounds.Size);
            Assert.Equal(insert.Bounds.Y, edit.Bounds.Y, precision: 0);
            Assert.True(edit.Bounds.X > insert.Bounds.X);
            Assert.Contains(insert.GetVisualDescendants().OfType<PathIcon>(), icon => icon.Width == 12);
            Assert.Contains(edit.GetVisualDescendants().OfType<PathIcon>(), icon => icon.Width == 12);

            window.Close();
        }

        /// <summary>
        /// Verifies multiline AcceptsReturn applies min/max height caps for auto-grow.
        /// </summary>
        [AvaloniaFact]
        public void AcceptsReturnTrue_AppliesAutoGrowMinAndMaxHeight()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { AcceptsReturn = true };
            var window = new Window
            {
                Width = 320,
                Height = 400,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(App.Ui.Views.FormatEditor.FormatEditor.MultilineMinHeight, box.MinHeight);
            Assert.Equal(App.Ui.Views.FormatEditor.FormatEditor.MultilineMaxHeight, box.MaxHeight);
            Assert.True(box.WordWrap);

            editor.Text = string.Concat(
                Enumerable.Repeat("<file-name> <counter:initial=1,step=1> long-token-string ", 20)
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            editor.UpdateAutoGrowHeightForTests();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(box.Height >= App.Ui.Views.FormatEditor.FormatEditor.MultilineMinHeight);
            Assert.True(box.Height <= App.Ui.Views.FormatEditor.FormatEditor.MultilineMaxHeight);

            window.Close();
        }

        /// <summary>
        /// Verifies AcceptsReturn=false starts compact, wraps for display, and auto-grows with a ceiling.
        /// </summary>
        [AvaloniaFact]
        public void AcceptsReturnFalse_WrapsAndAutoGrowsWithCap()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { AcceptsReturn = false };
            var window = new Window
            {
                Width = 320,
                Height = 400,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(App.Ui.Views.FormatEditor.FormatEditor.SingleLineMinHeight, box.MinHeight);
            Assert.Equal(App.Ui.Views.FormatEditor.FormatEditor.MultilineMaxHeight, box.MaxHeight);
            Assert.True(box.WordWrap);

            editor.Text = string.Concat(Enumerable.Repeat("<id3v2:TIT2> <file-name> long-token-string ", 20));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            editor.UpdateAutoGrowHeightForTests();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(box.Height >= App.Ui.Views.FormatEditor.FormatEditor.SingleLineMinHeight);
            Assert.True(box.Height <= App.Ui.Views.FormatEditor.FormatEditor.MultilineMaxHeight);

            window.Close();
        }

        /// <summary>
        /// Verifies AcceptsReturn=false centers a short line vertically via equal spare padding.
        /// </summary>
        [AvaloniaFact]
        public void AcceptsReturnFalse_CentersShortTextVertically()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor
            {
                AcceptsReturn = false,
                Text = "<file-name>",
            };
            var window = new Window
            {
                Width = 320,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            editor.UpdateAutoGrowHeightForTests();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(
                App.Ui.Views.FormatEditor.FormatEditor.SingleLineMinHeight,
                box.Height,
                precision: 0
            );
            Assert.Equal(box.Padding.Top, box.Padding.Bottom, precision: 0);
            Assert.True(box.Padding.Top >= App.Ui.Views.FormatEditor.FormatEditor.TemplateMinVerticalPadding);
            Assert.Equal(ScrollBarVisibility.Disabled, box.VerticalScrollBarVisibility);

            window.Close();
        }

        /// <summary>
        /// Verifies empty single-line FormatEditor does not show a vertical scrollbar.
        /// </summary>
        [AvaloniaFact]
        public void AcceptsReturnFalse_Empty_HidesVerticalScrollbar()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { AcceptsReturn = false, Text = string.Empty };
            var window = new Window
            {
                Width = 320,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            editor.UpdateAutoGrowHeightForTests();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(ScrollBarVisibility.Disabled, box.VerticalScrollBarVisibility);
            Assert.True(box.Height < App.Ui.Views.FormatEditor.FormatEditor.MultilineMaxHeight);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit under caret for counter replaces the span with the editor result.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_Counter_ReplacesSpan()
        {
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "counter");
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = "pre" + entry.InsertText + "post" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            var tokenStart = editor.Text.IndexOf('<');
            box.CaretOffset = tokenStart + 2;
            box.Select(tokenStart + 2, 0);

            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counter =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counter.Initial = 5;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.StartsWith("pre<", editor.Text);
            Assert.Contains("initial=5", editor.Text);
            Assert.EndsWith(">post", editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies right-click selects the token under the pointer, not the caret token.
        /// </summary>
        [AvaloniaFact]
        public void RightClick_SelectsClickedTokenNotCaret()
        {
            var (_, box, window, fileName, counter) = _ShowTwoTokenEditor();
            box.CaretOffset = fileName.Start + 2;
            box.Select(fileName.Start + 2, 0);
            box.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var clickIndex = counter.Start + 2;
            var windowPoint = _CharacterWindowPoint(window, box, clickIndex);
            window.MouseMove(windowPoint);
            window.MouseDown(windowPoint, MouseButton.Right);

            Assert.Equal(counter.Start, box.SelectionStart);
            Assert.Equal(counter.Start + counter.Length, _SelectionEnd(box));

            window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Verifies right-click on a token cancels Avalonia's text context flyout (no Cut/Copy/Paste over the dialog).
        /// </summary>
        [AvaloniaFact]
        public void RightClick_OnToken_SuppressesTextContextFlyout()
        {
            var (_, box, window, _, counter) = _ShowTwoTokenEditor();
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuItem { Header = "Paste" });
            box.TextArea.ContextFlyout = flyout;
            box.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var windowPoint = _CharacterWindowPoint(window, box, counter.Start + 2);
            window.MouseMove(windowPoint);
            window.MouseDown(windowPoint, MouseButton.Right);
            window.MouseUp(windowPoint, MouseButton.Right);

            Assert.Equal(counter.Start, box.SelectionStart);
            Assert.Equal(counter.Start + counter.Length, _SelectionEnd(box));
            Assert.False(flyout.IsOpen);

            // Close before draining the posted token-dialog open so ShowDialog does not block headless.
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Verifies token selection stays when focus moves to the Edit button.
        /// </summary>
        [AvaloniaFact]
        public void TemplateBox_KeepsTokenSelectionAfterLostFocus()
        {
            var (editor, box, window, _, counter) = _ShowTwoTokenEditor();

            Assert.True(editor.EditTokenAtIndexForTests(counter.Start + 2, accept: false));
            Assert.Equal(counter.Start, box.SelectionStart);
            Assert.Equal(counter.Start + counter.Length, _SelectionEnd(box));

            var editButton = editor.FindControl<Button>("EditButton");
            Assert.NotNull(editButton);
            editButton.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(box.IsFocused);
            Assert.Equal(counter.Start, box.SelectionStart);
            Assert.Equal(counter.Start + counter.Length, _SelectionEnd(box));

            window.Close();
        }

        /// <summary>
        /// Verifies Edit at a click index replaces that token while the caret sits on another.
        /// </summary>
        [AvaloniaFact]
        public void EditTokenAtIndex_ReplacesClickedTokenNotCaret()
        {
            var (editor, box, window, fileName, counter) = _ShowTwoTokenEditor();
            box.CaretOffset = fileName.Start + 2;
            box.Select(fileName.Start + 2, 0);

            Assert.True(
                editor.EditTokenAtIndexForTests(
                    counter.Start + 2,
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 9;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("<file-name>", editor.Text);
            Assert.Contains("initial=9", editor.Text);
            Assert.DoesNotContain("initial=1", editor.Text);
            Assert.False(editor.ViewModel.HasError);

            window.Close();
        }

        /// <summary>
        /// Verifies caret on the second of two adjacent tokens edits that token, not the previous <c>&gt;</c>.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_AdjacentTokens_CaretOnSecondSelectsSecond()
        {
            var (editor, box, window, fileName, counter) = _ShowTwoTokenEditor(
                "pre<file-name><counter:initial=1,step=1>post"
            );
            box.CaretOffset = counter.Start;
            box.Select(counter.Start, 0);

            Assert.Equal(fileName.Start + fileName.Length, counter.Start);
            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 4;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("<file-name>", editor.Text);
            Assert.Contains("initial=4", editor.Text);
            Assert.DoesNotContain("initial=1", editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit under caret falls back to the previous token when the caret is after its exclusive end.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_CaretAfterToken_EditsPreviousToken()
        {
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "counter");
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = entry.InsertText + "_backup" };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var counter = Assert.Single(editor.ViewModel.LastParseResult!.Tokens);
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            // Exclusive end (post-insert) and deeper into the literal both fall back left.
            box.CaretOffset = counter.Start + counter.Length;
            box.Select(box.CaretOffset, 0);
            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 8;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("initial=8", editor.Text);
            Assert.EndsWith("_backup", editor.Text);

            box.CaretOffset = editor.Text.Length;
            box.Select(box.CaretOffset, 0);
            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 9;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("initial=9", editor.Text);
            Assert.EndsWith("_backup", editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit under caret between two tokens prefers the left token, not the right.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_CaretBetweenTokens_EditsLeftToken()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor
            {
                Text = "<counter:initial=1,step=1>mid<counter:initial=2,step=1>",
            };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var tokens = editor.ViewModel.LastParseResult?.Tokens;
            Assert.NotNull(tokens);
            Assert.Equal(2, tokens.Count);
            var left = tokens[0];
            var right = tokens[1];
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = left.Start + left.Length + 1;
            box.Select(box.CaretOffset, 0);
            Assert.True(box.CaretOffset < right.Start);

            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 6;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("initial=6", editor.Text);
            Assert.Contains("initial=2", editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies Edit under caret still warns when no token exists to the left of the caret.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_CaretBeforeFirstToken_ReturnsFalse()
        {
            var (editor, box, window, _, _) = _ShowTwoTokenEditor();
            box.CaretOffset = 0;
            box.Select(0, 0);

            Assert.False(editor.EditUnderCaretForTests(accept: false));

            window.Close();
        }

        /// <summary>
        /// Verifies right-click on adjacent tokens selects the glyph under the pointer (MFR7 exclusive end).
        /// </summary>
        [AvaloniaFact]
        public void RightClick_AdjacentTokens_SelectsGlyphToken()
        {
            var (_, box, window, fileName, _) = _ShowTwoTokenEditor("pre<file-name><counter:initial=1,step=1>post");
            box.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var greaterThanPoint = _CharacterWindowPoint(window, box, fileName.Start + fileName.Length - 1);
            window.MouseMove(greaterThanPoint);
            window.MouseDown(greaterThanPoint, MouseButton.Right);
            Assert.Equal(fileName.Start, box.SelectionStart);
            Assert.Equal(fileName.Start + fileName.Length, _SelectionEnd(box));

            window.Close();
            Dispatcher.UIThread.RunJobs();
            FormatTokenSpan? counter;
            (_, box, window, _, counter) = _ShowTwoTokenEditor("pre<file-name><counter:initial=1,step=1>post");
            box.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var lessThanPoint = _CharacterWindowPoint(window, box, counter.Start);
            window.MouseMove(lessThanPoint);
            window.MouseDown(lessThanPoint, MouseButton.Right);
            Assert.Equal(counter.Start, box.SelectionStart);
            Assert.Equal(counter.Start + counter.Length, _SelectionEnd(box));

            window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Verifies valid tokens before an error remain available for highlight after validation fails.
        /// </summary>
        [AvaloniaFact]
        public void Validate_GoodThenUnknown_KeepsPriorTokenSpans()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = "Track: <file-name> <does-not-exist>" };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.ViewModel.HasError);
            var tokens = editor.ViewModel.LastParseResult?.Tokens;
            Assert.NotNull(tokens);
            Assert.Single(tokens);
            Assert.Equal("file-name", tokens[0].CanonicalName);
            Assert.Equal("file-name", tokens[0].WrittenName);

            window.Close();
        }

        /// <summary>
        /// Verifies soft zebra token chip + name accent brushes resolve.
        /// </summary>
        [AvaloniaFact]
        public void Theme_ResolvesTokenBackgroundBrushes()
        {
            var app = Application.Current;
            Assert.NotNull(app);
            Assert.True(app.TryGetResource("FormatTokenBackgroundBrush", app.ActualThemeVariant, out var tokenBg));
            Assert.True(
                app.TryGetResource("FormatTokenAltBackgroundBrush", app.ActualThemeVariant, out var tokenAltBg)
            );
            Assert.True(app.TryGetResource("FormatTokenNameForegroundBrush", app.ActualThemeVariant, out var nameFg));
            Assert.True(app.TryGetResource("FormatTokenErrorBackgroundBrush", app.ActualThemeVariant, out var errorBg));
            Assert.IsAssignableFrom<ISolidColorBrush>(tokenBg);
            Assert.IsAssignableFrom<ISolidColorBrush>(tokenAltBg);
            Assert.IsAssignableFrom<ISolidColorBrush>(nameFg);
            Assert.IsAssignableFrom<ISolidColorBrush>(errorBg);
            Assert.False(app.TryGetResource("FormatTokenDelimiterForegroundBrush", app.ActualThemeVariant, out _));
            Assert.False(app.TryGetResource("FormatTokenForegroundBrush", app.ActualThemeVariant, out _));
        }

        /// <summary>
        /// Verifies Edit still targets a prior-good token when a later unknown token fails validation.
        /// </summary>
        [AvaloniaFact]
        public void EditUnderCaret_GoodThenUnknown_EditsPriorToken()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor
            {
                Text = "pre<counter:initial=1,step=1> <does-not-exist>",
            };
            var window = new Window
            {
                Width = 480,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.ViewModel.HasError);
            var counter = Assert.Single(editor.ViewModel.LastParseResult!.Tokens, t => t.CanonicalName == "counter");
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = counter.Start + 2;
            box.Select(counter.Start + 2, 0);

            Assert.True(
                editor.EditUnderCaretForTests(
                    accept: true,
                    mutate: vm =>
                    {
                        var counterVm =
                            Assert.IsType<App.Ui.ViewModels.FormatEditor.TokenEditors.CounterFormatTokenEditorViewModel>(
                                vm
                            );
                        counterVm.Initial = 7;
                    }
                )
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("initial=7", editor.Text);
            Assert.Contains("<does-not-exist>", editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies empty-field watermark shows only while focused (matches filter TextBox tips).
        /// </summary>
        [AvaloniaFact]
        public void Watermark_EmptyFocused_IsVisible()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Watermark = "<file-name>" };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var watermark = editor.FindControl<TextBlock>("TemplateWatermark");
            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(watermark);
            Assert.NotNull(box);
            Assert.False(watermark.IsVisible);
            Assert.Equal("<file-name>", watermark.Text);

            // AvaloniaEdit focuses TextArea (TemplateBox itself is not Focusable).
            Assert.True(box.TextArea.Focus());
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.True(watermark.IsVisible);

            editor.Text = "x";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.False(watermark.IsVisible);

            editor.Text = string.Empty;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.True(watermark.IsVisible);

            window.Close();
        }

        /// <summary>
        /// Verifies MaxLength truncates Text and insert results.
        /// </summary>
        [AvaloniaFact]
        public void MaxLength_ClampsTextAndInsert()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { MaxLength = 8, Text = "abcdefghij" };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("abcdefgh", editor.Text);
            Assert.Equal("abcdefgh", editor.FindControl<TextEditor>("TemplateBox")?.Text);

            editor.Text = "ab";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = 2;
            box.Select(2, 0);
            editor.InsertTextAtCaret("cdefghijkl");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("abcdefgh", editor.Text);
            Assert.Equal(8, editor.Text.Length);

            window.Close();
        }

        /// <summary>
        /// Verifies MaxLength clamp during an open document update (paste-style) does not throw.
        /// </summary>
        [AvaloniaFact]
        public void MaxLength_ClampsDuringOpenUndoGroup_DoesNotThrow()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { MaxLength = 8, Text = "ab" };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            var document = box.Document;
            Assert.NotNull(document);

            // Mimic paste: insert while BeginUpdate keeps an undo group open through TextChanged.
            document.BeginUpdate();
            document.Insert(2, "cdefghijkl");
            document.EndUpdate();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("abcdefgh", editor.Text);
            Assert.Equal("abcdefgh", box.Text);
            Assert.Equal(8, editor.Text.Length);

            window.Close();
        }

        /// <summary>
        /// Verifies AcceptsReturn=false swallows Enter so single-line hosts do not insert a newline.
        /// </summary>
        [AvaloniaFact]
        public void AcceptsReturnFalse_Enter_DoesNotInsertNewline()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { AcceptsReturn = false, Text = "ab" };
            var window = new Window { Content = editor };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            box.CaretOffset = 1;
            box.Focus();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var args = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                Source = box.TextArea,
            };
            box.TextArea.RaiseEvent(args);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(args.Handled);
            Assert.Equal("ab", editor.Text);
            Assert.DoesNotContain("\n", editor.Text);
            Assert.DoesNotContain("\r", editor.Text);

            window.Close();
        }

        private static (
            App.Ui.Views.FormatEditor.FormatEditor Editor,
            TextEditor Box,
            Window Window,
            FormatTokenSpan FileName,
            FormatTokenSpan Counter
        ) _ShowTwoTokenEditor(string text = "pre<file-name>mid<counter:initial=1,step=1>post")
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor { Text = text };
            var window = new Window
            {
                Width = 640,
                Height = 200,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var box = editor.FindControl<TextEditor>("TemplateBox");
            Assert.NotNull(box);
            var tokens = editor.ViewModel.LastParseResult?.Tokens;
            Assert.NotNull(tokens);
            var fileName = Assert.Single(tokens, t => t.CanonicalName == "file-name");
            var counter = Assert.Single(tokens, t => t.CanonicalName == "counter");
            return (editor, box, window, fileName, counter);
        }

        private static int _SelectionEnd(TextEditor box)
        {
            return box.SelectionStart + box.SelectionLength;
        }

        private static Point _CharacterWindowPoint(Window window, TextEditor box, int charIndex)
        {
            var textView = box.TextArea.TextView;
            textView.EnsureVisualLines();
            var document = box.Document;
            Assert.NotNull(document);
            var location = document.GetLocation(Math.Clamp(charIndex, 0, document.TextLength));
            var visual = textView.GetVisualPosition(new TextViewPosition(location), VisualYPosition.TextMiddle);
            visual -= textView.ScrollOffset;
            var windowPoint = textView.TranslatePoint(visual, window);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }

        private static void _RaiseKeyDown(Control control, Key key)
        {
            control.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = key,
                    Source = control,
                }
            );
        }

        private static (App.Ui.Views.FormatEditor.FormatEditor Editor, Window Window) _ShowWithInsertFlyout()
        {
            var editor = new App.Ui.Views.FormatEditor.FormatEditor();
            var window = new Window
            {
                Width = 480,
                Height = 320,
                Content = editor,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var insertButton = editor.FindControl<Button>("InsertButton");
            Assert.NotNull(insertButton);
            Assert.IsType<Flyout>(insertButton.Flyout);
            ((Flyout)insertButton.Flyout).ShowAt(insertButton);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            return (editor, window);
        }

        /// <summary>
        /// Resolves the insert catalog tree (lives in <see cref="FormatTokenPicker"/>'s name scope).
        /// </summary>
        private static TreeView _RequireInsertList(App.Ui.Views.FormatEditor.FormatEditor editor)
        {
            var list = editor.TokenPickerControl.FindControl<TreeView>("TokenList");
            Assert.NotNull(list);
            return list;
        }

        /// <summary>
        /// Resolves the insert search box inside the shared picker.
        /// </summary>
        private static TextBox _RequireInsertSearchBox(App.Ui.Views.FormatEditor.FormatEditor editor)
        {
            var search = editor.TokenPickerControl.FindControl<TextBox>("TokenSearchBox");
            Assert.NotNull(search);
            return search;
        }
    }
}

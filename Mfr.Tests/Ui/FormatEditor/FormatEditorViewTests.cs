using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            box.CaretIndex = 3;
            box.SelectionStart = 3;
            box.SelectionEnd = 5;

            editor.InsertTextAtCaret("<file-name>");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("pre<file-name>post", editor.Text);
            Assert.False(editor.ViewModel.HasError);

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

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            Assert.Equal(editor.Text.IndexOf('<'), box.SelectionStart);
            Assert.Equal(editor.Text.IndexOf('<') + "<does-not-exist>".Length, box.SelectionEnd);

            window.Close();
        }

        /// <summary>
        /// Verifies tapping a filtered catalog leaf inserts its default text (keyboard highlight alone must not).
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Tapped_InsertsCatalogEntry()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            editor.ViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            Assert.False(editor.ViewModel.IsGrouped);
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            var leaf = Assert.Single(editor.ViewModel.VisibleItems, n => n.Entry?.CanonicalName == "file-name");
            list.ScrollIntoView(leaf);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = Assert.IsType<TreeViewItem>(list.ContainerFromItem(leaf));
            container.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(entry.InsertText, editor.Text);
            Assert.False(editor.ViewModel.HasError);
            Assert.True(editor.ViewModel.IsGrouped);

            window.Close();
        }

        /// <summary>
        /// Verifies empty-search picker shows nested group folders and leaf tap still inserts.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Grouped_TappedLeaf_InsertsCatalogEntry()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            Assert.True(editor.ViewModel.IsGrouped);

            var fileNameGroup = Assert.Single(editor.ViewModel.VisibleItems, n => n.Title == "File Name");
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

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            var fileNameGroup = Assert.Single(editor.ViewModel.VisibleItems, n => n.Title == "File Name");
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
            Assert.True(editor.ViewModel.IsGrouped);

            groupContainer.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.False(groupContainer.IsExpanded);
            Assert.Equal(string.Empty, editor.Text);

            window.Close();
        }

        /// <summary>
        /// Verifies opening the insert flyout focuses the search box.
        /// </summary>
        [AvaloniaFact]
        public void InsertFlyout_Opened_FocusesSearchBox()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var search = editor.FindControl<TextBox>("InsertSearchBox");
            Assert.NotNull(search);
            Assert.True(search.IsFocused);

            window.Close();
        }

        /// <summary>
        /// Verifies the insert picker uses compact app list chrome instead of Fluent TreeView defaults.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_UsesCompactAppChrome()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            Assert.Equal(new Thickness(0), list.BorderThickness);
            Assert.Equal(
                ScrollBarVisibility.Disabled,
                list.GetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty)
            );

            window.Close();
        }

        /// <summary>
        /// Verifies Enter on the list inserts the highlighted leaf without relying on SelectionChanged.
        /// </summary>
        [AvaloniaFact]
        public void InsertList_Enter_InsertsHighlightedLeaf()
        {
            var (editor, window) = _ShowWithInsertFlyout();

            editor.ViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            var leaf = Assert.Single(editor.ViewModel.VisibleItems, n => n.Entry?.CanonicalName == "file-name");
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

            editor.ViewModel.SearchText = "file-name";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = editor.FindControl<TreeView>("InsertList");
            var search = editor.FindControl<TextBox>("InsertSearchBox");
            Assert.NotNull(list);
            Assert.NotNull(search);
            var leaf = Assert.Single(editor.ViewModel.VisibleItems, n => n.Entry?.CanonicalName == "file-name");
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

            var list = editor.FindControl<TreeView>("InsertList");
            Assert.NotNull(list);
            var fileNameGroup = Assert.Single(editor.ViewModel.VisibleItems, n => n.Title == "File Name");
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

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            var tokenStart = editor.Text.IndexOf('<');
            box.CaretIndex = tokenStart + 1;
            box.SelectionStart = tokenStart + 1;
            box.SelectionEnd = tokenStart + 1;

            box.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(tokenStart, box.SelectionStart);
            Assert.Equal(tokenStart + "<file-name>".Length, box.SelectionEnd);

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

            Assert.NotEmpty(editor.ViewModel.VisibleItems);
            Assert.True(editor.ViewModel.IsGrouped);
            Assert.False(editor.ViewModel.HasError);
            window.Close();
        }

        /// <summary>
        /// Verifies Insert and Edit are matching square glyph buttons.
        /// </summary>
        [AvaloniaFact]
        public void InsertAndEditButtons_AreSameSize()
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
            Assert.Contains(insert.GetVisualDescendants().OfType<PathIcon>(), icon => icon.Width == 12);
            Assert.Contains(edit.GetVisualDescendants().OfType<PathIcon>(), icon => icon.Width == 12);

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

            var box = editor.FindControl<TextBox>("TemplateBox");
            Assert.NotNull(box);
            var tokenStart = editor.Text.IndexOf('<');
            box.CaretIndex = tokenStart + 2;
            box.SelectionStart = tokenStart + 2;
            box.SelectionEnd = tokenStart + 2;

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
    }
}

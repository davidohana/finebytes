using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Misc;
using Mfr.Filters.Misc;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Headless tests for <see cref="MoverFilterEditorView"/>.
    /// </summary>
    public sealed class MoverFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Mover option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Mover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<MoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<MoverFilterEditorView>().Single();
            var rootFolder = editor.FindControl<TextBox>("RootFolderBox");
            var subFolder = editor.FindControl<TextBox>("SubFolderBox");
            var browse = editor.FindControl<HyperlinkButton>("BrowseRootButton");
            Assert.NotNull(rootFolder);
            Assert.NotNull(subFolder);
            Assert.NotNull(browse);
            Assert.Equal(@"C:\", rootFolder.Text);
            Assert.Equal("MFR", subFolder.Text);
            Assert.Same(
                (
                    (MoverFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor
                ).BrowseRootFolderCommand,
                browse.Command
            );

            rootFolder.Text = @"E:\Archive";
            subFolder.Text = "<now:yyyy>";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (MoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(@"E:\Archive", filter.Options.RootFolder);
            Assert.Equal("<now:yyyy>", filter.Options.SubFolder);

            window.Close();
        }

        private static (
            Window Window,
            MainWindowViewModel MainViewModel,
            FilterEditorView EditorView
        ) _ShowFilterEditorPanes()
        {
            var mainViewModel = new MainWindowViewModel();
            var appliedView = new AppliedFiltersView
            {
                DataContext = mainViewModel.AppliedFiltersViewModel,
                AddFromPaletteCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
            };
            var editorView = new FilterEditorView { DataContext = mainViewModel.FilterEditorViewModel };

            var grid = new Grid { RowDefinitions = new RowDefinitions("*,*"), Children = { appliedView, editorView } };
            Grid.SetRow(editorView, 1);

            var window = new Window
            {
                Width = 320,
                Height = 280,
                Content = grid,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            return (window, mainViewModel, editorView);
        }
    }
}

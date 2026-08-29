using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for the Applied Filters list UI.
    /// </summary>
    public sealed class AppliedFiltersViewTests
    {
        /// <summary>
        /// Verifies seeded steps render display name and Apply-To subtitle in the list.
        /// </summary>
        [AvaloniaFact]
        public void Seeded_steps_render_in_list()
        {
            var (window, viewModel, list) = _ShowSeededList();

            Assert.Equal(2, list.ItemCount);
            Assert.Equal("Shrink Spaces", _RowDisplayName(list, 0));
            Assert.Equal("File Prefix", _RowApplyToLabel(list, 0));
            Assert.Equal("Letters Case", _RowDisplayName(list, 1));
            Assert.Equal("File Prefix", _RowApplyToLabel(list, 1));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);
            Assert.Single(list.Selection.SelectedIndexes);

            window.Close();
        }

        /// <summary>
        /// Verifies clicking the row checkbox updates the step and <see cref="AppliedFiltersViewModel.ToChain"/>.
        /// </summary>
        [AvaloniaFact]
        public void Checkbox_click_toggles_enabled_on_step_and_chain()
        {
            var (window, viewModel, list) = _ShowSeededList();
            var step = viewModel.Steps[0];
            Assert.True(step.Enabled);
            Assert.True(viewModel.ToChain().Steps[0].Enabled);

            _ClickRowCheckBox(window, list, rowIndex: 0);

            Assert.False(step.Enabled);
            Assert.False(viewModel.ToChain().Steps[0].Enabled);
            Assert.True(viewModel.ToChain().Steps[1].Enabled);

            window.Close();
        }

        /// <summary>
        /// Verifies the main window status bar reflects the applied-filter count.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_FilterCount_tracks_applied_steps()
        {
            var viewModel = new MainWindowViewModel();
            var window = new Window
            {
                Width = 480,
                Height = 320,
                DataContext = viewModel,
                Content = new AppliedFiltersView { DataContext = viewModel.AppliedFiltersViewModel },
            };

            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, viewModel.FilterCount);

            viewModel.AppliedFiltersViewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, viewModel.FilterCount);
            Assert.Equal(1, viewModel.AppliedFiltersViewModel.Count);

            window.Close();
        }

        private static (Window Window, AppliedFiltersViewModel ViewModel, ListBox List) _ShowSeededList()
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(_Entry("LettersCase"));

            var view = new AppliedFiltersView { DataContext = viewModel };
            var window = new Window
            {
                Width = 280,
                Height = 220,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = view.FindControl<ListBox>("AppliedFiltersList");
            Assert.NotNull(list);
            return (window, viewModel, list);
        }

        private static string _RowDisplayName(ListBox list, int rowIndex)
        {
            return _RowTextBlock(list, rowIndex, blockIndex: 0);
        }

        private static string _RowApplyToLabel(ListBox list, int rowIndex)
        {
            return _RowTextBlock(list, rowIndex, blockIndex: 1);
        }

        private static string _RowTextBlock(ListBox list, int rowIndex, int blockIndex)
        {
            var container = list.ContainerFromIndex(rowIndex) as Visual;
            Assert.NotNull(container);

            var textBlocks = container.GetVisualDescendants().OfType<TextBlock>().ToList();
            Assert.True(textBlocks.Count > blockIndex);
            return textBlocks[blockIndex].Text ?? string.Empty;
        }

        private static void _ClickRowCheckBox(Window window, ListBox list, int rowIndex)
        {
            var container = list.ContainerFromIndex(rowIndex) as Visual;
            Assert.NotNull(container);

            var checkBox = container.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
            Assert.NotNull(checkBox);

            var local = new Point(Math.Max(2, checkBox.Bounds.Width / 2), Math.Max(2, checkBox.Bounds.Height / 2));
            var windowPoint = checkBox.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);

            window.MouseMove(windowPoint.Value);
            window.MouseDown(windowPoint.Value, MouseButton.Left);
            window.MouseUp(windowPoint.Value, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        private static FilterCatalogEntry _Entry(string type)
        {
            return FilterCatalog.Entries.Single(entry => entry.Type == type);
        }
    }
}

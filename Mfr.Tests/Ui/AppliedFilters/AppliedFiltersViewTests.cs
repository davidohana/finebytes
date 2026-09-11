using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views.AppliedFilters;

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
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList();

            Assert.Equal(2, list.ItemCount);
            Assert.Equal("Shrink Spaces", _RowDisplayName(list, 0));
            Assert.Equal("File Name", AppliedFiltersTestUi.RowApplyToLabel(list, 0));
            Assert.Equal("Letters Case", _RowDisplayName(list, 1));
            Assert.Equal("File Name", AppliedFiltersTestUi.RowApplyToLabel(list, 1));
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
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList();
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

            viewModel.AppliedFiltersViewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, viewModel.FilterCount);
            Assert.Equal(1, viewModel.AppliedFiltersViewModel.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies the Applied Filters list exposes the context menu with expected headers.
        /// </summary>
        [AvaloniaFact]
        public void List_Has_ContextMenu_With_Expected_Items()
        {
            var (window, _, list, _) = AppliedFiltersTestUi.ShowSeededList();

            Assert.NotNull(list.ContextMenu);
            var headers = list.ContextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();
            Assert.Equal(
                [
                    "Check All Filters",
                    "Uncheck All Filters",
                    "Invert Check",
                    "Remove Selected Filter",
                    "Remove All But Selected",
                    "Remove All Filters",
                    "Filter Options",
                ],
                headers
            );

            window.Close();
        }

        /// <summary>
        /// Verifies a context request on an unselected row selects that row (control + VM).
        /// </summary>
        [AvaloniaFact]
        public void ContextRequest_On_Unselected_Row_Selects_That_Row()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 1);
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);

            var row = list.ContainerFromIndex(0) as ListBoxItem;
            Assert.NotNull(row);
            row.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
            Assert.Equal([0], list.Selection.SelectedIndexes.ToList());

            window.Close();
        }

        /// <summary>
        /// Verifies a context request on a row already in a multi-selection keeps that selection.
        /// </summary>
        [AvaloniaFact]
        public void ContextRequest_On_Selected_Row_Keeps_MultiSelection()
        {
            var (window, viewModel, list, _) = AppliedFiltersTestUi.ShowSeededList(selectIndex: 0);
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1], list.Selection.SelectedIndexes.OrderBy(i => i).ToList());

            var row = list.ContainerFromIndex(1) as ListBoxItem;
            Assert.NotNull(row);
            row.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([viewModel.Steps[0], viewModel.Steps[1]], viewModel.SelectedSteps);
            Assert.Equal([0, 1], list.Selection.SelectedIndexes.OrderBy(i => i).ToList());

            window.Close();
        }

        private static string _RowDisplayName(ListBox list, int rowIndex)
        {
            return _RowTextBlock(list, rowIndex, blockIndex: 0);
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
    }
}

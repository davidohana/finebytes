using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for Applied Filters remove, clear, and reorder gestures.
    /// </summary>
    public sealed class AppliedFiltersRemoveReorderTests
    {
        /// <summary>
        /// Verifies the remove shuttle button deletes the selected step.
        /// </summary>
        [AvaloniaFact]
        public void Remove_button_removes_selected_filter()
        {
            var (window, viewModel, list, view) = _ShowSeededList(selectIndex: 0);

            var removeButton = view.FindControl<Button>("RemoveFromAppliedButton");
            Assert.NotNull(removeButton);
            Assert.NotNull(removeButton.Command);
            Assert.True(removeButton.Command.CanExecute(null));
            removeButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);
            Assert.Equal(1, list.ItemCount);

            window.Close();
        }

        /// <summary>
        /// Verifies Delete on the Applied list removes the selection.
        /// </summary>
        [AvaloniaFact]
        public void Delete_on_applied_list_removes_selected_filter()
        {
            var (window, viewModel, list, _) = _ShowSeededList(selectIndex: 0);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            _PressKeyOnControl(list, Key.Delete);

            Assert.Single(viewModel.Steps);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies Ctrl+Up on the Applied list moves the selection up.
        /// </summary>
        [AvaloniaFact]
        public void Ctrl_up_on_applied_list_moves_selected_filter()
        {
            var (window, viewModel, list, _) = _ShowSeededList(selectIndex: 1);

            list.Focus();
            Dispatcher.UIThread.RunJobs();
            _PressKeyOnControl(list, Key.Up, KeyModifiers.Control);

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[0], viewModel.SelectedSteps[0]);

            window.Close();
        }

        /// <summary>
        /// Verifies the move-down shuttle button reorders the selection.
        /// </summary>
        [AvaloniaFact]
        public void Move_down_button_reorders_selected_filter()
        {
            var (window, viewModel, _, view) = _ShowSeededList(selectIndex: 0);

            var moveDownButton = view.FindControl<Button>("MoveAppliedDownButton");
            Assert.NotNull(moveDownButton);
            Assert.NotNull(moveDownButton.Command);
            Assert.True(moveDownButton.Command.CanExecute(null));
            moveDownButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["Letters Case", "Shrink Spaces"], viewModel.Steps.Select(step => step.DisplayName));
            Assert.Equal(viewModel.Steps[1], viewModel.SelectedSteps[0]);

            window.Close();
        }

        /// <summary>
        /// Verifies the clear shuttle button removes every step.
        /// </summary>
        [AvaloniaFact]
        public void Clear_button_removes_all_filters()
        {
            var (window, viewModel, list, view) = _ShowSeededList(selectIndex: 0);

            var clearButton = view.FindControl<Button>("ClearAppliedButton");
            Assert.NotNull(clearButton);
            Assert.NotNull(clearButton.Command);
            Assert.True(clearButton.Command.CanExecute(null));
            clearButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewModel.Steps);
            Assert.Empty(viewModel.SelectedSteps);
            Assert.Equal(0, list.ItemCount);

            window.Close();
        }

        private static (
            Window Window,
            AppliedFiltersViewModel ViewModel,
            ListBox List,
            AppliedFiltersView View
        ) _ShowSeededList(int selectIndex)
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(_Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(_Entry("LettersCase"));
            viewModel.SetSelectedSteps([viewModel.Steps[selectIndex]]);

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
            return (window, viewModel, list, view);
        }

        private static void _PressKeyOnControl(Control control, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            control.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = key,
                    KeyModifiers = modifiers,
                    Source = control,
                }
            );
            Dispatcher.UIThread.RunJobs();
        }

        private static FilterCatalogEntry _Entry(string type)
        {
            return FilterCatalog.Entries.Single(entry => entry.Type == type);
        }
    }
}

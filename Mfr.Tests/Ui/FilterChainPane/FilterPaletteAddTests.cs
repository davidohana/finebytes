using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.App.Ui.Views.FilterChainPane;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.Filters;

namespace Mfr.Tests.Ui.FilterChainPane
{
    /// <summary>
    /// Headless tests for adding filters from Available Filters to Filter Chain.
    /// </summary>
    public sealed class FilterPaletteAddTests
    {
        /// <summary>
        /// Verifies Enter on the Available list appends the selected catalog row.
        /// </summary>
        [AvaloniaFact]
        public void Enter_on_available_list_appends_selected_filter()
        {
            var (window, mainViewModel, paletteList, filterChainView) = _ShowFilterPanes();
            var shrinkSpaces = FilterChainTestUi.Entry("ShrinkSpaces");
            _SelectPaletteEntry(paletteList, shrinkSpaces);

            FilterChainTestUi.PressKeyOnControl(paletteList, Key.Enter);

            Assert.Single(mainViewModel.FilterChainViewModel.Steps);
            Assert.Equal("Shrink Spaces", mainViewModel.FilterChainViewModel.Steps[0].DisplayName);
            Assert.Equal(1, filterChainView.FindControl<ListBox>("FilterChainList")!.ItemCount);
            Assert.Equal(1, mainViewModel.FilterCount);

            window.Close();
        }

        /// <summary>
        /// Verifies double-click on the Available list appends the selected catalog row.
        /// </summary>
        [AvaloniaFact]
        public void Double_click_on_available_list_appends_selected_filter()
        {
            var (window, mainViewModel, paletteList, _) = _ShowFilterPanes();
            var lettersCase = FilterChainTestUi.Entry("LettersCase");
            _SelectPaletteEntry(paletteList, lettersCase);

            paletteList.RaiseEvent(new RoutedEventArgs(InputElement.DoubleTappedEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.FilterChainViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.FilterChainViewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies the Filter Chain add button appends the palette selection.
        /// </summary>
        [AvaloniaFact]
        public void Filter_chain_add_button_appends_palette_selection()
        {
            var (window, mainViewModel, paletteList, filterChainView) = _ShowFilterPanes();
            var shrinkSpaces = FilterChainTestUi.Entry("ShrinkSpaces");
            _SelectPaletteEntry(paletteList, shrinkSpaces);

            var addButton = filterChainView.FindControl<Button>("AddFromPaletteButton");
            Assert.NotNull(addButton);
            Assert.NotNull(addButton.Command);
            Assert.True(addButton.Command.CanExecute(null));
            addButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.FilterChainViewModel.Steps);
            Assert.Equal("Shrink Spaces", mainViewModel.FilterChainViewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a catalog row from Available Filters inserts at the drop index.
        /// </summary>
        [AvaloniaFact]
        public void Drop_from_available_inserts_filter_at_drop_index()
        {
            var (window, mainViewModel, paletteList, filterChainView) = _ShowFilterPanes();
            var lettersCase = FilterChainTestUi.Entry("LettersCase");
            _SelectPaletteEntry(paletteList, lettersCase);

            var filterChainList = filterChainView.FindControl<ListBox>("FilterChainList");
            Assert.NotNull(filterChainList);

            var payload = new FilterPaletteDragPayload([lettersCase.Type]);
            var dataTransfer = payload.CreateTransfer();

            filterChainList.RaiseEvent(
                new DragEventArgs(DragDrop.DropEvent, dataTransfer, filterChainList, default, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.FilterChainViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.FilterChainViewModel.Steps[0].DisplayName);
            Assert.Equal(
                mainViewModel.FilterChainViewModel.Steps[0],
                mainViewModel.FilterChainViewModel.SelectedSteps[0]
            );
            Assert.Equal(1, mainViewModel.FilterCount);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping an Applied row onto Available Filters removes it from the stack.
        /// </summary>
        [AvaloniaFact]
        public void Drop_from_applied_to_palette_removes_filter()
        {
            var (window, mainViewModel, paletteList, _) = _ShowFilterPanes();
            mainViewModel.FilterChainViewModel.AddCommand.Execute(FilterChainTestUi.Entry("ShrinkSpaces"));
            mainViewModel.FilterChainViewModel.SetSelectedSteps([]);
            mainViewModel.FilterChainViewModel.AddCommand.Execute(FilterChainTestUi.Entry("LettersCase"));

            var payload = new IndicesDragPayload([0]);
            var dataTransfer = payload.CreateTransfer(IndicesDragPayload.FilterChainFormat);

            paletteList.RaiseEvent(
                new DragEventArgs(DragDrop.DropEvent, dataTransfer, paletteList, default, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.FilterChainViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.FilterChainViewModel.Steps[0].DisplayName);
            Assert.Equal(1, mainViewModel.FilterCount);

            window.Close();
        }

        private static (
            Window Window,
            MainWindowViewModel MainViewModel,
            ListBox PaletteList,
            FilterChainView FilterChainView
        ) _ShowFilterPanes()
        {
            var mainViewModel = new MainWindowViewModel();
            var paletteView = new FilterPaletteView
            {
                DataContext = mainViewModel.FilterPaletteViewModel,
                AddSelectedToFilterChainCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
                RemoveFilterChainStepsCommand = mainViewModel.FilterChainViewModel.RemoveStepsAtIndicesCommand,
            };
            var filterChainView = new FilterChainView
            {
                DataContext = mainViewModel.FilterChainViewModel,
                AddFromPaletteCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children = { paletteView, filterChainView },
            };
            Grid.SetColumn(filterChainView, 1);

            var window = new Window
            {
                Width = 560,
                Height = 320,
                Content = grid,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var paletteList = paletteView.FindControl<ListBox>("FilterList");
            Assert.NotNull(paletteList);
            return (window, mainViewModel, paletteList, filterChainView);
        }

        private static void _SelectPaletteEntry(ListBox paletteList, FilterCatalogEntry entry)
        {
            paletteList.SelectedItem = entry;
            paletteList.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(entry, paletteList.SelectedItem);
        }
    }
}

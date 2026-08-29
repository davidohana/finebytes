using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.Filters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Headless tests for adding filters from Available to Applied.
    /// </summary>
    public sealed class FilterPaletteAddTests
    {
        /// <summary>
        /// Verifies Enter on the Available list appends the selected catalog row.
        /// </summary>
        [AvaloniaFact]
        public void Enter_on_available_list_appends_selected_filter()
        {
            var (window, mainViewModel, paletteList, appliedView) = _ShowFilterPanes();
            var shrinkSpaces = AppliedFiltersTestUi.Entry("ShrinkSpaces");
            _SelectPaletteEntry(paletteList, shrinkSpaces);

            AppliedFiltersTestUi.PressKeyOnControl(paletteList, Key.Enter);

            Assert.Single(mainViewModel.AppliedFiltersViewModel.Steps);
            Assert.Equal("Shrink Spaces", mainViewModel.AppliedFiltersViewModel.Steps[0].DisplayName);
            Assert.Equal(1, appliedView.FindControl<ListBox>("AppliedFiltersList")!.ItemCount);
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
            var lettersCase = AppliedFiltersTestUi.Entry("LettersCase");
            _SelectPaletteEntry(paletteList, lettersCase);

            paletteList.RaiseEvent(new RoutedEventArgs(InputElement.DoubleTappedEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.AppliedFiltersViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.AppliedFiltersViewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies the Applied Filters add button appends the palette selection.
        /// </summary>
        [AvaloniaFact]
        public void Applied_add_button_appends_palette_selection()
        {
            var (window, mainViewModel, paletteList, appliedView) = _ShowFilterPanes();
            var shrinkSpaces = AppliedFiltersTestUi.Entry("ShrinkSpaces");
            _SelectPaletteEntry(paletteList, shrinkSpaces);

            var addButton = appliedView.FindControl<Button>("AddFromPaletteButton");
            Assert.NotNull(addButton);
            Assert.NotNull(addButton.Command);
            Assert.True(addButton.Command.CanExecute(null));
            addButton.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.AppliedFiltersViewModel.Steps);
            Assert.Equal("Shrink Spaces", mainViewModel.AppliedFiltersViewModel.Steps[0].DisplayName);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a catalog row from Available Filters inserts at the drop index.
        /// </summary>
        [AvaloniaFact]
        public void Drop_from_available_inserts_filter_at_drop_index()
        {
            var (window, mainViewModel, paletteList, appliedView) = _ShowFilterPanes();
            var lettersCase = AppliedFiltersTestUi.Entry("LettersCase");
            _SelectPaletteEntry(paletteList, lettersCase);

            var appliedList = appliedView.FindControl<ListBox>("AppliedFiltersList");
            Assert.NotNull(appliedList);

            var payload = new FilterPaletteDragPayload([lettersCase.Type]);
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(FilterPaletteDragPayload.Format, payload.Serialize()));

            appliedList.RaiseEvent(
                new DragEventArgs(DragDrop.DropEvent, dataTransfer, appliedList, default, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.AppliedFiltersViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.AppliedFiltersViewModel.Steps[0].DisplayName);
            Assert.Equal(
                mainViewModel.AppliedFiltersViewModel.Steps[0],
                mainViewModel.AppliedFiltersViewModel.SelectedSteps[0]
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
            mainViewModel.AppliedFiltersViewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            mainViewModel.AppliedFiltersViewModel.SetSelectedSteps([]);
            mainViewModel.AppliedFiltersViewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            var payload = new AppliedFilterDragPayload([0]);
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(AppliedFilterDragPayload.Format, payload.Serialize()));

            paletteList.RaiseEvent(
                new DragEventArgs(DragDrop.DropEvent, dataTransfer, paletteList, default, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Single(mainViewModel.AppliedFiltersViewModel.Steps);
            Assert.Equal("Letters Case", mainViewModel.AppliedFiltersViewModel.Steps[0].DisplayName);
            Assert.Equal(1, mainViewModel.FilterCount);

            window.Close();
        }

        private static (
            Window Window,
            MainWindowViewModel MainViewModel,
            ListBox PaletteList,
            AppliedFiltersView AppliedView
        ) _ShowFilterPanes()
        {
            var mainViewModel = new MainWindowViewModel();
            var paletteView = new FilterPaletteView
            {
                DataContext = mainViewModel.FilterPaletteViewModel,
                AddSelectedToAppliedCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
                AppliedFiltersViewModel = mainViewModel.AppliedFiltersViewModel,
            };
            var appliedView = new AppliedFiltersView
            {
                DataContext = mainViewModel.AppliedFiltersViewModel,
                AddFromPaletteCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Children = { paletteView, appliedView },
            };
            Grid.SetColumn(appliedView, 1);

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
            return (window, mainViewModel, paletteList, appliedView);
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

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Shared catalog lookup and headless host for Applied Filters tests.
    /// </summary>
    internal static class AppliedFiltersTestUi
    {
        /// <summary>
        /// Looks up a catalog row by <see cref="FilterCatalogEntry.Type"/>.
        /// </summary>
        /// <param name="type">Catalog type discriminator.</param>
        /// <returns>The matching catalog entry.</returns>
        public static FilterCatalogEntry Entry(string type)
        {
            return FilterCatalog.Entries.Single(entry => entry.Type == type);
        }

        /// <summary>
        /// Shows an Applied Filters view seeded with Shrink Spaces then Letters Case.
        /// </summary>
        /// <param name="selectIndex">Optional row to select after seed; otherwise the last added row stays selected.</param>
        /// <returns>Host window, view model, list, and view.</returns>
        public static (
            Window Window,
            AppliedFiltersViewModel ViewModel,
            ListBox List,
            AppliedFiltersView View
        ) ShowSeededList(int? selectIndex = null)
        {
            var viewModel = new AppliedFiltersViewModel();
            viewModel.AddCommand.Execute(Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(Entry("LettersCase"));
            if (selectIndex is int index)
            {
                viewModel.SetSelectedSteps([viewModel.Steps[index]]);
            }

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

        /// <summary>
        /// Raises a tunneled key-down on <paramref name="control"/>.
        /// </summary>
        /// <param name="control">Target control.</param>
        /// <param name="key">Key.</param>
        /// <param name="modifiers">Key modifiers.</param>
        public static void PressKeyOnControl(Control control, Key key, KeyModifiers modifiers = KeyModifiers.None)
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
    }
}

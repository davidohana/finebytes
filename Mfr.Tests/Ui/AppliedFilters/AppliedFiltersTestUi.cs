using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        /// Reads the Apply-To subtitle from an Applied Filters list row.
        /// </summary>
        /// <param name="list">Applied Filters list.</param>
        /// <param name="rowIndex">Zero-based row index.</param>
        /// <returns>Subtitle text.</returns>
        public static string RowApplyToLabel(ListBox list, int rowIndex)
        {
            var container = list.ContainerFromIndex(rowIndex) as Visual;
            Assert.NotNull(container);

            var textBlocks = container.GetVisualDescendants().OfType<TextBlock>().ToList();
            Assert.True(textBlocks.Count > 1);
            return textBlocks[1].Text ?? string.Empty;
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

        /// <summary>
        /// Clicks an Applied Filters list row (not the checkbox).
        /// </summary>
        /// <param name="window">Host window for pointer routing.</param>
        /// <param name="list">Applied Filters list.</param>
        /// <param name="rowIndex">Zero-based row index.</param>
        public static void ClickRow(Window window, ListBox list, int rowIndex)
        {
            _ = window;
            var item = list.ContainerFromIndex(rowIndex) as ListBoxItem;
            Assert.NotNull(item);

            var labelText = item.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
            var target = (Visual?)labelText ?? item;
            var point =
                target.TranslatePoint(
                    new Point(Math.Max(2, target.Bounds.Width / 2), Math.Max(2, target.Bounds.Height / 2)),
                    item
                ) ?? new Point(8, 4);

            var pointer = new Pointer(1, PointerType.Mouse, true);
            var pressedProps = new PointerPointProperties(
                RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed
            );
            item.RaiseEvent(
                new PointerPressedEventArgs(
                    item,
                    pointer,
                    list,
                    point,
                    0,
                    pressedProps,
                    KeyModifiers.None,
                    clickCount: 1
                )
                {
                    RoutedEvent = InputElement.PointerPressedEvent,
                }
            );

            var releasedProps = new PointerPointProperties(
                RawInputModifiers.None,
                PointerUpdateKind.LeftButtonReleased
            );
            item.RaiseEvent(
                new PointerReleasedEventArgs(
                    item,
                    pointer,
                    list,
                    point,
                    0,
                    releasedProps,
                    KeyModifiers.None,
                    MouseButton.Left
                )
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                }
            );
            Dispatcher.UIThread.RunJobs();
        }
    }
}

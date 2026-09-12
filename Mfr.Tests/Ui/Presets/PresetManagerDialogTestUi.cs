using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Shared headless host helpers for Preset Manager dialog tests.
    /// </summary>
    internal static class PresetManagerDialogTestUi
    {
        /// <summary>
        /// Shows a Preset Manager dialog seeded with named empty presets in order.
        /// </summary>
        /// <param name="names">Preset names to upsert in list order.</param>
        /// <returns>Dialog, view model, and presets list.</returns>
        public static (
            PresetManagerDialog Dialog,
            PresetManagerDialogViewModel ViewModel,
            ListBox List
        ) ShowWithPresets(params string[] names)
        {
            var manager = PresetManager.CreateEmpty();
            foreach (var name in names)
            {
                manager.Upsert(
                    new FilterPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = $"desc-{name}",
                        Chain = new FilterChain { Steps = [] },
                    }
                );
            }

            var appliedFilters = new AppliedFiltersViewModel(presetManager: manager);
            var viewModel = new PresetManagerDialogViewModel(appliedFilters);
            var dialog = new PresetManagerDialog(viewModel, appliedFilters, tryLoadAsync: _ => Task.FromResult(false));
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("PresetsList");
            Assert.NotNull(list);
            return (dialog, viewModel, list);
        }

        /// <summary>
        /// Clicks a presets list row with optional modifiers (Ctrl multi-select).
        /// </summary>
        /// <param name="dialog">Host dialog for pointer routing.</param>
        /// <param name="list">Presets list.</param>
        /// <param name="index">Zero-based row index.</param>
        /// <param name="modifiers">Pointer modifiers.</param>
        public static void ClickListIndex(
            PresetManagerDialog dialog,
            ListBox list,
            int index,
            RawInputModifiers modifiers
        )
        {
            var windowPoint = ListIndexClickPoint(dialog, list, index);
            dialog.MouseMove(windowPoint, modifiers);
            dialog.MouseDown(windowPoint, MouseButton.Left, modifiers);
            dialog.MouseUp(windowPoint, MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Raises a left-button press on a list row (keeps multi-selection before drag threshold).
        /// </summary>
        /// <param name="list">Presets list.</param>
        /// <param name="index">Zero-based row index.</param>
        public static void PressListIndex(ListBox list, int index)
        {
            var item = list.ContainerFromIndex(index) as ListBoxItem;
            Assert.NotNull(item);

            var point = new Point(8, 4);
            var props = new PointerPointProperties(
                RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed
            );
            var pointer = new Pointer(1, PointerType.Mouse, true);
            list.RaiseEvent(
                new PointerPressedEventArgs(item, pointer, list, point, 0, props, KeyModifiers.None, clickCount: 1)
                {
                    RoutedEvent = InputElement.PointerPressedEvent,
                }
            );
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Resolves a click point on a presets list row in dialog coordinates.
        /// </summary>
        /// <param name="dialog">Host dialog.</param>
        /// <param name="list">Presets list.</param>
        /// <param name="index">Zero-based row index.</param>
        /// <returns>Point in dialog space.</returns>
        public static Point ListIndexClickPoint(PresetManagerDialog dialog, ListBox list, int index)
        {
            list.ScrollIntoView(index);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = list.ContainerFromIndex(index);
            Assert.NotNull(container);

            var labelText = container
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
            var target = (Visual?)labelText ?? container;
            var local = new Point(Math.Max(8, target.Bounds.Width / 2), Math.Max(1, target.Bounds.Height / 2));
            var windowPoint = target.TranslatePoint(local, dialog);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }
    }
}

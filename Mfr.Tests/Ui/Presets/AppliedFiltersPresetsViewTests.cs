using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.Resources;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.Filters.Case;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Headless tests for Applied Filters preset toolbar (▾ quick-pick + tips).
    /// </summary>
    public sealed class AppliedFiltersPresetsViewTests
    {
        /// <summary>
        /// Verifies Presets / ▾ / Save toolbar tips bind to <see cref="AppTips"/>.
        /// </summary>
        [AvaloniaFact]
        public void Preset_Toolbar_Tips_Bind()
        {
            var (window, _, view) = _ShowWithPresets();

            Assert.Equal(AppTips.Presets, ToolTip.GetTip(view.FindControl<Button>("PresetsButton")!));
            Assert.Equal(AppTips.PresetsQuickPick, ToolTip.GetTip(view.FindControl<Button>("PresetsQuickPickButton")!));
            Assert.Equal(AppTips.SavePreset, ToolTip.GetTip(view.FindControl<Button>("SavePresetButton")!));
            Assert.Null(view.FindControl<Button>("SavePresetAsButton"));

            window.Close();
        }

        /// <summary>
        /// Verifies ▾ shows a disabled empty placeholder when no presets exist.
        /// </summary>
        [AvaloniaFact]
        public void QuickPick_Empty_Shows_Disabled_No_Presets()
        {
            var viewModel = new AppliedFiltersViewModel();
            var (window, view) = _Show(viewModel);
            var button = view.FindControl<Button>("PresetsQuickPickButton");
            Assert.NotNull(button);
            var flyout = view.PresetsQuickPickFlyout;

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(flyout.IsOpen);
            var item = Assert.Single(flyout.Items.OfType<MenuItem>());
            Assert.Equal("No presets", item.Header);
            Assert.False(item.IsEnabled);

            flyout.Hide();
            window.Close();
        }

        /// <summary>
        /// Verifies clicking ▾ opens the flyout with sorted preset names.
        /// </summary>
        [AvaloniaFact]
        public void QuickPick_Click_Opens_Sorted_Items()
        {
            var manager = PresetManager.CreateEmpty();
            foreach (var name in new[] { "beta", "Alpha", "alpha2" })
            {
                manager.Upsert(
                    new FilterPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Chain = new FilterChain { Steps = [] },
                    }
                );
            }

            var (window, view) = _Show(new AppliedFiltersViewModel(presetManager: manager));
            var button = view.FindControl<Button>("PresetsQuickPickButton");
            Assert.NotNull(button);
            var flyout = view.PresetsQuickPickFlyout;

            var local = new Point(Math.Max(2, button.Bounds.Width / 2), Math.Max(2, button.Bounds.Height / 2));
            var windowPoint = button.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);
            window.MouseMove(windowPoint.Value);
            window.MouseDown(windowPoint.Value, MouseButton.Left);
            window.MouseUp(windowPoint.Value, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.True(flyout.IsOpen);
            Assert.Equal(
                ["Alpha", "alpha2", "beta"],
                flyout.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList()
            );

            flyout.Hide();
            window.Close();
        }

        /// <summary>
        /// Verifies choosing a ▾ menu item loads the preset and sets last-loaded.
        /// </summary>
        [AvaloniaFact]
        public void QuickPick_Load_Sets_LastLoaded()
        {
            var (window, viewModel, view) = _ShowWithPresets();
            var button = view.FindControl<Button>("PresetsQuickPickButton");
            Assert.NotNull(button);
            var flyout = view.PresetsQuickPickFlyout;

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(flyout.IsOpen);
            var item = Assert.Single(flyout.Items.OfType<MenuItem>());
            Assert.Equal("Demo", item.Header);
            Assert.True(item.IsEnabled);

            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Demo", viewModel.LastLoaded?.Name);
            Assert.Equal("Letters Case", viewModel.Steps[0].DisplayName);

            window.Close();
        }

        private static (Window Window, AppliedFiltersViewModel ViewModel, AppliedFiltersView View) _ShowWithPresets()
        {
            var manager = PresetManager.CreateEmpty();
            var letters = new LettersCaseFilter();
            manager.Upsert(
                new FilterPreset
                {
                    Id = Guid.NewGuid(),
                    Name = "Demo",
                    Description = "demo",
                    Chain = new FilterChain { Steps = [new FilterChainStep(Enabled: true, Filter: letters)] },
                }
            );
            var viewModel = new AppliedFiltersViewModel(presetManager: manager);
            var (window, view) = _Show(viewModel);
            return (window, viewModel, view);
        }

        private static (Window Window, AppliedFiltersView View) _Show(AppliedFiltersViewModel viewModel)
        {
            var view = new AppliedFiltersView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 240,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return (window, view);
        }
    }
}

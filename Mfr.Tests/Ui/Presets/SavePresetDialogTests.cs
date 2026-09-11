using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Headless smoke tests for <see cref="SavePresetDialog"/>.
    /// </summary>
    public sealed class SavePresetDialogTests
    {
        /// <summary>
        /// Verifies the dialog loads with Name suggestions, Save enabled for a non-blank name, and footer wiring.
        /// </summary>
        [AvaloniaFact]
        public void Constructs_With_Name_Suggestions_And_Save_Enabled()
        {
            var existing = new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = "Demo",
                Description = "notes",
                Chain = new FilterChain { Steps = [] },
            };
            var viewModel = new SavePresetDialogViewModel(existing, existingPresets: [existing]);
            var dialog = new SavePresetDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(dialog.IsVisible);
            Assert.Equal("Save Preset", dialog.Title);

            var nameBox = dialog.FindControl<ComboBox>("NameBox");
            Assert.NotNull(nameBox);
            Assert.True(nameBox.IsEditable);
            Assert.Equal(["Demo"], Assert.IsAssignableFrom<IEnumerable<string>>(nameBox.ItemsSource).ToList());

            var save = dialog.FindControl<Button>("SaveButton");
            Assert.NotNull(save);
            Assert.True(save.IsEnabled);
            Assert.True(save.IsDefault);

            var footer = dialog.FindControl<StackPanel>("Footer");
            Assert.NotNull(footer);
            Assert.Equal(HorizontalAlignment.Center, footer.HorizontalAlignment);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Save stays disabled when the name is blank.
        /// </summary>
        [AvaloniaFact]
        public void Save_Disabled_When_Name_Blank()
        {
            var dialog = new SavePresetDialog(new SavePresetDialogViewModel());
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var save = dialog.FindControl<Button>("SaveButton");
            Assert.NotNull(save);
            Assert.False(save.IsEnabled);

            dialog.Close();
        }
    }
}

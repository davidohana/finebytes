using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.Views.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Delete-preset confirmation gates for Preset Manager.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class PresetDeleteConfirmTests
    {
        public PresetDeleteConfirmTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies delete confirms by default and removes when accepted.
        /// </summary>
        [AvaloniaFact]
        public void Delete_confirm_accept_removes_preset()
        {
            var (dialog, viewModel, _) = PresetManagerDialogTestUi.ShowWithPresets("A", "B");
            var asked = 0;
            dialog.ConfirmDeleteAsync = (title, message) =>
            {
                asked++;
                Assert.Equal("Delete Preset", title);
                Assert.Contains("'A'", message, StringComparison.Ordinal);
                return Task.FromResult(true);
            };

            _ClickDelete(dialog);

            Assert.Equal(1, asked);
            Assert.Equal(["B"], viewModel.Presets.Select(preset => preset.Name));
            dialog.Close();
        }

        /// <summary>
        /// Verifies decline leaves presets unchanged.
        /// </summary>
        [AvaloniaFact]
        public void Delete_confirm_decline_keeps_preset()
        {
            var (dialog, viewModel, _) = PresetManagerDialogTestUi.ShowWithPresets("A", "B");
            dialog.ConfirmDeleteAsync = (_, _) => Task.FromResult(false);

            _ClickDelete(dialog);

            Assert.Equal(["A", "B"], viewModel.Presets.Select(preset => preset.Name));
            dialog.Close();
        }

        /// <summary>
        /// Verifies a suppressed DeletePreset skips the confirm hook and deletes.
        /// </summary>
        [AvaloniaFact]
        public void Delete_suppressed_skips_confirm()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.DeletePreset);
            var (dialog, viewModel, _) = PresetManagerDialogTestUi.ShowWithPresets("A", "B");
            var asked = 0;
            dialog.ConfirmDeleteAsync = (_, _) =>
            {
                asked++;
                return Task.FromResult(false);
            };

            _ClickDelete(dialog);

            Assert.Equal(0, asked);
            Assert.Equal(["B"], viewModel.Presets.Select(preset => preset.Name));
            dialog.Close();
        }

        private static void _ClickDelete(PresetManagerDialog dialog)
        {
            var delete = dialog.FindControl<Button>("DeleteButton");
            Assert.NotNull(delete);
            delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }
    }
}

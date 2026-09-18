using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.Views.Crash;
using Mfr.App.Ui.Views.FileList;
using Mfr.App.Ui.Views.FilterChainPane;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.App.Ui.Views.LogDialog;
using Mfr.App.Ui.Views.Presets;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.Services.Session
{
    /// <summary>
    /// Headless tests for resizable-dialog geometry restore and capture.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class DialogSessionTests
    {
        public DialogSessionTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies Attach restores size and position when remember is on.
        /// </summary>
        [AvaloniaFact]
        public void Attach_RememberOn_RestoresSizeAndPosition()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.FieldShuttle] = new WindowGeometryPrefs
                {
                    X = 40,
                    Y = 60,
                    Width = 820,
                    Height = 560,
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.FieldShuttle);

            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
            Assert.Equal(820, window.Width);
            Assert.Equal(560, window.Height);
            Assert.Equal(new PixelPoint(40, 60), window.Position);
            Assert.Equal(WindowState.Normal, window.WindowState);
        }

        /// <summary>
        /// Verifies Attach restores restore-bounds then maximizes when maximized is set.
        /// </summary>
        [AvaloniaFact]
        public void Attach_Maximized_AppliesRestoreBoundsThenMaximizes()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.RenameLog] = new WindowGeometryPrefs
                {
                    X = 40,
                    Y = 60,
                    Width = 820,
                    Height = 560,
                    Maximized = true,
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.RenameLog);

            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
            Assert.Equal(820, window.Width);
            Assert.Equal(560, window.Height);
            Assert.Equal(new PixelPoint(40, 60), window.Position);
            Assert.Equal(WindowState.Maximized, window.WindowState);
        }

        /// <summary>
        /// Verifies maximize-only prefs (no restore size) leave the dialog at XAML defaults.
        /// </summary>
        [AvaloniaFact]
        public void Attach_MaximizedWithoutRestoreSize_LeavesDefaults()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.RenameLog] = new WindowGeometryPrefs { Maximized = true },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.RenameLog);

            Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
            Assert.Equal(500, window.Width);
            Assert.Equal(400, window.Height);
            Assert.Equal(WindowState.Normal, window.WindowState);
        }

        /// <summary>
        /// Verifies maximized-frame leftovers (negative top) without maximized flag are not restored.
        /// </summary>
        [AvaloniaFact]
        public void Attach_MaximizedFrameCoords_DoesNotRestore()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.RenameLog] = new WindowGeometryPrefs
                {
                    X = -8,
                    Y = -8,
                    Width = 2560,
                    Height = 1369,
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.RenameLog);

            Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
            Assert.Equal(500, window.Width);
            Assert.Equal(400, window.Height);
            Assert.Equal(WindowState.Normal, window.WindowState);
        }

        /// <summary>
        /// Verifies Closing while maximized keeps prior restore bounds and sets maximized.
        /// </summary>
        [AvaloniaFact]
        public void Closing_Maximized_KeepsRestoreBoundsAndSetsFlag()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.RenameLog] = new WindowGeometryPrefs
                {
                    X = 40,
                    Y = 60,
                    Width = 820,
                    Height = 560,
                },
            };

            var window = _CreateDialog(width: 700, height: 450);
            DialogSession.Attach(window, DialogIds.RenameLog);
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(-8, -8);
            window.Width = 2560;
            window.Height = 1369;
            window.WindowState = WindowState.Maximized;
            window.Close();

            var saved = Assert.Contains(DialogIds.RenameLog, ConfigStore.Dialogs);
            Assert.Equal(40, saved.X);
            Assert.Equal(60, saved.Y);
            Assert.Equal(820, saved.Width);
            Assert.Equal(560, saved.Height);
            Assert.True(saved.Maximized);
        }

        /// <summary>
        /// Verifies Closing while maximized without prior restore bounds does not persist maximized alone.
        /// </summary>
        [AvaloniaFact]
        public void Closing_MaximizedWithoutPriorBounds_DoesNotPersistMaximizedOnly()
        {
            ConfigStore.Options.RememberWindowState = true;

            var window = _CreateDialog(width: 700, height: 450);
            DialogSession.Attach(window, DialogIds.SavePreset);
            window.Show();
            window.UpdateLayout();
            window.WindowState = WindowState.Maximized;
            window.Close();

            Assert.False(ConfigStore.Dialogs!.ContainsKey(DialogIds.SavePreset));
        }

        /// <summary>
        /// Verifies width-only mode restores width and position but leaves height alone.
        /// </summary>
        [AvaloniaFact]
        public void Attach_WidthAndPosition_DoesNotRestoreHeight()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.FilterOptions] = new WindowGeometryPrefs
                {
                    X = 10,
                    Y = 20,
                    Width = 640,
                    Height = 999,
                },
            };

            var window = _CreateDialog(width: 500, height: 300);
            DialogSession.Attach(window, DialogIds.FilterOptions, DialogGeometryMode.WidthAndPosition);

            Assert.Equal(640, window.Width);
            Assert.Equal(300, window.Height);
            Assert.Equal(new PixelPoint(10, 20), window.Position);
        }

        /// <summary>
        /// Verifies remember-off skips restore and leaves CenterOwner defaults.
        /// </summary>
        [AvaloniaFact]
        public void Attach_RememberOff_DoesNotRestore()
        {
            ConfigStore.Options.RememberWindowState = false;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.RenameLog] = new WindowGeometryPrefs
                {
                    X = 40,
                    Y = 60,
                    Width = 820,
                    Height = 560,
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.RenameLog);

            Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
            Assert.Equal(500, window.Width);
            Assert.Equal(400, window.Height);
        }

        /// <summary>
        /// Verifies invalid saved size leaves the dialog unchanged.
        /// </summary>
        [AvaloniaFact]
        public void Attach_InvalidSavedSize_DoesNotRestore()
        {
            ConfigStore.Options.RememberWindowState = true;
            ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
            {
                [DialogIds.Crash] = new WindowGeometryPrefs
                {
                    X = 40,
                    Y = 60,
                    Width = 0,
                    Height = 560,
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, DialogIds.Crash);

            Assert.Equal(WindowStartupLocation.CenterOwner, window.WindowStartupLocation);
            Assert.Equal(500, window.Width);
            Assert.Equal(400, window.Height);
        }

        /// <summary>
        /// Verifies Closing captures geometry into ConfigStore when remember is on.
        /// </summary>
        [AvaloniaFact]
        public void Closing_RememberOn_CapturesIntoConfigStore()
        {
            ConfigStore.Options.RememberWindowState = true;

            var window = _CreateDialog(width: 700, height: 450);
            DialogSession.Attach(window, DialogIds.PresetManager);
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(33, 44);
            window.Width = 700;
            window.Height = 450;
            window.Close();

            var saved = Assert.Contains(DialogIds.PresetManager, ConfigStore.Dialogs!);
            Assert.Equal(33, saved.X);
            Assert.Equal(44, saved.Y);
            Assert.Equal(700, saved.Width);
            Assert.Equal(450, saved.Height);
            Assert.False(saved.Maximized);
        }

        /// <summary>
        /// Verifies width-only mode still captures full geometry (restore ignores height).
        /// </summary>
        [AvaloniaFact]
        public void Closing_WidthAndPosition_CapturesSizeAndPosition()
        {
            ConfigStore.Options.RememberWindowState = true;

            var window = _CreateDialog(width: 640, height: 300);
            DialogSession.Attach(window, DialogIds.FilterOptions, DialogGeometryMode.WidthAndPosition);
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(15, 25);
            window.Width = 680;
            window.Height = 310;
            window.Close();

            var saved = Assert.Contains(DialogIds.FilterOptions, ConfigStore.Dialogs!);
            Assert.Equal(15, saved.X);
            Assert.Equal(25, saved.Y);
            Assert.Equal(680, saved.Width);
            Assert.Equal(310, saved.Height);
        }

        /// <summary>
        /// Verifies Closing does not capture when remember is off.
        /// </summary>
        [AvaloniaFact]
        public void Closing_RememberOff_DoesNotCapture()
        {
            ConfigStore.Options.RememberWindowState = false;

            var window = _CreateDialog(width: 700, height: 450);
            window.Position = new PixelPoint(33, 44);
            DialogSession.Attach(window, DialogIds.SavePreset);
            window.Show();
            window.Close();

            Assert.Null(ConfigStore.Dialogs);
        }

        /// <summary>
        /// Verifies each live resizable dialog constructs and captures geometry on close.
        /// </summary>
        [AvaloniaFact]
        public void LiveDialogs_Construct_CapturesOnClose()
        {
            ConfigStore.Options.RememberWindowState = true;

            _SmokeCapture(new RenameListFieldShuttleDialog(), DialogIds.FieldShuttle);
            _SmokeCapture(new RenameLogDialog(), DialogIds.RenameLog);
            _SmokeCapture(new PresetManagerDialog(), DialogIds.PresetManager);
            _SmokeCapture(new ImportSamplePresetsDialog(), DialogIds.ImportSamplePresets);
            _SmokeCapture(new SavePresetDialog(), DialogIds.SavePreset);
            _SmokeCapture(new FilterOptionsDialog(), DialogIds.FilterOptions);
            _SmokeCapture(new FormatTokenEditorDialog(), DialogIds.FormatTokenEditor);
            _SmokeCapture(new ExcludeMasksDialog(), DialogIds.ExcludeMasks);
            _SmokeCapture(new RenameListRowErrorDialog(), DialogIds.RenameListRowError);
            _SmokeCapture(new CrashDialog(), DialogIds.Crash);
        }

        private static void _SmokeCapture(Window window, string id)
        {
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(12, 34);
            // SizeToContent=Height dialogs may still report NaN height before content lock.
            if (double.IsNaN(window.Height) || window.Height <= 0)
            {
                window.Height = 300;
            }

            window.Close();

            Assert.Contains(id, ConfigStore.Dialogs!);
        }

        private static Window _CreateDialog(double width, double height)
        {
            return new Window
            {
                Width = width,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
        }
    }
}

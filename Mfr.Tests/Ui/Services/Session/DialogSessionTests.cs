using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Session;

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
            ConfigStore.MainWindow = new MainWindowPrefs
            {
                RememberWindowState = true,
                Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                {
                    ["fieldShuttle"] = new WindowGeometryPrefs
                    {
                        X = 40,
                        Y = 60,
                        Width = 820,
                        Height = 560,
                    },
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, "fieldShuttle");

            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
            Assert.Equal(820, window.Width);
            Assert.Equal(560, window.Height);
            Assert.Equal(new PixelPoint(40, 60), window.Position);
        }

        /// <summary>
        /// Verifies width-only mode restores width and position but leaves height alone.
        /// </summary>
        [AvaloniaFact]
        public void Attach_WidthAndPosition_DoesNotRestoreHeight()
        {
            ConfigStore.MainWindow = new MainWindowPrefs
            {
                RememberWindowState = true,
                Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                {
                    ["filterOptions"] = new WindowGeometryPrefs
                    {
                        X = 10,
                        Y = 20,
                        Width = 640,
                        Height = 999,
                    },
                },
            };

            var window = _CreateDialog(width: 500, height: 300);
            DialogSession.Attach(window, "filterOptions", DialogGeometryMode.WidthAndPosition);

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
            ConfigStore.MainWindow = new MainWindowPrefs
            {
                RememberWindowState = false,
                Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                {
                    ["renameLog"] = new WindowGeometryPrefs
                    {
                        X = 40,
                        Y = 60,
                        Width = 820,
                        Height = 560,
                    },
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, "renameLog");

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
            ConfigStore.MainWindow = new MainWindowPrefs
            {
                RememberWindowState = true,
                Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                {
                    ["crash"] = new WindowGeometryPrefs
                    {
                        X = 40,
                        Y = 60,
                        Width = 0,
                        Height = 560,
                    },
                },
            };

            var window = _CreateDialog(width: 500, height: 400);
            DialogSession.Attach(window, "crash");

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
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = true };

            var window = _CreateDialog(width: 700, height: 450);
            DialogSession.Attach(window, "presetManager");
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(33, 44);
            window.Width = 700;
            window.Height = 450;
            window.Close();

            var saved = Assert.Contains("presetManager", ConfigStore.MainWindow!.Dialogs!);
            Assert.Equal(33, saved.X);
            Assert.Equal(44, saved.Y);
            Assert.Equal(700, saved.Width);
            Assert.Equal(450, saved.Height);
        }

        /// <summary>
        /// Verifies width-only mode still captures full geometry (restore ignores height).
        /// </summary>
        [AvaloniaFact]
        public void Closing_WidthAndPosition_CapturesSizeAndPosition()
        {
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = true };

            var window = _CreateDialog(width: 640, height: 300);
            DialogSession.Attach(window, "filterOptions", DialogGeometryMode.WidthAndPosition);
            window.Show();
            window.UpdateLayout();
            window.Position = new PixelPoint(15, 25);
            window.Width = 680;
            window.Height = 310;
            window.Close();

            var saved = Assert.Contains("filterOptions", ConfigStore.MainWindow!.Dialogs!);
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
            ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = false };

            var window = _CreateDialog(width: 700, height: 450);
            window.Position = new PixelPoint(33, 44);
            DialogSession.Attach(window, "savePreset");
            window.Show();
            window.Close();

            Assert.Null(ConfigStore.MainWindow?.Dialogs);
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

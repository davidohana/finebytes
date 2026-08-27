using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for main-window geometry restore and first-run defaults.
    /// </summary>
    public sealed class WindowSessionTests
    {
        /// <summary>
        /// Verifies a null saved state does not change the window and reports no restore.
        /// </summary>
        [AvaloniaFact]
        public void TryRestore_Null_ReturnsFalse()
        {
            var window = _CreateWindow();
            var beforeWidth = window.Width;
            var beforeHeight = window.Height;

            var restored = WindowSession.TryRestore(window, saved: null);

            Assert.False(restored);
            Assert.Equal(beforeWidth, window.Width);
            Assert.Equal(beforeHeight, window.Height);
        }

        /// <summary>
        /// Verifies invalid sizes are rejected.
        /// </summary>
        [AvaloniaFact]
        public void TryRestore_InvalidSize_ReturnsFalse()
        {
            var window = _CreateWindow();

            Assert.False(
                WindowSession.TryRestore(
                    window,
                    new SessionStateMainWindow
                    {
                        X = 10,
                        Y = 20,
                        Width = 0,
                        Height = 720,
                        State = "Normal",
                    }
                )
            );
            Assert.False(
                WindowSession.TryRestore(
                    window,
                    new SessionStateMainWindow
                    {
                        X = 10,
                        Y = 20,
                        Width = double.NaN,
                        Height = 720,
                        State = "Normal",
                    }
                )
            );
        }

        /// <summary>
        /// Verifies a normal saved layout is applied.
        /// </summary>
        [AvaloniaFact]
        public void TryRestore_Normal_AppliesSizeAndPosition()
        {
            var window = _CreateWindow();

            var restored = WindowSession.TryRestore(
                window,
                new SessionStateMainWindow
                {
                    X = 40,
                    Y = 60,
                    Width = 960,
                    Height = 640,
                    State = "Normal",
                }
            );

            Assert.True(restored);
            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.Equal(960, window.Width);
            Assert.Equal(640, window.Height);
            Assert.Equal(new PixelPoint(40, 60), window.Position);
        }

        /// <summary>
        /// Verifies maximized restore only sets maximized state.
        /// </summary>
        [AvaloniaFact]
        public void TryRestore_Maximized_SetsWindowStateOnly()
        {
            var window = _CreateWindow();
            var beforeWidth = window.Width;
            var beforeHeight = window.Height;

            var restored = WindowSession.TryRestore(
                window,
                new SessionStateMainWindow
                {
                    X = 0,
                    Y = 0,
                    Width = 960,
                    Height = 640,
                    State = "Maximized",
                }
            );

            Assert.True(restored);
            Assert.Equal(WindowState.Maximized, window.WindowState);
            Assert.Equal(beforeWidth, window.Width);
            Assert.Equal(beforeHeight, window.Height);
        }

        /// <summary>
        /// Verifies Capture reflects current geometry and state.
        /// </summary>
        [AvaloniaFact]
        public void Capture_RoundTrips_NormalGeometry()
        {
            var window = _CreateWindow();
            window.Width = 900;
            window.Height = 600;
            window.Position = new PixelPoint(12, 34);
            window.WindowState = WindowState.Normal;

            var captured = WindowSession.Capture(window);

            Assert.Equal(12, captured.X);
            Assert.Equal(34, captured.Y);
            Assert.Equal(900, captured.Width);
            Assert.Equal(600, captured.Height);
            Assert.Equal("Normal", captured.State);
        }

        /// <summary>
        /// Verifies ApplyDefault sizes to about two-thirds of the primary working area and centers.
        /// </summary>
        [AvaloniaFact]
        public void ApplyDefault_AfterShow_SizesToTwoThirdsAndCenters()
        {
            var window = _CreateWindow();
            window.Show();
            window.UpdateLayout();

            var screen = window.Screens?.Primary ?? window.Screens?.All[0];
            Assert.NotNull(screen);

            WindowSession.ApplyDefault(window);

            var area = screen.WorkingArea;
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1.0;
            var expectedWidth = Math.Max(800, area.Width / scaling * (2.0 / 3.0));
            var expectedHeight = Math.Max(500, area.Height / scaling * (2.0 / 3.0));
            var pixelWidth = (int)Math.Round(window.Width * scaling);
            var pixelHeight = (int)Math.Round(window.Height * scaling);
            var expectedX = area.X + ((area.Width - pixelWidth) / 2);
            var expectedY = area.Y + ((area.Height - pixelHeight) / 2);

            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
            Assert.InRange(window.Width, expectedWidth - 1, expectedWidth + 1);
            Assert.InRange(window.Height, expectedHeight - 1, expectedHeight + 1);
            Assert.Equal(expectedX, window.Position.X);
            Assert.Equal(expectedY, window.Position.Y);
        }

        /// <summary>
        /// Verifies ApplyDefault before Show either applies geometry immediately or arms the CenterScreen fallback.
        /// </summary>
        [AvaloniaFact]
        public void ApplyDefault_BeforeShow_DoesNotThrow()
        {
            var window = _CreateWindow();

            WindowSession.ApplyDefault(window);

            Assert.Equal(WindowState.Normal, window.WindowState);
            Assert.True(
                window.WindowStartupLocation is WindowStartupLocation.CenterScreen or WindowStartupLocation.Manual
            );
        }

        private static MainWindow _CreateWindow()
        {
            return new MainWindow
            {
                DataContext = new MainWindowViewModel(),
                Width = 1100,
                Height = 720,
            };
        }
    }
}

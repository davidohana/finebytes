using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Shell;

namespace Mfr.Tests.Ui.Services.Shell
{
    /// <summary>
    /// Tests <see cref="ShellOwnerHwnd"/> resolution when no main window is set.
    /// </summary>
    public sealed class ShellOwnerHwndTests
    {
        /// <summary>
        /// Verifies the helper returns zero when the desktop main window is missing.
        /// </summary>
        [AvaloniaFact]
        public void TryGetMainWindowHandle_Returns_Zero_Without_MainWindow()
        {
            Assert.Equal(IntPtr.Zero, ShellOwnerHwnd.TryGetMainWindowHandle());
        }
    }
}

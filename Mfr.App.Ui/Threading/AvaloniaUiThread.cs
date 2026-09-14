using Avalonia;
using Avalonia.Threading;

namespace Mfr.App.Ui.Threading
{
    /// <summary>
    /// Marshals work onto the Avalonia UI thread for view-model background completions.
    /// </summary>
    internal static class AvaloniaUiThread
    {
        /// <summary>
        /// Runs <paramref name="action"/> on the UI thread, or inline when already there / no app.
        /// </summary>
        /// <param name="action">Work that touches UI-bound state.</param>
        public static void Post(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (Application.Current is null)
            {
                action();
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
                return;
            }

            Dispatcher.UIThread.Post(action);
        }
    }
}

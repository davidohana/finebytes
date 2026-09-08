using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Ensures modal dialog windows can receive Escape / Enter for <see cref="Button.IsCancel"/> and
    /// <see cref="Button.IsDefault"/>.
    /// </summary>
    /// <remarks>
    /// Avalonia often leaves keyboard focus on the owner after <see cref="Window.ShowDialog(Window)"/>,
    /// so Cancel buttons with <c>IsCancel="True"</c> never see Escape until the dialog is clicked.
    /// </remarks>
    internal static class ModalDialogKeyboard
    {
        private static readonly ConditionalWeakTable<Window, object> s_attached = [];

        /// <summary>
        /// Makes <paramref name="window"/> focusable and focuses it when opened.
        /// </summary>
        /// <param name="window">Modal dialog window.</param>
        public static void Attach(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);
            if (s_attached.TryGetValue(window, out _))
            {
                return;
            }

            s_attached.Add(window, string.Empty);
            window.Focusable = true;
            window.Opened += (_, _) =>
            {
                window.Activate();
                window.Focus();
            };
        }
    }
}

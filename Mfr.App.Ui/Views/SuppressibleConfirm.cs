using Avalonia.Controls;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared gate + dialog for suppressible confirmation kinds.
    /// </summary>
    internal static class SuppressibleConfirm
    {
        /// <summary>
        /// When <paramref name="kind"/> is suppressed, returns <see langword="true"/> (proceed).
        /// Otherwise shows <see cref="ConfirmMessageDialog"/> (or <paramref name="confirmHook"/> when set).
        /// </summary>
        /// <param name="owner">Owner window for the dialog.</param>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Dialog body.</param>
        /// <param name="kind">Suppressible confirmation kind (keep-showing checkbox).</param>
        /// <param name="confirmHook">When set, replaces the dialog (tests).</param>
        /// <returns>
        /// <see langword="true"/> when the action may proceed; <see langword="false"/> when cancelled.
        /// </returns>
        public static async Task<bool> ConfirmAsync(
            Window owner,
            string title,
            string message,
            ConfirmationKind kind,
            Func<Task<bool>>? confirmHook = null
        )
        {
            if (!ConfirmationPolicy.ShouldConfirm(kind))
            {
                return true;
            }

            if (confirmHook is { } hook)
            {
                return await hook().ConfigureAwait(true);
            }

            var dialog = new ConfirmMessageDialog(title: title, message: message, kind: kind);
            return await dialog.ShowDialog<bool?>(owner).ConfigureAwait(true) == true;
        }
    }
}

using System.Windows.Input;
using Avalonia.Input;

namespace Mfr.App.Ui.Input
{
    /// <summary>
    /// Compares <see cref="KeyEventArgs"/> to a <see cref="KeyGesture"/>.
    /// </summary>
    internal static class KeyGestureMatch
    {
        /// <summary>
        /// Gets whether <paramref name="e"/> matches <paramref name="gesture"/> (key and modifiers).
        /// </summary>
        /// <param name="e">Key event.</param>
        /// <param name="gesture">Expected gesture.</param>
        /// <returns><see langword="true"/> when key and modifiers are equal.</returns>
        public static bool Matches(KeyEventArgs e, KeyGesture gesture)
        {
            return e.Key == gesture.Key && e.KeyModifiers == gesture.KeyModifiers;
        }

        /// <summary>
        /// Executes <paramref name="command"/> when <paramref name="e"/> matches <paramref name="gesture"/> and CanExecute.
        /// </summary>
        /// <param name="e">Key event; marked handled on success.</param>
        /// <param name="gesture">Expected gesture.</param>
        /// <param name="command">Command to run, or <see langword="null"/>.</param>
        /// <returns>
        /// <see langword="true"/> when the command ran; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryExecute(KeyEventArgs e, KeyGesture gesture, ICommand? command)
        {
            if (e.Handled || command is null || !Matches(e, gesture))
            {
                return false;
            }

            if (!command.CanExecute(null))
            {
                return false;
            }

            command.Execute(null);
            e.Handled = true;
            return true;
        }
    }
}

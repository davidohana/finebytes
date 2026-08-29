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
    }
}

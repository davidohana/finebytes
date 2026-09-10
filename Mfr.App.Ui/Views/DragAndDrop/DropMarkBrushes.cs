using Avalonia;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Shared salmon brush for DnD insert markers across ListBox and Rename List surfaces.
    /// </summary>
    /// <remarks>
    /// Theme resource key is <see cref="ResourceKey"/> (<c>DropMarkBrush</c> in AppChrome).
    /// Code that cannot resolve the theme uses <see cref="Fallback"/>.
    /// </remarks>
    internal static class DropMarkBrushes
    {
        /// <summary>
        /// Theme resource key for the drop-mark brush.
        /// </summary>
        public const string ResourceKey = "DropMarkBrush";

        /// <summary>
        /// Salmon insert-marker color (MFR7 MarkedRow / <c>Color.Salmon</c>).
        /// </summary>
        public static readonly Color Color = Color.FromRgb(0xFA, 0x80, 0x72);

        /// <summary>
        /// Fallback brush when the theme resource is unavailable.
        /// </summary>
        public static readonly IBrush Fallback = new SolidColorBrush(Color);

        /// <summary>
        /// Resolves the drop-mark brush from <paramref name="owner"/>'s theme, with salmon fallback.
        /// </summary>
        /// <param name="owner">Element whose theme resources are searched.</param>
        /// <returns>Theme brush when present; otherwise <see cref="Fallback"/>.</returns>
        public static IBrush Resolve(StyledElement owner)
        {
            if (
                owner.TryGetResource(ResourceKey, owner.ActualThemeVariant, out var resource)
                && resource is IBrush brush
            )
            {
                return brush;
            }

            return Fallback;
        }
    }
}

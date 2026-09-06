using Avalonia.Controls;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.GridColumnSizing
{
    /// <summary>
    /// Shared app chrome font names and sizes used by theme resources and grid column measurement.
    /// </summary>
    internal static class GridFonts
    {
        /// <summary>
        /// Proportional UI sans family list.
        /// </summary>
        public const string AppChromeFamilyName = "Segoe UI, SegoeUI";

        /// <summary>
        /// Fixed-width UI family list (Rename List, format-string fields).
        /// </summary>
        public const string AppChromeFixedWidthFamilyName = "Cascadia Mono, Consolas, monospace";

        /// <summary>
        /// Grid body and header size in device-independent pixels.
        /// </summary>
        public const double FontSize = 12;

        /// <summary>
        /// Sort and preview glyph size in device-independent pixels.
        /// </summary>
        public const double SortGlyphFontSize = 11;

        /// <summary>
        /// Proportional UI sans font family.
        /// </summary>
        public static FontFamily AppChromeFamily { get; } = new(AppChromeFamilyName);

        /// <summary>
        /// Fixed-width UI font family.
        /// </summary>
        public static FontFamily AppChromeFixedWidthFamily { get; } = new(AppChromeFixedWidthFamilyName);

        /// <summary>
        /// Registers theme keys used by app chrome and File List / Rename List styles.
        /// </summary>
        /// <param name="resources">Application resource dictionary.</param>
        public static void AddResources(IResourceDictionary resources)
        {
            ArgumentNullException.ThrowIfNull(resources);
            resources["AppChromeFont"] = AppChromeFamily;
            resources["AppChromeFixedWidthFont"] = AppChromeFixedWidthFamily;
            resources["AppChromeFontSize"] = FontSize;
            resources["FileListSortGlyphFontSize"] = SortGlyphFontSize;
        }
    }
}

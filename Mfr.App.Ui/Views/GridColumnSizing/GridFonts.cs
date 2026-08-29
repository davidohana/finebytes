using Avalonia.Controls;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.GridColumnSizing
{
    /// <summary>
    /// File List / Rename List grid font names and sizes shared by theme resources and column measurement.
    /// </summary>
    internal static class GridFonts
    {
        /// <summary>
        /// Proportional File List / Rename List family list.
        /// </summary>
        public const string FileListFamilyName = "Segoe UI, SegoeUI";

        /// <summary>
        /// Fixed-width Rename List family list.
        /// </summary>
        public const string RenameListFixedWidthFamilyName = "Cascadia Mono, Consolas, monospace";

        /// <summary>
        /// Grid body and header size in device-independent pixels.
        /// </summary>
        public const double FontSize = 12;

        /// <summary>
        /// Sort and preview glyph size in device-independent pixels.
        /// </summary>
        public const double SortGlyphFontSize = 11;

        /// <summary>
        /// Proportional File List / Rename List font family.
        /// </summary>
        public static FontFamily FileListFamily { get; } = new(FileListFamilyName);

        /// <summary>
        /// Fixed-width Rename List font family.
        /// </summary>
        public static FontFamily RenameListFixedWidthFamily { get; } = new(RenameListFixedWidthFamilyName);

        /// <summary>
        /// Registers theme keys used by File List / Rename List styles.
        /// </summary>
        /// <param name="resources">Application resource dictionary.</param>
        public static void AddResources(IResourceDictionary resources)
        {
            ArgumentNullException.ThrowIfNull(resources);
            resources["FileListFont"] = FileListFamily;
            resources["RenameListFixedWidthFont"] = RenameListFixedWidthFamily;
            resources["FileListFontSize"] = FontSize;
            resources["FileListSortGlyphFontSize"] = SortGlyphFontSize;
        }
    }
}

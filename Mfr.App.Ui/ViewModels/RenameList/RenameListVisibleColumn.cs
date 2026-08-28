using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One visible Rename List grid column: field identity plus optional width override.
    /// </summary>
    /// <param name="Key">Field key (original or preview).</param>
    /// <param name="Width">
    /// Column width in pixels, or <see cref="UseCatalogDefaultWidth"/> to use the catalog default.
    /// </param>
    public sealed record RenameListVisibleColumn(RenameListFieldKey Key, int Width = -1)
    {
        /// <summary>
        /// Sentinel width: use <see cref="RenameListField.DefaultWidth"/> from the catalog.
        /// </summary>
        public const int UseCatalogDefaultWidth = -1;

        /// <summary>
        /// Builds the MFR7 default visible column list (catalog keys, catalog widths).
        /// </summary>
        /// <returns>Default visible columns in grid order.</returns>
        public static IReadOnlyList<RenameListVisibleColumn> CreateDefaults()
        {
            return [.. RenameListFieldCatalog.DefaultVisibleColumns.Select(key => new RenameListVisibleColumn(key))];
        }

        /// <summary>
        /// Resolves the grid width for this column.
        /// </summary>
        /// <returns>Explicit width when set; otherwise the catalog default for <see cref="Key"/>.</returns>
        public int ResolveWidth()
        {
            if (Width != UseCatalogDefaultWidth)
            {
                return Width;
            }

            if (!RenameListFieldCatalog.TryGetField(Key, out var field))
            {
                return 180;
            }

            return field.DefaultWidth;
        }
    }
}

using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One visible Rename List grid column: field identity plus optional width override.
    /// </summary>
    /// <param name="Key">Field key (original or preview).</param>
    /// <param name="Width">
    /// Column width in pixels, or <see cref="UseCatalogDefaultWidth"/> to use the catalog override when set.
    /// </param>
    public sealed record RenameListVisibleColumn(RenameListFieldKey Key, int Width = -1)
    {
        /// <summary>
        /// Sentinel width: use the catalog override when declared; otherwise fit the header text.
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
        /// Resolves an optional catalog width override for this column.
        /// </summary>
        /// <returns>
        /// User/session pixel width when set; otherwise the catalog override when declared; otherwise <see langword="null"/>.
        /// </returns>
        public int? ResolveCatalogWidth()
        {
            if (Width != UseCatalogDefaultWidth)
            {
                return Width;
            }

            if (!RenameListFieldCatalog.TryGetField(Key, out var field))
            {
                return null;
            }

            return field.DefaultWidth;
        }
    }
}

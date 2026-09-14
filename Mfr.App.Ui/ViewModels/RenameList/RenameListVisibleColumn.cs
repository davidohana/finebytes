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
        /// Maps a mixed original/preview column list to originals-only for A/B Mode.
        /// </summary>
        /// <param name="columns">Columns in left-to-right order (may include preview keys).</param>
        /// <returns>
        /// Originals-only list: each preview key becomes its matching original; first-seen order; widths prefer
        /// an existing original entry, else the preview width, else <see cref="UseCatalogDefaultWidth"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> is null.</exception>
        public static IReadOnlyList<RenameListVisibleColumn> NormalizeToOriginals(
            IReadOnlyList<RenameListVisibleColumn> columns
        )
        {
            ArgumentNullException.ThrowIfNull(columns);

            var keyToPreferredWidth = new Dictionary<RenameListFieldKey, int>();
            foreach (var column in columns)
            {
                var originalKey = column.Key.IsPreview
                    ? RenameListFieldKey.Original(column.Key.GroupId, column.Key.PropertyKey)
                    : column.Key;
                if (column.Key.IsPreview)
                {
                    keyToPreferredWidth.TryAdd(originalKey, column.Width);
                    continue;
                }

                keyToPreferredWidth[originalKey] = column.Width;
            }

            var normalized = new List<RenameListVisibleColumn>();
            var keyToIsSeen = new HashSet<RenameListFieldKey>();
            foreach (var column in columns)
            {
                var originalKey = column.Key.IsPreview
                    ? RenameListFieldKey.Original(column.Key.GroupId, column.Key.PropertyKey)
                    : column.Key;
                if (!keyToIsSeen.Add(originalKey))
                {
                    continue;
                }

                var width = keyToPreferredWidth.TryGetValue(originalKey, out var preferredWidth)
                    ? preferredWidth
                    : UseCatalogDefaultWidth;
                normalized.Add(new RenameListVisibleColumn(originalKey, width));
            }

            return normalized;
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

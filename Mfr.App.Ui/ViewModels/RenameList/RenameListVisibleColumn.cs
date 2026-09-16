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
        /// Copies widths from <paramref name="previous"/> onto matching keys in <paramref name="columns"/>.
        /// </summary>
        /// <param name="columns">New columns in left-to-right order.</param>
        /// <param name="previous">Prior columns whose widths should be kept when the key still exists.</param>
        /// <returns>
        /// Same keys and order as <paramref name="columns"/>; width from <paramref name="previous"/> when the key
        /// was present, otherwise the width already on that column.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="columns"/> or <paramref name="previous"/> is null.
        /// </exception>
        public static IReadOnlyList<RenameListVisibleColumn> WithPreservedWidths(
            IReadOnlyList<RenameListVisibleColumn> columns,
            IReadOnlyList<RenameListVisibleColumn> previous
        )
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(previous);

            if (previous.Count == 0)
            {
                return columns;
            }

            var keyToWidth = new Dictionary<RenameListFieldKey, int>(capacity: previous.Count);
            foreach (var column in previous)
            {
                keyToWidth[column.Key] = column.Width;
            }

            var preserved = new List<RenameListVisibleColumn>(capacity: columns.Count);
            foreach (var column in columns)
            {
                if (keyToWidth.TryGetValue(column.Key, out var width))
                {
                    preserved.Add(column with { Width = width });
                    continue;
                }

                preserved.Add(column);
            }

            return preserved;
        }

        /// <summary>
        /// Fills <see cref="UseCatalogDefaultWidth"/> entries from a remembered-width map.
        /// </summary>
        /// <param name="columns">Columns in left-to-right order.</param>
        /// <param name="keyToWidth">
        /// Remembered absolute pixel widths by field key, or <see langword="null"/> / empty to leave widths unchanged.
        /// </param>
        /// <returns>
        /// Same keys and order; remembered width when the column still uses the catalog default and the key
        /// is present in <paramref name="keyToWidth"/>; otherwise the width already on that column.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> is null.</exception>
        public static IReadOnlyList<RenameListVisibleColumn> WithRememberedWidths(
            IReadOnlyList<RenameListVisibleColumn> columns,
            IReadOnlyDictionary<RenameListFieldKey, int>? keyToWidth
        )
        {
            ArgumentNullException.ThrowIfNull(columns);

            if (keyToWidth is null || keyToWidth.Count == 0)
            {
                return columns;
            }

            var applied = new List<RenameListVisibleColumn>(capacity: columns.Count);
            foreach (var column in columns)
            {
                if (
                    column.Width == UseCatalogDefaultWidth
                    && keyToWidth.TryGetValue(column.Key, out var width)
                    && width > 0
                )
                {
                    applied.Add(column with { Width = width });
                    continue;
                }

                applied.Add(column);
            }

            return applied;
        }

        /// <summary>
        /// Preserves widths from <paramref name="previous"/>, then fills remaining catalog defaults from remembered widths.
        /// </summary>
        /// <param name="columns">New columns in left-to-right order.</param>
        /// <param name="previous">Prior columns whose widths should be kept when the key still exists.</param>
        /// <param name="rememberedWidths">
        /// Absolute widths for catalog-default keys, or <see langword="null"/> / empty to skip remember fill.
        /// </param>
        /// <returns>Preserved then remember-filled columns (same keys/order as <paramref name="columns"/>).</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="columns"/> or <paramref name="previous"/> is null.
        /// </exception>
        public static IReadOnlyList<RenameListVisibleColumn> WithPreservedThenRememberedWidths(
            IReadOnlyList<RenameListVisibleColumn> columns,
            IReadOnlyList<RenameListVisibleColumn> previous,
            IReadOnlyDictionary<RenameListFieldKey, int>? rememberedWidths
        )
        {
            return WithRememberedWidths(WithPreservedWidths(columns, previous), rememberedWidths);
        }

        /// <summary>
        /// Builds the Set-from-filters column list: catalog defaults, append relevant keys, optional A/B
        /// normalize, preserve prior widths, then fill catalog defaults from remembered widths.
        /// </summary>
        /// <param name="relevantKeys">Filter-chain field keys to append when missing from defaults.</param>
        /// <param name="previous">Prior visible/draft columns for session width preserve.</param>
        /// <param name="rememberedWidths">
        /// Absolute widths for catalog-default keys, or <see langword="null"/> / empty to skip remember fill.
        /// </param>
        /// <param name="originalsOnly">When <see langword="true"/>, normalize to originals-only (A/B Mode).</param>
        /// <returns>Set-from-filters columns in left-to-right order.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="relevantKeys"/> or <paramref name="previous"/> is null.
        /// </exception>
        public static IReadOnlyList<RenameListVisibleColumn> BuildSetFromFiltersColumns(
            IReadOnlyList<RenameListFieldKey> relevantKeys,
            IReadOnlyList<RenameListVisibleColumn> previous,
            IReadOnlyDictionary<RenameListFieldKey, int>? rememberedWidths,
            bool originalsOnly
        )
        {
            ArgumentNullException.ThrowIfNull(relevantKeys);

            var columns = CreateDefaults().ToList();
            var keyToIsPresent = columns.Select(column => column.Key).ToHashSet();
            foreach (var key in relevantKeys)
            {
                if (!keyToIsPresent.Add(key))
                {
                    continue;
                }

                columns.Add(new RenameListVisibleColumn(key));
            }

            var normalized = originalsOnly ? NormalizeToOriginals(columns) : columns;
            return WithPreservedThenRememberedWidths(normalized, previous, rememberedWidths);
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
                var originalKey = column.Key.AsOriginal();
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
                var originalKey = column.Key.AsOriginal();
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
        /// Inserts a preview companion immediately after each original that supports preview and does not
        /// already have that preview key in the list (catalog default width).
        /// </summary>
        /// <param name="columns">Columns in left-to-right order (typically originals-only after A/B Mode).</param>
        /// <returns>
        /// Same order with missing preview companions inserted after their originals; idempotent when
        /// companions are already present.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> is null.</exception>
        public static IReadOnlyList<RenameListVisibleColumn> WithPreviewCompanions(
            IReadOnlyList<RenameListVisibleColumn> columns
        )
        {
            ArgumentNullException.ThrowIfNull(columns);

            var keyToIsPresent = columns.Select(column => column.Key).ToHashSet();
            var expanded = new List<RenameListVisibleColumn>(capacity: columns.Count * 2);
            foreach (var column in columns)
            {
                expanded.Add(column);
                if (column.Key.IsPreview)
                {
                    continue;
                }

                if (!RenameListFieldCatalog.TryGetField(column.Key, out var field) || !field.SupportsPreview)
                {
                    continue;
                }

                if (!keyToIsPresent.Add(field.PreviewKey))
                {
                    continue;
                }

                expanded.Add(new RenameListVisibleColumn(field.PreviewKey));
            }

            return expanded;
        }

        /// <summary>
        /// Maps each key to its original form and drops later duplicates, preserving first-seen order.
        /// </summary>
        /// <param name="keys">Keys in left-to-right order (may include preview keys).</param>
        /// <returns>Originals-only keys, first-seen order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> is null.</exception>
        public static IReadOnlyList<RenameListFieldKey> ToOriginalKeysFirstSeen(IReadOnlyList<RenameListFieldKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var mappedKeys = new List<RenameListFieldKey>();
            var keyToIsSeen = new HashSet<RenameListFieldKey>();
            foreach (var key in keys)
            {
                var originalKey = key.AsOriginal();
                if (!keyToIsSeen.Add(originalKey))
                {
                    continue;
                }

                mappedKeys.Add(originalKey);
            }

            return mappedKeys;
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

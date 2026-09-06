using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// One insert-picker row: a <see cref="FormatTokenCatalogEntry.GroupPath"/> folder or a catalog leaf.
    /// </summary>
    public sealed class FormatInsertPickerNode
    {
        private FormatInsertPickerNode(
            string title,
            FormatTokenCatalogEntry? entry,
            IReadOnlyList<FormatInsertPickerNode> children,
            bool showGroupSubtitle
        )
        {
            Title = title;
            Entry = entry;
            Children = children;
            ShowGroupSubtitle = showGroupSubtitle;
        }

        /// <summary>
        /// Gets the group folder name or catalog <see cref="FormatTokenCatalogEntry.DisplayName"/>.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the catalog row when this node is a leaf; otherwise <see langword="null"/>.
        /// </summary>
        public FormatTokenCatalogEntry? Entry { get; }

        /// <summary>
        /// Gets nested group / leaf children (empty for leaves).
        /// </summary>
        public IReadOnlyList<FormatInsertPickerNode> Children { get; }

        /// <summary>
        /// Gets whether the leaf should show <see cref="FormatTokenCatalogEntry.GroupPath"/> under the title
        /// (flat search results only).
        /// </summary>
        public bool ShowGroupSubtitle { get; }

        /// <summary>
        /// Gets whether this node is a group folder (not insertable).
        /// </summary>
        public bool IsGroup => Entry is null;

        /// <summary>
        /// Creates a nested group folder.
        /// </summary>
        /// <param name="title">Single path segment (for example <c>Audio</c> or <c>Tag</c>).</param>
        /// <param name="children">Child groups and/or leaves.</param>
        /// <returns>Group node.</returns>
        public static FormatInsertPickerNode Group(string title, IReadOnlyList<FormatInsertPickerNode> children)
        {
            return new FormatInsertPickerNode(title, entry: null, children, showGroupSubtitle: false);
        }

        /// <summary>
        /// Creates an insertable catalog leaf.
        /// </summary>
        /// <param name="entry">Catalog row.</param>
        /// <param name="showGroupSubtitle">When <see langword="true"/>, show <see cref="FormatTokenCatalogEntry.GroupPath"/>.</param>
        /// <returns>Leaf node.</returns>
        public static FormatInsertPickerNode Leaf(FormatTokenCatalogEntry entry, bool showGroupSubtitle)
        {
            return new FormatInsertPickerNode(entry.DisplayName, entry, children: [], showGroupSubtitle);
        }
    }
}

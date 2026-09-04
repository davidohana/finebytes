using Mfr.Utils;

namespace Mfr.Engine.Preview
{
    /// <summary>
    /// Collects directory items whose preview full path differs from the original.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by commit planning, conflict detection, and folder-descendant rebase so each keeps
    /// the same definition of an in-batch folder path rename.
    /// </para>
    /// </remarks>
    internal static class PreviewFolderPathChanges
    {
        /// <summary>
        /// Returns directory items in <paramref name="items"/> whose preview path changed.
        /// </summary>
        /// <param name="items">Rename items already scoped by the caller (for example PreviewOk only).</param>
        /// <returns>Folder path renames from the given set, in encounter order.</returns>
        internal static List<RenameItem> Collect(IEnumerable<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var folderRenames = new List<RenameItem>();
            foreach (var item in items)
            {
                if (item.Original.Attributes.IsDirectory() && !item.IsPreviewPathUnchanged())
                {
                    folderRenames.Add(item);
                }
            }

            return folderRenames;
        }
    }
}

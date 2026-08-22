using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.RenameList
{
    /// <summary>
    /// Maps engine rename items to Rename List grid rows.
    /// </summary>
    internal static class RenameListEntryMapper
    {
        /// <summary>
        /// Builds a grid row from a rename item (identity preview until filter preview exists).
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <returns>Row view model for the Rename List grid.</returns>
        public static RenameListEntry ToEntry(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var original = item.Original;
            var fullFileName = original.Prefix + original.Extension;
            var isDirectory = original.Attributes.IsDirectory();

            return new RenameListEntry
            {
                FileFolder = isDirectory ? "Folder" : "File",
                ParentFolder = original.DirectoryPath,
                FullFileName = fullFileName,
                FullFileNamePreview = fullFileName,
            };
        }
    }
}

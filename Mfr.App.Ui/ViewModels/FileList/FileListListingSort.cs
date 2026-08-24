using Mfr.App.Ui.Services.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// Windows Explorer-style File List sort: listing group, then folders, then the column.
    /// </summary>
    internal static class FileListListingSort
    {
        /// <summary>
        /// Maps a grid column binding path to a known <see cref="FileListEntry"/> property name.
        /// </summary>
        /// <param name="memberPath">Binding path from the Report view, or <see langword="null"/>.</param>
        /// <returns>Name, Date modified, Type, or Size; unknown paths become Name.</returns>
        public static string NormalizeMemberPath(string? memberPath)
        {
            if (string.Equals(memberPath, nameof(FileListEntry.LastWriteTime), StringComparison.Ordinal))
            {
                return nameof(FileListEntry.LastWriteTime);
            }

            if (string.Equals(memberPath, nameof(FileListEntry.Type), StringComparison.Ordinal))
            {
                return nameof(FileListEntry.Type);
            }

            if (string.Equals(memberPath, nameof(FileListEntry.Length), StringComparison.Ordinal))
            {
                return nameof(FileListEntry.Length);
            }

            return nameof(FileListEntry.Name);
        }

        /// <summary>
        /// Sorts <paramref name="items"/> in place using the current column and direction.
        /// </summary>
        /// <param name="items">Listed rows to reorder.</param>
        /// <param name="memberPath">Normalized <see cref="FileListEntry"/> property name.</param>
        /// <param name="isAscending">Whether the column sort is ascending.</param>
        public static void Apply(List<FileListListedItem> items, string memberPath, bool isAscending)
        {
            items.Sort((left, right) => _Compare(left, right, memberPath, isAscending));
        }

        private static int _Compare(
            FileListListedItem left,
            FileListListedItem right,
            string memberPath,
            bool isAscending
        )
        {
            var groupCmp = left.ListingGroup.CompareTo(right.ListingGroup);
            if (groupCmp != 0)
            {
                return groupCmp;
            }

            var folderCmp = right.IsDirectory.CompareTo(left.IsDirectory);
            if (folderCmp != 0)
            {
                return folderCmp;
            }

            var fieldCmp = _CompareSortField(left, right, memberPath);
            if (fieldCmp == 0)
            {
                fieldCmp = PathComparers.Os.Compare(left.Name, right.Name);
            }

            return isAscending ? fieldCmp : -fieldCmp;
        }

        private static int _CompareSortField(FileListListedItem left, FileListListedItem right, string memberPath)
        {
            if (memberPath == nameof(FileListEntry.LastWriteTime))
            {
                return Comparer<DateTime?>.Default.Compare(left.LastWriteTime, right.LastWriteTime);
            }

            if (memberPath == nameof(FileListEntry.Length))
            {
                return Comparer<long?>.Default.Compare(left.Length, right.Length);
            }

            if (memberPath == nameof(FileListEntry.Type))
            {
                return PathComparers.Os.Compare(
                    FileListEntryDisplay.TypeLabel(left),
                    FileListEntryDisplay.TypeLabel(right)
                );
            }

            return PathComparers.Os.Compare(left.Name, right.Name);
        }
    }
}

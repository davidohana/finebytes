namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Shared Rename List test helpers.
    /// </summary>
    internal static class RenameListTestHelpers
    {
        /// <summary>
        /// Builds a one-field session sort list for <see cref="App.Ui.ViewModels.RenameList.RenameListViewModel.ApplySession"/>.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <param name="descending">When <see langword="true"/>, sort descending.</param>
        /// <returns>Single-element session field list.</returns>
        internal static List<SessionStateRenameListSortField> SortSession(
            RenameListSortColumn column,
            bool descending = false
        )
        {
            return [new SessionStateRenameListSortField(column, descending)];
        }
    }
}

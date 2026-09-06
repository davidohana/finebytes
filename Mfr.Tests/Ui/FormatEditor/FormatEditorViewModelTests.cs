using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatEditorViewModel"/>.
    /// </summary>
    public sealed class FormatEditorViewModelTests
    {
        /// <summary>
        /// Verifies an empty search nests catalog rows under <see cref="FormatTokenCatalogEntry.GroupPath"/>.
        /// </summary>
        [Fact]
        public void SearchText_Empty_NestsByGroupPath()
        {
            var vm = _CreateVm();

            Assert.True(vm.IsGrouped);
            Assert.All(vm.VisibleItems, n => Assert.True(n.IsGroup));
            Assert.Contains(vm.VisibleItems, n => n.Title == "File Name");
            Assert.Contains(vm.VisibleItems, n => n.Title == "Audio");

            var audio = Assert.Single(vm.VisibleItems, n => n.Title == "Audio");
            Assert.Contains(audio.Children, n => n.Title == "Tag");
            Assert.Contains(audio.Children, n => n.Title == "MP3");

            var fileName = Assert.Single(vm.VisibleItems, n => n.Title == "File Name");
            var fileNameLeaf = Assert.Single(fileName.Children, n => n.Entry?.CanonicalName == "file-name");
            Assert.False(fileNameLeaf.ShowGroupSubtitle);
            Assert.Equal(FormatTokenCatalog.Entries.Count, _CountLeaves(vm.VisibleItems));
        }

        /// <summary>
        /// Verifies whitespace-only search is treated as empty (grouped browse).
        /// </summary>
        [Fact]
        public void SearchText_WhitespaceOnly_KeepsGroupedItems()
        {
            var vm = _CreateVm();

            vm.SearchText = "   ";
            Assert.True(vm.IsGrouped);
            Assert.All(vm.VisibleItems, n => Assert.True(n.IsGroup));
        }

        /// <summary>
        /// Verifies search filters to a flat leaf list by name and description.
        /// </summary>
        [Fact]
        public void SearchText_FiltersVisibleItemsToFlatLeaves()
        {
            var vm = _CreateVm();

            vm.SearchText = "counter";
            Assert.False(vm.IsGrouped);
            Assert.Contains(vm.VisibleItems, n => n.Entry?.CanonicalName == "counter");
            Assert.All(vm.VisibleItems, n => Assert.False(n.IsGroup));
            Assert.All(vm.VisibleItems, n => Assert.True(n.ShowGroupSubtitle));
            Assert.All(
                vm.VisibleItems,
                n =>
                {
                    var e = n.Entry!;
                    Assert.True(
                        e.CanonicalName.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.DisplayName.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.ShortDescription.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.GroupPath.Contains("counter", StringComparison.OrdinalIgnoreCase)
                    );
                }
            );
        }

        /// <summary>
        /// Verifies search matches <see cref="FormatTokenCatalogEntry.GroupPath"/> even when display names do not.
        /// </summary>
        [Fact]
        public void SearchText_MatchesGroupPath()
        {
            var vm = _CreateVm();

            vm.SearchText = "Audio\\MP3";
            Assert.False(vm.IsGrouped);
            Assert.NotEmpty(vm.VisibleItems);
            Assert.All(vm.VisibleItems, n => Assert.False(n.IsGroup));
            Assert.All(
                vm.VisibleItems,
                n => Assert.Contains("Audio\\MP3", n.Entry!.GroupPath, StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Verifies InsertEntry invokes the insert callback with catalog InsertText.
        /// </summary>
        [Fact]
        public void InsertEntryCommand_InsertsCatalogText()
        {
            var inserted = new List<string>();
            var vm = new FormatEditorViewModel(
                insertText: inserted.Add,
                jumpToError: static () => { },
                editUnderCaret: static () => { }
            );

            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            vm.SearchText = "file";
            Assert.True(vm.InsertEntryCommand.CanExecute(entry));
            vm.InsertEntryCommand.Execute(entry);

            Assert.Equal([entry.InsertText], inserted);
            Assert.Equal(string.Empty, vm.SearchText);
            Assert.True(vm.IsGrouped);
        }

        /// <summary>
        /// Verifies Validate sets error state for unknown tokens.
        /// </summary>
        [Fact]
        public void Validate_UnknownToken_SetsError()
        {
            var vm = _CreateVm();

            vm.Validate("<nope>");
            Assert.True(vm.HasError);
            Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
            Assert.NotNull(vm.LastParseResult);
            Assert.False(vm.LastParseResult.Success);
        }

        /// <summary>
        /// Verifies long parse errors are truncated inline while FullErrorMessage stays complete.
        /// </summary>
        [Fact]
        public void Validate_LongError_TruncatesInlineMessage()
        {
            var vm = _CreateVm();

            // Unknown token with a very long name forces a long ErrorMessage from TryValidate.
            var longName = new string('a', 200);
            vm.Validate($"<{longName}>");
            Assert.True(vm.HasError);
            Assert.NotNull(vm.FullErrorMessage);
            Assert.True(vm.FullErrorMessage.Length > vm.ErrorMessage.Length);
            Assert.EndsWith("…", vm.ErrorMessage);
        }

        private static FormatEditorViewModel _CreateVm()
        {
            return new(insertText: static _ => { }, jumpToError: static () => { }, editUnderCaret: static () => { });
        }

        private static int _CountLeaves(IEnumerable<FormatInsertPickerNode> nodes)
        {
            return nodes.Sum(n => n.IsGroup ? _CountLeaves(n.Children) : 1);
        }
    }
}

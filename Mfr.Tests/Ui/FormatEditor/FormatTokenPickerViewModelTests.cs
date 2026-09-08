using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatTokenPickerViewModel"/>.
    /// </summary>
    public sealed class FormatTokenPickerViewModelTests
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
            var vm = new FormatTokenPickerViewModel(inserted.Add);

            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            vm.SearchText = "file";
            Assert.True(vm.InsertEntryCommand.CanExecute(entry));
            vm.InsertEntryCommand.Execute(entry);

            Assert.Equal([entry.InsertText], inserted);
            Assert.Equal(string.Empty, vm.SearchText);
            Assert.True(vm.IsGrouped);
        }

        private static FormatTokenPickerViewModel _CreateVm()
        {
            return new(static _ => { });
        }

        private static int _CountLeaves(IEnumerable<FormatTokenPickerNode> nodes)
        {
            return nodes.Sum(n => n.IsGroup ? _CountLeaves(n.Children) : 1);
        }
    }
}

using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Rename List Refresh command tests.
    /// </summary>
    public sealed class RenameListViewModelRefreshTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies Refresh is unavailable while the list is empty.
        /// </summary>
        [Fact]
        public void Refresh_unavailable_when_list_empty()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();

            Assert.False(renameListViewModel.RefreshCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Refresh re-reads extended original fields after disk changes.
        /// </summary>
        [Fact]
        public async Task Refresh_updates_visible_size_column()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "size.txt");
            File.WriteAllText(path, "a");

            var sizeKey = RenameListFieldKey.Original(ExtendedRenameListFields.Group, ExtendedSizeField.SizeKey);
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(sizeKey),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
            ]);

            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var entry = Assert.Single(renameListViewModel.Entries);
            var before = entry.GetFieldText(sizeKey);

            File.WriteAllText(path, new string('x', 4096));

            await renameListViewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.NotEqual(before, entry.GetFieldText(sizeKey));
        }

        /// <summary>
        /// Verifies Refresh reloads audio metadata shown in visible columns.
        /// </summary>
        [Fact]
        public async Task Refresh_reloads_title_after_tag_change()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "Before", album: null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(titleKey),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
            ]);

            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.Equal("Before", entry.GetFieldText(titleKey));

            TaggedMinimalWav.WriteTagged(path, title: "After", album: null);

            await renameListViewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.Equal("After", entry.GetFieldText(titleKey));
        }

        /// <summary>
        /// Verifies a deleted file stays present in the grid until Refresh snapshots missing-from-disk.
        /// </summary>
        [Fact]
        public async Task Refresh_snapshots_missing_from_disk()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "vanish.txt");
            File.WriteAllText(path, "x");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var entry = Assert.Single(renameListViewModel.Entries);

            File.Delete(path);
            Assert.False(entry.IsMissingFromDisk);

            await renameListViewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.True(entry.IsMissingFromDisk);
        }
    }
}

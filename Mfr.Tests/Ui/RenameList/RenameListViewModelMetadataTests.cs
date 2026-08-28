using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Eager metadata hydration for Rename List columns and Auto-Sort.
    /// </summary>
    public sealed class RenameListViewModelMetadataTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new(pinAddPolicy: true);

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies add hydrates metadata when a visible audio column is configured.
        /// </summary>
        [Fact]
        public async Task Add_With_Title_Column_Hydrates_New_Rows()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "AddTitle", album: null);

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
            Assert.Equal("AddTitle", entry.GetFieldText(titleKey));
            Assert.True(entry.EngineItem.EmbeddedTagsLoadAttempted);
        }

        /// <summary>
        /// Verifies shuttle apply hydrates existing rows when a new metadata family is added.
        /// </summary>
        [Fact]
        public async Task ApplyFieldShuttleAsync_Adds_Title_Column_And_Hydrates_Existing_Rows()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "ShuttleTitle", album: null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.False(entry.EngineItem.EmbeddedTagsLoadAttempted);

            await renameListViewModel
                .ApplyFieldShuttleAsync(
                    [
                        new RenameListVisibleColumn(titleKey),
                        new RenameListVisibleColumn(
                            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                        ),
                    ],
                    []
                )
                .ConfigureAwait(true);

            Assert.Equal("ShuttleTitle", entry.GetFieldText(titleKey));
            Assert.True(entry.EngineItem.EmbeddedTagsLoadAttempted);
        }

        /// <summary>
        /// Verifies GetFieldText does not open files; the grid reads memory only.
        /// </summary>
        [Fact]
        public async Task GetFieldText_Does_Not_Load_Metadata()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "LazyTitle", album: null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.Equal(string.Empty, entry.GetFieldText(titleKey));
            Assert.False(entry.EngineItem.EmbeddedTagsLoadAttempted);
        }

        /// <summary>
        /// Verifies adding a second field from an already-hydrated family does not reopen files.
        /// </summary>
        [Fact]
        public async Task ApplyFieldShuttleAsync_Same_Family_Does_Not_Reload()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "FamilyTitle", album: "FamilyAlbum");

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var albumKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Album");
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(titleKey),
                new RenameListVisibleColumn(fullNameKey),
            ]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.True(entry.EngineItem.EmbeddedTagsLoadAttempted);

            await renameListViewModel
                .ApplyFieldShuttleAsync(
                    [
                        new RenameListVisibleColumn(titleKey),
                        new RenameListVisibleColumn(albumKey),
                        new RenameListVisibleColumn(fullNameKey),
                    ],
                    []
                )
                .ConfigureAwait(true);

            Assert.False(renameListViewModel.IsAdding);
            Assert.Equal("FamilyTitle", entry.GetFieldText(titleKey));
            Assert.Equal("FamilyAlbum", entry.GetFieldText(albumKey));
        }

        /// <summary>
        /// Verifies canceling shuttle hydrate leaves columns unchanged.
        /// </summary>
        [Fact]
        public async Task ApplyFieldShuttleAsync_Cancel_Does_Not_Apply_Columns()
        {
            var dir = _context.CreateTempDir();
            var paths = Enumerable
                .Range(0, 40)
                .Select(i =>
                {
                    var path = Path.Combine(dir, $"tagged_{i}.wav");
                    TaggedMinimalWav.WriteTagged(path, title: $"Title{i}", album: null);
                    return path;
                })
                .ToList();

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync(paths).ConfigureAwait(true);

            var originalColumns = renameListViewModel.VisibleColumns.ToList();
            var shuttle = renameListViewModel.ApplyFieldShuttleAsync(
                [
                    new RenameListVisibleColumn(titleKey),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                    ),
                ],
                []
            );

            await _WaitUntilAsync(() => renameListViewModel.IsAdding).ConfigureAwait(true);
            renameListViewModel.AddProgress.CancelCommand.Execute(null);
            await shuttle.ConfigureAwait(true);

            Assert.Equal(originalColumns, renameListViewModel.VisibleColumns);
            Assert.DoesNotContain(renameListViewModel.VisibleColumns, column => column.Key == titleKey);
        }

        /// <summary>
        /// Verifies header Auto-Sort on a metadata field hydrates then sorts off the UI thread.
        /// </summary>
        [Fact]
        public async Task SortByFieldKey_On_Title_Hydrates_Then_Sorts()
        {
            var dir = _context.CreateTempDir();
            var betaPath = Path.Combine(dir, "beta.wav");
            var alphaPath = Path.Combine(dir, "alpha.wav");
            TaggedMinimalWav.WriteTagged(betaPath, title: "Beta", album: null);
            TaggedMinimalWav.WriteTagged(alphaPath, title: "Alpha", album: null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([betaPath, alphaPath]).ConfigureAwait(true);

            renameListViewModel.SortByFieldKey(titleKey);
            await _WaitForBackgroundAsync(renameListViewModel).ConfigureAwait(true);

            Assert.Equal(alphaPath, renameListViewModel.Entries[0].EngineItem.Original.FullPath);
            Assert.All(renameListViewModel.Entries, entry => Assert.True(entry.EngineItem.EmbeddedTagsLoadAttempted));
        }

        private static async Task _WaitUntilAsync(Func<bool> condition)
        {
            var deadline = Environment.TickCount64 + 10_000;
            while (!condition() && Environment.TickCount64 < deadline)
            {
                await Task.Delay(20).ConfigureAwait(true);
            }

            Assert.True(condition());
        }

        private static async Task _WaitForBackgroundAsync(RenameListViewModel viewModel)
        {
            var deadline = Environment.TickCount64 + 10_000;
            while (viewModel.IsAdding && Environment.TickCount64 < deadline)
            {
                await Task.Delay(20).ConfigureAwait(true);
            }

            Assert.False(viewModel.IsAdding);
        }
    }
}

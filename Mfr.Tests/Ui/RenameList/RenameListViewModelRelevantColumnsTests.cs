using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Add / Replace Fields by Applied Filters on <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed class RenameListViewModelRelevantColumnsTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies both commands are disabled when the applied filter chain is empty.
        /// </summary>
        [Fact]
        public void RelevantColumns_commands_disabled_when_chain_empty()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);

            Assert.False(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.False(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies both commands enable when the chain becomes non-empty and disable again when cleared.
        /// </summary>
        [Fact]
        public void RelevantColumns_commands_track_chain_count()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);

            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            Assert.True(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.True(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));

            appliedFilters.RemoveSelectedCommand.Execute(null);

            Assert.False(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.False(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Add appends missing relevant keys at the end without changing existing order or widths.
        /// </summary>
        [Fact]
        public async Task AddRelevantColumns_merges_missing_keys_at_end()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, Width: 180),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
            ]);

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            Assert.Equal(
                [
                    new RenameListVisibleColumn(folderKey, Width: 180),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                    ),
                    new RenameListVisibleColumn(nameOriginal),
                    new RenameListVisibleColumn(namePreview),
                ],
                renameListViewModel.VisibleColumns
            );
        }

        /// <summary>
        /// Verifies Add does not duplicate keys already visible.
        /// </summary>
        [Fact]
        public async Task AddRelevantColumns_dedupes_already_visible_keys()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(nameOriginal, Width: 120),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
            ]);

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(nameOriginal, Width: 120),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                    ),
                    new RenameListVisibleColumn(namePreview),
                ],
                renameListViewModel.VisibleColumns
            );
        }

        /// <summary>
        /// Verifies Replace resets to catalog defaults then appends remaining relevant keys.
        /// </summary>
        [Fact]
        public async Task ReplaceWithRelevantColumns_defaults_then_appends_relevant()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    Width: 90
                ),
            ]);

            await renameListViewModel.ReplaceWithRelevantColumnsCommand.ExecuteAsync(null);

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var expected = RenameListVisibleColumn.CreateDefaults().ToList();
            expected.Add(new RenameListVisibleColumn(nameOriginal));
            expected.Add(new RenameListVisibleColumn(namePreview));
            Assert.Equal(expected, renameListViewModel.VisibleColumns);
        }

        /// <summary>
        /// Verifies Replace with an unmapped chain still applies catalog defaults only.
        /// </summary>
        [Fact]
        public async Task ReplaceWithRelevantColumns_empty_map_applies_defaults_only()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new Id3v2FrameTarget("TIT2")), "Remove Spaces");

            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title"),
                    Width: 200
                ),
            ]);

            await renameListViewModel.ReplaceWithRelevantColumnsCommand.ExecuteAsync(null);

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
        }

        /// <summary>
        /// Verifies Add with an unmapped chain leaves visible columns unchanged.
        /// </summary>
        [Fact]
        public async Task AddRelevantColumns_empty_map_is_noop()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new Id3v2FrameTarget("TIT2")), "Remove Spaces");

            var before = renameListViewModel.VisibleColumns.ToList();
            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            Assert.Equal(before, renameListViewModel.VisibleColumns);
        }

        /// <summary>
        /// Verifies Add hydrates metadata for newly appended columns (same path as field-shuttle apply).
        /// </summary>
        [Fact]
        public async Task AddRelevantColumns_hydrates_metadata_for_new_columns()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "RelevantTitle", album: null);

            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(dir, appliedFilters: appliedFilters);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            appliedFilters.AddAndSelect(
                new FormatterFilter(new FilePrefixTarget(), new FormatterOptions("<audio-title>")),
                "Formatter"
            );

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.False(entry.EngineItem.TagLibLoadAttempted);

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            Assert.Contains(renameListViewModel.VisibleColumns, column => column.Key == titleKey);
            Assert.Equal("RelevantTitle", entry.GetFieldText(titleKey));
            Assert.True(entry.EngineItem.TagLibLoadAttempted);
        }
    }
}

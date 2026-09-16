using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Formatting;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Add / Set columns from filters on <see cref="RenameListViewModel"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameListViewModelRelevantColumnsTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies both commands are disabled when the Filter Chain is empty.
        /// </summary>
        [Fact]
        public void RelevantColumns_commands_disabled_when_chain_empty()
        {
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);

            Assert.False(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.False(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies both commands enable when the chain becomes non-empty and disable again when cleared.
        /// </summary>
        [Fact]
        public void RelevantColumns_commands_track_chain_count()
        {
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);

            filterChain.AddAndSelect(new RemoveSpacesFilter(new FileNameTarget()), "Remove Spaces");

            Assert.True(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.True(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));

            filterChain.RemoveSelectedCommand.Execute(null);

            Assert.False(renameListViewModel.AddRelevantColumnsCommand.CanExecute(null));
            Assert.False(renameListViewModel.ReplaceWithRelevantColumnsCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies Add appends missing relevant keys at the end without changing existing order or widths.
        /// </summary>
        [Fact]
        public async Task AddRelevantColumns_merges_missing_keys_at_end()
        {
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new FileNameTarget()), "Remove Spaces");

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
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new FileNameTarget()), "Remove Spaces");

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
        /// Verifies Replace resets to catalog defaults then appends remaining relevant keys,
        /// preserving widths for keys that were already visible.
        /// </summary>
        [Fact]
        public async Task ReplaceWithRelevantColumns_defaults_then_appends_relevant()
        {
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new FileNameTarget()), "Remove Spaces");

            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, Width: 180),
                new RenameListVisibleColumn(nameOriginal, Width: 90),
            ]);

            await renameListViewModel.ReplaceWithRelevantColumnsCommand.ExecuteAsync(null);

            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var expected = RenameListVisibleColumn
                .WithPreservedWidths(
                    RenameListVisibleColumn.CreateDefaults(),
                    [new RenameListVisibleColumn(folderKey, Width: 180)]
                )
                .ToList();
            expected.Add(new RenameListVisibleColumn(nameOriginal, Width: 90));
            expected.Add(new RenameListVisibleColumn(namePreview));
            Assert.Equal(expected, renameListViewModel.VisibleColumns);
        }

        /// <summary>
        /// Verifies Replace with an unmapped chain still applies catalog defaults only.
        /// </summary>
        [Fact]
        public async Task ReplaceWithRelevantColumns_empty_map_applies_defaults_only()
        {
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new Id3v2FrameTarget("TIT2")), "Remove Spaces");

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
            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new Id3v2FrameTarget("TIT2")), "Remove Spaces");

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

            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(dir, filterChain: filterChain);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            filterChain.AddAndSelect(
                new FormatterFilter(new FileNameTarget(), new FormatterOptions("<audio-title>")),
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

        /// <summary>
        /// Verifies resize remembers width when the option is on, and Add reuses it after hide.
        /// </summary>
        [Fact]
        public async Task Remembered_widths_update_on_resize_and_reuse_on_add()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = true;

            var filterChain = new FilterChainViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(filterChain: filterChain);
            filterChain.AddAndSelect(new RemoveSpacesFilter(new FileNameTarget()), "Remove Spaces");

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(nameOriginal, Width: 90),
                new RenameListVisibleColumn(folderKey),
            ]);

            renameListViewModel.UpdateVisibleColumnWidth(nameOriginal, 140);
            Assert.Equal(140, renameListViewModel.RememberedColumnWidths[nameOriginal]);

            renameListViewModel.HideColumn(nameOriginal);
            Assert.DoesNotContain(renameListViewModel.VisibleColumns, column => column.Key == nameOriginal);

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            Assert.Equal(
                140,
                Assert.Single(renameListViewModel.VisibleColumns, column => column.Key == nameOriginal).Width
            );
        }

        /// <summary>
        /// Verifies resize does not update the remembered map when the option is off.
        /// </summary>
        [Fact]
        public void Remembered_widths_skip_update_when_option_off()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = false;

            var renameListViewModel = _context.CreateRenameListViewModel();
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.ApplySessionSection(
                new RenameListPrefs
                {
                    VisibleColumns = [new RenameListVisibleColumnSpec(nameOriginal, Width: 90)],
                    ColumnWidths = [new RenameListVisibleColumnSpec(nameOriginal, Width: 200)],
                }
            );

            renameListViewModel.UpdateVisibleColumnWidth(nameOriginal, 140);

            Assert.Equal(200, renameListViewModel.RememberedColumnWidths[nameOriginal]);
            Assert.Equal(140, renameListViewModel.VisibleColumns[0].Width);
        }

        /// <summary>
        /// Verifies CaptureSession includes remembered widths and seeds from visible absolute widths.
        /// </summary>
        [Fact]
        public void CaptureSession_includes_remembered_column_widths()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = true;

            var renameListViewModel = _context.CreateRenameListViewModel();
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(nameOriginal, Width: 155)]);

            var captured = renameListViewModel.CaptureSession();

            Assert.NotNull(captured.ColumnWidths);
            Assert.Contains(captured.ColumnWidths, spec => spec.Key == nameOriginal && spec.Width == 155);
        }

        /// <summary>
        /// Verifies session restore fills catalog-default visible widths from the remembered map.
        /// </summary>
        [Fact]
        public void ApplySessionSection_fills_catalog_default_visible_from_remembered()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = true;

            var renameListViewModel = _context.CreateRenameListViewModel();
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.ApplySessionSection(
                new RenameListPrefs
                {
                    VisibleColumns = [new RenameListVisibleColumnSpec(nameOriginal)],
                    ColumnWidths = [new RenameListVisibleColumnSpec(nameOriginal, Width: 188)],
                }
            );

            Assert.Equal(188, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(188, renameListViewModel.RememberedColumnWidths[nameOriginal]);
        }

        /// <summary>
        /// Verifies session restore does not fill catalog-default visible widths when remembering is off.
        /// </summary>
        [Fact]
        public void ApplySessionSection_skips_fill_when_option_off()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = false;

            var renameListViewModel = _context.CreateRenameListViewModel();
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.ApplySessionSection(
                new RenameListPrefs
                {
                    VisibleColumns = [new RenameListVisibleColumnSpec(nameOriginal)],
                    ColumnWidths = [new RenameListVisibleColumnSpec(nameOriginal, Width: 188)],
                }
            );

            Assert.Equal(RenameListVisibleColumn.UseCatalogDefaultWidth, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(188, renameListViewModel.RememberedColumnWidths[nameOriginal]);
        }

        /// <summary>
        /// Verifies preset-style column apply does not overlay the remembered map onto omitted widths.
        /// </summary>
        [Fact]
        public void ApplyVisibleColumnSpecs_does_not_fill_from_remembered_map()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.RememberColumnWidths = true;

            var renameListViewModel = _context.CreateRenameListViewModel();
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.ApplySessionSection(
                new RenameListPrefs
                {
                    VisibleColumns = [new RenameListVisibleColumnSpec(nameOriginal, Width: 90)],
                    ColumnWidths = [new RenameListVisibleColumnSpec(nameOriginal, Width: 188)],
                }
            );

            renameListViewModel.ApplyVisibleColumnSpecs([new RenameListVisibleColumnSpec(nameOriginal)]);

            Assert.Equal(RenameListVisibleColumn.UseCatalogDefaultWidth, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(188, renameListViewModel.RememberedColumnWidths[nameOriginal]);
        }
    }
}

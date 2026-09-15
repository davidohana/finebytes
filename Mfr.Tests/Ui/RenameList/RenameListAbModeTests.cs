using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests A/B Mode prefs, normalize, and <see cref="RenameListViewModel.ProjectedColumns"/>.
    /// </summary>
    public sealed class RenameListAbModeTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void WithPreviewCompanions_inserts_after_each_original_idempotently()
        {
            var itemType = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType);
            var fullName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var fullNamePreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var name = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);

            var expanded = RenameListVisibleColumn.WithPreviewCompanions([
                new RenameListVisibleColumn(itemType, Width: 80),
                new RenameListVisibleColumn(fullName, Width: 200),
                new RenameListVisibleColumn(name, Width: 120),
            ]);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(itemType, Width: 80),
                    new RenameListVisibleColumn(fullName, Width: 200),
                    new RenameListVisibleColumn(fullNamePreview),
                    new RenameListVisibleColumn(name, Width: 120),
                    new RenameListVisibleColumn(namePreview),
                ],
                expanded
            );
            Assert.Equal(expanded, RenameListVisibleColumn.WithPreviewCompanions(expanded));
        }

        [Fact]
        public void WithPreviewCompanions_skips_when_preview_already_present()
        {
            var fullName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var fullNamePreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );

            var columns = new[]
            {
                new RenameListVisibleColumn(fullName, Width: 200),
                new RenameListVisibleColumn(fullNamePreview, Width: 180),
            };

            Assert.Equal(columns, RenameListVisibleColumn.WithPreviewCompanions(columns));
        }

        [Fact]
        public void NormalizeToOriginals_preview_only_becomes_originals()
        {
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var folderPreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );

            var normalized = RenameListVisibleColumn.NormalizeToOriginals([
                new RenameListVisibleColumn(namePreview, Width: 140),
                new RenameListVisibleColumn(folderPreview, Width: 200),
            ]);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                        Width: 140
                    ),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                        Width: 200
                    ),
                ],
                normalized
            );
        }

        [Fact]
        public void NormalizeToOriginals_dedupes_preview_and_original_preserving_first_seen_order()
        {
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var folderOriginal = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );

            var normalized = RenameListVisibleColumn.NormalizeToOriginals([
                new RenameListVisibleColumn(namePreview, Width: 90),
                new RenameListVisibleColumn(folderOriginal, Width: 160),
                new RenameListVisibleColumn(nameOriginal, Width: 120),
            ]);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(nameOriginal, Width: 120),
                    new RenameListVisibleColumn(folderOriginal, Width: 160),
                ],
                normalized
            );
        }

        [Fact]
        public void NormalizeToOriginals_width_prefers_existing_original_over_preview()
        {
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);

            var normalized = RenameListVisibleColumn.NormalizeToOriginals([
                new RenameListVisibleColumn(nameOriginal, Width: 111),
                new RenameListVisibleColumn(namePreview, Width: 222),
            ]);

            Assert.Single(normalized);
            Assert.Equal(nameOriginal, normalized[0].Key);
            Assert.Equal(111, normalized[0].Width);
        }

        [Fact]
        public void AsOriginal_returns_self_for_original_and_maps_preview()
        {
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);

            Assert.Equal(nameOriginal, nameOriginal.AsOriginal());
            Assert.Equal(nameOriginal, namePreview.AsOriginal());
            Assert.False(namePreview.AsOriginal().IsPreview);
        }

        [Fact]
        public void ToOriginalKeysFirstSeen_maps_and_dedupes_preserving_order()
        {
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var folderPreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );
            var folderOriginal = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );

            var mapped = RenameListVisibleColumn.ToOriginalKeysFirstSeen([
                namePreview,
                folderPreview,
                nameOriginal,
                folderOriginal,
            ]);

            Assert.Equal([nameOriginal, folderOriginal], mapped);
        }

        [Fact]
        public void ProjectedColumns_off_matches_visible_including_preview_keys()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();

            Assert.False(renameListViewModel.IsAbModeEnabled);
            Assert.Equal(renameListViewModel.VisibleColumns, renameListViewModel.ProjectedColumns);
            Assert.Contains(renameListViewModel.ProjectedColumns, column => column.Key.IsPreview);
        }

        [Fact]
        public void Enabling_ab_mode_normalizes_defaults_to_originals_only()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.IsAbModeEnabled = true;

            Assert.All(renameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));
            Assert.Equal(3, renameListViewModel.VisibleColumns.Count);
            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                ],
                renameListViewModel.VisibleColumns.Select(column => column.Key)
            );
        }

        [Fact]
        public void ProjectedColumns_original_side_is_stored_originals()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSideOriginal;

            Assert.Equal(renameListViewModel.VisibleColumns, renameListViewModel.ProjectedColumns);
            Assert.DoesNotContain(renameListViewModel.ProjectedColumns, column => column.Key.IsPreview);
        }

        [Fact]
        public void ProjectedColumns_after_side_swaps_previewable_fields_keeping_count_and_width()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var itemTypeKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.ItemType
            );
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(itemTypeKey, Width: 120),
                new RenameListVisibleColumn(fullNameKey, Width: 220),
            ]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;

            var projected = renameListViewModel.ProjectedColumns;
            Assert.Equal(
                [
                    new RenameListVisibleColumn(itemTypeKey, Width: 120),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                        Width: 220
                    ),
                ],
                projected
            );
            Assert.Equal(2, renameListViewModel.VisibleColumns.Count);
            Assert.Equal(2, projected.Count);
            Assert.All(renameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));
        }

        [Fact]
        public void UpdateVisibleColumnWidth_maps_after_side_preview_key_to_stored_original()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey, Width: 200)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;

            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            renameListViewModel.UpdateVisibleColumnWidth(previewKey, 400);

            Assert.Equal(400, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(previewKey, renameListViewModel.ProjectedColumns[0].Key);
            Assert.Equal(400, renameListViewModel.ProjectedColumns[0].Width);
        }

        [Fact]
        public void ReorderVisibleColumns_maps_after_side_preview_keys_to_originals()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var fullNamePreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var folderPreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, Width: 100),
                new RenameListVisibleColumn(fullNameKey, Width: 200),
            ]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;

            renameListViewModel.ReorderVisibleColumns([fullNamePreview, folderPreview]);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(fullNameKey, Width: 200),
                    new RenameListVisibleColumn(folderKey, Width: 100),
                ],
                renameListViewModel.VisibleColumns
            );
        }

        [Fact]
        public void ApplySessionSection_ab_mode_normalizes_columns_and_invalid_side()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var fullNamePreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);

            renameListViewModel.ApplySessionSection(
                new RenameListPrefs
                {
                    AbModeEnabled = true,
                    AbSide = "bogus",
                    VisibleColumns =
                    [
                        new RenameListVisibleColumnSpec(fullNamePreview, Width: 175),
                        new RenameListVisibleColumnSpec(nameOriginal, Width: 130),
                    ],
                }
            );

            Assert.True(renameListViewModel.IsAbModeEnabled);
            Assert.Equal(RenameListPrefs.AbSidePreview, renameListViewModel.AbSide);
            Assert.Equal(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                        Width: 175
                    ),
                    new RenameListVisibleColumn(nameOriginal, Width: 130),
                ],
                renameListViewModel.VisibleColumns
            );

            var captured = renameListViewModel.CaptureSession();
            Assert.True(captured.AbModeEnabled);
            Assert.Equal(RenameListPrefs.AbSidePreview, captured.AbSide);
            Assert.NotNull(captured.VisibleColumns);
            Assert.All(captured.VisibleColumns, column => Assert.False(column.Key.IsPreview));
        }

        [Fact]
        public void ApplyVisibleColumns_null_under_ab_mode_uses_normalized_defaults()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
                ),
            ]);

            renameListViewModel.ApplyVisibleColumns(null);

            Assert.Equal(3, renameListViewModel.VisibleColumns.Count);
            Assert.All(renameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));
            Assert.Equal(
                RenameListVisibleColumn.NormalizeToOriginals(RenameListVisibleColumn.CreateDefaults()),
                renameListViewModel.VisibleColumns
            );
        }

        [Fact]
        public async Task ReplaceWithRelevantColumns_under_ab_mode_stays_originals_only()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");
            renameListViewModel.IsAbModeEnabled = true;

            await renameListViewModel.ReplaceWithRelevantColumnsCommand.ExecuteAsync(null);

            Assert.All(renameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));
            Assert.Contains(
                renameListViewModel.VisibleColumns,
                column =>
                    column.Key
                    == RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
            );
            Assert.DoesNotContain(
                renameListViewModel.VisibleColumns,
                column =>
                    column.Key == RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
            );
        }

        [Fact]
        public async Task AddRelevantColumns_under_ab_mode_adds_original_only_and_skips_preview_companion()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(folderKey, Width: 180)]);
            renameListViewModel.IsAbModeEnabled = true;

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            Assert.Equal(
                [new RenameListVisibleColumn(folderKey, Width: 180), new RenameListVisibleColumn(nameOriginal)],
                renameListViewModel.VisibleColumns
            );
        }

        [Fact]
        public async Task AddRelevantColumns_under_ab_mode_is_noop_when_original_already_visible()
        {
            var appliedFilters = new AppliedFiltersViewModel();
            var renameListViewModel = _context.CreateRenameListViewModel(appliedFilters: appliedFilters);
            appliedFilters.AddAndSelect(new RemoveSpacesFilter(new FilePrefixTarget()), "Remove Spaces");

            var nameOriginal = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(nameOriginal, Width: 120)]);
            renameListViewModel.IsAbModeEnabled = true;

            await renameListViewModel.AddRelevantColumnsCommand.ExecuteAsync(null);

            Assert.Equal([new RenameListVisibleColumn(nameOriginal, Width: 120)], renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void ToggleAbMode_enables_and_normalizes_preview_keys()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var fullNamePreview = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNamePreview)]);
            Assert.False(renameListViewModel.IsAbModeEnabled);

            renameListViewModel.ToggleAbMode();

            Assert.True(renameListViewModel.IsAbModeEnabled);
            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.False(renameListViewModel.VisibleColumns[0].Key.IsPreview);
            Assert.Equal(BasicRenameListFields.Key.FullName, renameListViewModel.VisibleColumns[0].Key.PropertyKey);

            renameListViewModel.ToggleAbMode();
            Assert.False(renameListViewModel.IsAbModeEnabled);
            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    fullNamePreview,
                ],
                renameListViewModel.VisibleColumns.Select(column => column.Key)
            );
        }

        [Fact]
        public void Disabling_ab_mode_inserts_preview_companions_after_originals()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var itemType = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType);
            var folder = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var fullName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(itemType),
                new RenameListVisibleColumn(folder),
                new RenameListVisibleColumn(fullName),
            ]);
            renameListViewModel.IsAbModeEnabled = true;

            renameListViewModel.IsAbModeEnabled = false;

            Assert.Equal(
                [
                    itemType,
                    folder,
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    fullName,
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                ],
                renameListViewModel.VisibleColumns.Select(column => column.Key)
            );
        }

        [AvaloniaFact]
        public void IsAbSide_bindables_follow_AbSide()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            Assert.True(renameListViewModel.IsAbSidePreview);
            Assert.False(renameListViewModel.IsAbSideOriginal);

            renameListViewModel.AbSide = RenameListPrefs.AbSideOriginal;

            Assert.True(renameListViewModel.IsAbSideOriginal);
            Assert.False(renameListViewModel.IsAbSidePreview);
        }

        [Fact]
        public void SetAbSide_updates_side_when_ab_mode_off()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            Assert.False(renameListViewModel.IsAbModeEnabled);

            renameListViewModel.SetAbSide(RenameListPrefs.AbSideOriginal);

            Assert.Equal(RenameListPrefs.AbSideOriginal, renameListViewModel.AbSide);
            Assert.True(renameListViewModel.IsAbSideOriginal);
        }

        [Fact]
        public async Task SetAbSide_to_preview_hydrates_when_projected_requirement_grows()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "SideFlipTitle", album: null);

            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey)]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSideOriginal;
            // Bypass hydrate (unlike shuttle apply) so Preview flip must load TagLib.
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(titleKey),
                new RenameListVisibleColumn(fullNameKey),
            ]);

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.False(entry.EngineItem.TagLibLoadAttempted);

            renameListViewModel.SetAbSide(RenameListPrefs.AbSidePreview);
            await _WaitUntilAsync(() =>
                    !renameListViewModel.IsBusy
                    && renameListViewModel.AbSide == RenameListPrefs.AbSidePreview
                    && entry.EngineItem.TagLibLoadAttempted
                )
                .ConfigureAwait(true);

            Assert.Equal(RenameListPrefs.AbSidePreview, renameListViewModel.AbSide);
            Assert.True(entry.EngineItem.TagLibLoadAttempted);
            Assert.Equal("SideFlipTitle", entry.GetFieldText(titleKey));
        }

        [Fact]
        public async Task ExportVisibleColumnsAsync_uses_projected_columns_on_preview_side()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "row.txt");
            await File.WriteAllTextAsync(path, "x");
            var outPath = Path.Combine(dir, "projected.csv");

            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;
            renameListViewModel.UiHooks = new RenameListUiHooks
            {
                PickSavePathAsync = (_, _, _) => Task.FromResult<string?>(outPath),
            };

            await renameListViewModel.ExportVisibleColumnsAsync();

            Assert.Equal(
                $"Full File Name (Preview){Environment.NewLine}row.txt{Environment.NewLine}",
                await File.ReadAllTextAsync(outPath)
            );
            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.Single(renameListViewModel.ProjectedColumns);
        }

        [Fact]
        public async Task Overrides_persist_across_ab_side_flips()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "row.txt");
            await File.WriteAllTextAsync(path, "x");

            var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(nameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSideOriginal;

            var entry = Assert.Single(renameListViewModel.Entries);
            entry.EngineItem.SetOverride(nameKey, "original-forced");
            entry.EngineItem.SetOverride(namePreview, "preview-forced");

            Assert.True(entry.IsOverridden(nameKey));
            Assert.True(entry.IsOverridden(namePreview));
            Assert.Equal("original-forced", entry.GetFieldText(nameKey));

            renameListViewModel.SetAbSide(RenameListPrefs.AbSidePreview);
            Assert.Equal("original-forced", entry.GetFieldText(nameKey));
            Assert.Equal("preview-forced", entry.GetFieldText(namePreview));
            Assert.True(entry.IsOverridden(nameKey));
            Assert.True(entry.IsOverridden(namePreview));

            renameListViewModel.SetAbSide(RenameListPrefs.AbSideOriginal);
            Assert.Equal("original-forced", entry.GetFieldText(nameKey));
            Assert.True(entry.IsOverridden(namePreview));
        }

        private static async Task _WaitUntilAsync(Func<bool> condition, int timeoutMs = 10_000)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException("Condition was not met in time.");
                }

                await Task.Delay(20).ConfigureAwait(true);
            }
        }
    }
}

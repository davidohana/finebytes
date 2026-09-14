using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Space;
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
        public void ProjectedColumns_preview_side_derives_companions_with_catalog_default_width()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, Width: 180),
                new RenameListVisibleColumn(fullNameKey, Width: 220),
            ]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;

            var projected = renameListViewModel.ProjectedColumns;
            Assert.Equal(
                [
                    new RenameListVisibleColumn(folderKey, Width: 180),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)
                    ),
                    new RenameListVisibleColumn(fullNameKey, Width: 220),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                    ),
                ],
                projected
            );
            Assert.Equal(2, renameListViewModel.VisibleColumns.Count);
            Assert.All(renameListViewModel.VisibleColumns, column => Assert.False(column.Key.IsPreview));
        }

        [Fact]
        public void UpdateVisibleColumnWidth_no_ops_for_derived_preview_key()
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

            Assert.Equal(200, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(RenameListVisibleColumn.UseCatalogDefaultWidth, renameListViewModel.ProjectedColumns[1].Width);
        }

        [Fact]
        public void ReorderVisibleColumns_maps_preview_side_key_superset_to_originals()
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
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, Width: 100),
                new RenameListVisibleColumn(fullNameKey, Width: 200),
            ]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;

            renameListViewModel.ReorderVisibleColumns([fullNameKey, fullNamePreview, folderKey]);

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
    }
}

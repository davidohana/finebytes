using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests Rename List visible-column state on <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed class RenameListVisibleColumnTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public void VisibleColumns_default_matches_mfr7_rename_grid()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
            Assert.Equal(4, renameListViewModel.VisibleColumns.Count);
            Assert.Equal(BasicItemTypeField.Key, renameListViewModel.VisibleColumns[0].Key.PropertyKey);
            Assert.False(renameListViewModel.VisibleColumns[0].Key.IsPreview);
            Assert.Equal(BasicFolderField.Key, renameListViewModel.VisibleColumns[1].Key.PropertyKey);
            Assert.Equal(BasicFullNameField.Key, renameListViewModel.VisibleColumns[2].Key.PropertyKey);
            Assert.False(renameListViewModel.VisibleColumns[2].Key.IsPreview);
            Assert.Equal(BasicFullNameField.Key, renameListViewModel.VisibleColumns[3].Key.PropertyKey);
            Assert.True(renameListViewModel.VisibleColumns[3].Key.IsPreview);
        }

        [Fact]
        public void HideColumn_removes_matching_entry()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);

            renameListViewModel.HideColumn(previewKey);

            Assert.Equal(3, renameListViewModel.VisibleColumns.Count);
            Assert.DoesNotContain(renameListViewModel.VisibleColumns, column => column.Key == previewKey);
        }

        [Fact]
        public void HideColumn_does_not_remove_last_column()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var onlyKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicItemTypeField.Key);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(onlyKey)]);

            renameListViewModel.HideColumn(onlyKey);

            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.Equal(onlyKey, renameListViewModel.VisibleColumns[0].Key);
        }

        [Fact]
        public void HideColumn_unknown_key_is_noop()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var before = renameListViewModel.VisibleColumns.ToList();

            renameListViewModel.HideColumn(RenameListFieldKey.Original("Unknown", "Missing"));

            Assert.Equal(before, renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void ApplyVisibleColumns_null_restores_defaults()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
            ]);

            renameListViewModel.ApplyVisibleColumns(null);

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void ApplyVisibleColumns_skips_unknown_and_duplicate_keys()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var validKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key);
            var unknownKey = RenameListFieldKey.Original("Unknown", "Missing");

            renameListViewModel.ApplyVisibleColumns([
                new RenameListVisibleColumn(validKey, Width: 140),
                new RenameListVisibleColumn(unknownKey),
                new RenameListVisibleColumn(validKey, Width: 90),
            ]);

            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.Equal(validKey, renameListViewModel.VisibleColumns[0].Key);
            Assert.Equal(140, renameListViewModel.VisibleColumns[0].Width);
        }

        [Fact]
        public void ApplyVisibleColumns_all_unknown_restores_defaults()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
            ]);

            renameListViewModel.ApplyVisibleColumns([
                new RenameListVisibleColumn(RenameListFieldKey.Original("Unknown", "Missing")),
            ]);

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void SetVisibleColumns_rejects_empty_unknown_and_duplicate_lists()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var knownKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key);
            var unknownKey = RenameListFieldKey.Original("Unknown", "Missing");

            Assert.Throws<ArgumentException>(() => renameListViewModel.SetVisibleColumns([]));
            Assert.Throws<ArgumentException>(() =>
                renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(unknownKey)])
            );
            Assert.Throws<ArgumentException>(() =>
                renameListViewModel.SetVisibleColumns([
                    new RenameListVisibleColumn(knownKey),
                    new RenameListVisibleColumn(knownKey),
                ])
            );
        }

        [Fact]
        public void ReorderVisibleColumns_reorders_keys_and_preserves_widths()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var firstKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key);
            var secondKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(firstKey, Width: 150),
                new RenameListVisibleColumn(secondKey, Width: 120),
            ]);

            renameListViewModel.ReorderVisibleColumns([secondKey, firstKey]);

            Assert.Equal(secondKey, renameListViewModel.VisibleColumns[0].Key);
            Assert.Equal(120, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(firstKey, renameListViewModel.VisibleColumns[1].Key);
            Assert.Equal(150, renameListViewModel.VisibleColumns[1].Width);
        }

        [Fact]
        public void ReorderVisibleColumns_no_ops_when_order_unchanged()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var before = renameListViewModel.VisibleColumns.ToList();
            var propertyChanged = false;
            renameListViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(RenameListViewModel.VisibleColumns))
                {
                    propertyChanged = true;
                }
            };

            renameListViewModel.ReorderVisibleColumns([.. before.Select(column => column.Key)]);

            Assert.False(propertyChanged);
            Assert.Equal(before, renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void ReorderVisibleColumns_rejects_unknown_mismatched_or_duplicate_keys()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var firstKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key);
            var secondKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key);
            var unknownKey = RenameListFieldKey.Original("Unknown", "Missing");
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(firstKey),
                new RenameListVisibleColumn(secondKey),
            ]);

            Assert.Throws<ArgumentException>(() => renameListViewModel.ReorderVisibleColumns([]));
            Assert.Throws<ArgumentException>(() => renameListViewModel.ReorderVisibleColumns([firstKey, firstKey]));
            Assert.Throws<ArgumentException>(() => renameListViewModel.ReorderVisibleColumns([unknownKey, secondKey]));
        }

        [Fact]
        public void ApplyVisibleColumnsFromSession_null_restores_defaults()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
            ]);

            renameListViewModel.ApplyVisibleColumnsFromSession(null);

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void CaptureVisibleColumnsForSession_omits_catalog_default_widths()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key),
                    Width: 120
                ),
            ]);

            var sessionColumns = renameListViewModel.CaptureVisibleColumnsForSession();

            Assert.Equal(2, sessionColumns.Count);
            Assert.Null(sessionColumns[0].Width);
            Assert.Equal(120, sessionColumns[1].Width);
        }

        [Fact]
        public void Visible_columns_session_round_trips_layout_and_widths()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var sessionColumns = new[]
            {
                new SessionStateRenameListColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key),
                    Width: 150
                ),
                new SessionStateRenameListColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key)
                ),
            };
            renameListViewModel.ApplyVisibleColumnsFromSession(sessionColumns);

            Assert.Equal(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key),
                        Width: 150
                    ),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key)
                    ),
                ],
                renameListViewModel.VisibleColumns
            );
            Assert.Equal(sessionColumns, renameListViewModel.CaptureVisibleColumnsForSession());
        }

        [Fact]
        public void UpdateVisibleColumnWidth_updates_without_raising_visible_columns()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicFolderField.Key);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(key, Width: 200)]);
            var raisedVisibleColumns = false;
            renameListViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(RenameListViewModel.VisibleColumns))
                {
                    raisedVisibleColumns = true;
                }
            };

            renameListViewModel.UpdateVisibleColumnWidth(key, 320);

            Assert.Equal(320, renameListViewModel.VisibleColumns[0].Width);
            Assert.False(raisedVisibleColumns);

            raisedVisibleColumns = false;
            renameListViewModel.UpdateVisibleColumnWidth(key, 320);
            renameListViewModel.UpdateVisibleColumnWidth(
                RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key),
                100
            );
            Assert.Equal(320, renameListViewModel.VisibleColumns[0].Width);
            Assert.False(raisedVisibleColumns);
        }
    }
}

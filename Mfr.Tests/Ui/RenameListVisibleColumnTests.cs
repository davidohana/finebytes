using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests Rename List visible-column state on <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed class RenameListVisibleColumnTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var fileListViewModel in _fileListViewModels)
            {
                fileListViewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        public void VisibleColumns_default_matches_mfr7_rename_grid()
        {
            var renameListViewModel = _CreateRenameListViewModel();

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
            var renameListViewModel = _CreateRenameListViewModel();
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);

            renameListViewModel.HideColumn(previewKey);

            Assert.Equal(3, renameListViewModel.VisibleColumns.Count);
            Assert.DoesNotContain(renameListViewModel.VisibleColumns, column => column.Key == previewKey);
        }

        [Fact]
        public void HideColumn_does_not_remove_last_column()
        {
            var renameListViewModel = _CreateRenameListViewModel();
            var onlyKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicItemTypeField.Key);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(onlyKey)]);

            renameListViewModel.HideColumn(onlyKey);

            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.Equal(onlyKey, renameListViewModel.VisibleColumns[0].Key);
        }

        [Fact]
        public void ApplyVisibleColumns_null_restores_defaults()
        {
            var renameListViewModel = _CreateRenameListViewModel();
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
            ]);

            renameListViewModel.ApplyVisibleColumns(null);

            Assert.Equal(RenameListVisibleColumn.CreateDefaults(), renameListViewModel.VisibleColumns);
        }

        [Fact]
        public void CaptureVisibleColumns_round_trips_current_layout()
        {
            var renameListViewModel = _CreateRenameListViewModel();
            var customColumns = new[]
            {
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key),
                    Width: 150
                ),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicExtensionField.Key),
                    Width: 100
                ),
            };
            renameListViewModel.SetVisibleColumns(customColumns);

            Assert.Equal(customColumns, renameListViewModel.CaptureVisibleColumns());
        }

        [Fact]
        public void ApplyVisibleColumns_skips_unknown_keys_and_keeps_valid_ones()
        {
            var renameListViewModel = _CreateRenameListViewModel();
            var validKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key);
            var unknownKey = RenameListFieldKey.Original("Unknown", "Missing");

            renameListViewModel.ApplyVisibleColumns([
                new RenameListVisibleColumn(validKey),
                new RenameListVisibleColumn(unknownKey),
            ]);

            Assert.Single(renameListViewModel.VisibleColumns);
            Assert.Equal(validKey, renameListViewModel.VisibleColumns[0].Key);
        }

        [Fact]
        public void SetVisibleColumns_rejects_empty_list()
        {
            var renameListViewModel = _CreateRenameListViewModel();

            Assert.Throws<ArgumentException>(() => renameListViewModel.SetVisibleColumns([]));
        }

        [Fact]
        public void ApplyVisibleColumnsFromSession_null_restores_defaults()
        {
            var renameListViewModel = _CreateRenameListViewModel();
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
            var renameListViewModel = _CreateRenameListViewModel();
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
            var renameListViewModel = _CreateRenameListViewModel();
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

        private RenameListViewModel _CreateRenameListViewModel()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                dir,
                NullFileShellOpener.Instance
            );
            _fileListViewModels.Add(fileListViewModel);
            return new RenameListViewModel(fileListViewModel);
        }
    }
}

using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="SessionStore"/>.
    /// </summary>
    public sealed class SessionStoreTests
    {
        [Fact]
        public void Load_missing_file_returns_empty_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-missing-" + Guid.NewGuid() + ".json");
            var session = SessionStore.Load(path);
            Assert.Equal(1, session.Version);
            Assert.Equal(RenameListAddMode.Files, session.Ui.AddMode);
            Assert.Null(session.MainWindow);
            Assert.Null(session.FileList);
            Assert.Null(session.RenameList);
        }

        [Fact]
        public void Load_corrupt_json_returns_empty_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-bad-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, "{ not-json");
                var session = SessionStore.Load(path);
                Assert.Equal(RenameListAddMode.Files, session.Ui.AddMode);
                Assert.Null(session.MainWindow);
                Assert.Null(session.FileList);
                Assert.Null(session.RenameList);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Save_and_Load_round_trip()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-round-" + Guid.NewGuid() + ".json");
            try
            {
                var original = new SessionState
                {
                    Version = 1,
                    MainWindow = new SessionStateMainWindow
                    {
                        X = 12,
                        Y = 34,
                        Width = 1100,
                        Height = 720,
                        State = "Maximized",
                        Splitters = new SessionStateSplitters
                        {
                            FileList = 0.35,
                            AvailableApplied = 0.45,
                            FilterLists = 0.55,
                            TopPanes = 0.65,
                        },
                    },
                    FileList = new SessionStateFileList
                    {
                        LastOpenedDirectory = Path.Combine(Path.GetTempPath(), "music"),
                        FileMask = "*.mp3",
                        ExcludeMasks = ["*.wav", "*.ogg"],
                        ExcludeMasksEnabled = true,
                        MaskSuggestions = ["*.mp3", "*.flac"],
                    },
                    RenameList = new SessionStateRenameList
                    {
                        SortFields =
                        [
                            new SessionStateRenameListSortField(
                                RenameListTestHelpers.FullFileNameKey,
                                Descending: true
                            ),
                        ],
                        VisibleColumns =
                        [
                            new SessionStateRenameListColumn(
                                RenameListFieldKey.Original(
                                    BasicRenameListField.Group,
                                    BasicRenameListFields.Key.FullPath
                                ),
                                Width: 220
                            ),
                            new SessionStateRenameListColumn(
                                RenameListFieldKey.Preview(
                                    BasicRenameListField.Group,
                                    BasicRenameListFields.Key.FullName
                                )
                            ),
                        ],
                    },
                };

                SessionStore.Save(original, path);
                var loaded = SessionStore.Load(path);

                Assert.Equal(1, loaded.Version);
                Assert.NotNull(loaded.MainWindow);
                Assert.Equal(12, loaded.MainWindow.X);
                Assert.Equal(34, loaded.MainWindow.Y);
                Assert.Equal(1100, loaded.MainWindow.Width);
                Assert.Equal(720, loaded.MainWindow.Height);
                Assert.Equal("Maximized", loaded.MainWindow.State);
                Assert.NotNull(loaded.MainWindow.Splitters);
                Assert.Equal(0.35, loaded.MainWindow.Splitters.FileList);
                Assert.Equal(0.45, loaded.MainWindow.Splitters.AvailableApplied);
                Assert.Equal(0.55, loaded.MainWindow.Splitters.FilterLists);
                Assert.Equal(0.65, loaded.MainWindow.Splitters.TopPanes);
                Assert.NotNull(loaded.FileList);
                Assert.Equal(original.FileList.LastOpenedDirectory, loaded.FileList.LastOpenedDirectory);
                Assert.Equal("*.mp3", loaded.FileList.FileMask);
                Assert.Equal(["*.wav", "*.ogg"], loaded.FileList.ExcludeMasks);
                Assert.True(loaded.FileList.ExcludeMasksEnabled);
                Assert.Equal(2, loaded.FileList.MaskSuggestions?.Count);
                Assert.Contains("*.mp3", loaded.FileList.MaskSuggestions!);
                Assert.Contains("*.flac", loaded.FileList.MaskSuggestions!);
                Assert.NotNull(loaded.RenameList);
                Assert.NotNull(loaded.RenameList.SortFields);
                Assert.Single(loaded.RenameList.SortFields);
                Assert.Equal(RenameListTestHelpers.FullFileNameKey, loaded.RenameList.SortFields[0].Key);
                Assert.True(loaded.RenameList.SortFields[0].Descending);
                Assert.NotNull(loaded.RenameList.VisibleColumns);
                Assert.Equal(2, loaded.RenameList.VisibleColumns.Count);
                Assert.Equal(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                    loaded.RenameList.VisibleColumns[0].Key
                );
                Assert.Equal(220, loaded.RenameList.VisibleColumns[0].Width);
                Assert.Equal(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    loaded.RenameList.VisibleColumns[1].Key
                );
                Assert.Null(loaded.RenameList.VisibleColumns[1].Width);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}

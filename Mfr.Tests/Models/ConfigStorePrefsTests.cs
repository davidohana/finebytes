using System.Text.Json;
using Mfr.Filters.Case;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore"/> session / filterDefaults sections and soft-load dialect.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfigStorePrefsTests
    {
        [Fact]
        public void Load_missing_default_style_file_returns_empty_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-missing-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, """{}""");
            try
            {
                ConfigStore.Load(path);
                Assert.Equal(1, ConfigStore.Session.Version);
                Assert.Null(ConfigStore.Session.MainWindow);
                Assert.Null(ConfigStore.Session.FileList);
                Assert.Null(ConfigStore.Session.RenameList);
                Assert.Null(ConfigStore.Session.FilterEditor);
                Assert.Empty(ConfigStore.FilterDefaultsJson);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_corrupt_json_returns_defaults()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-bad-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, "{ not-json");
                ConfigStore.Load(path);
                Assert.Null(ConfigStore.Session.MainWindow);
                Assert.Equal(ConfirmationPrompts.Normal, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.Empty(ConfigStore.FilterDefaultsJson);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_non_positive_session_version_normalizes_to_one()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-ver-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(
                    path, /*lang=json,strict*/
                    """{"session":{"version":0}}"""
                );
                ConfigStore.Load(path);
                Assert.Equal(1, ConfigStore.Session.Version);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Save_and_Load_round_trip_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-round-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Session = new SessionState
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
                        ViewMode = FileListViewMode.List,
                        ThumbnailSize = 128,
                    },
                    RenameList = new SessionStateRenameList
                    {
                        SortFields = [new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true)],
                        VisibleColumns =
                        [
                            new RenameListVisibleColumnSpec(
                                RenameListFieldKey.Original(
                                    BasicRenameListField.Group,
                                    BasicRenameListFields.Key.FullPath
                                ),
                                Width: 220
                            ),
                            new RenameListVisibleColumnSpec(
                                RenameListFieldKey.Preview(
                                    BasicRenameListField.Group,
                                    BasicRenameListFields.Key.FullName
                                )
                            ),
                        ],
                    },
                    FilterEditor = new SessionStateFilterEditor { FormatTokenPickerExpanded = false },
                };

                ConfigStore.Save(path);
                ConfigStore.Load(path);

                var loaded = ConfigStore.Session;
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
                Assert.Equal(Path.Combine(Path.GetTempPath(), "music"), loaded.FileList.LastOpenedDirectory);
                Assert.Equal("*.mp3", loaded.FileList.FileMask);
                Assert.Equal(["*.wav", "*.ogg"], loaded.FileList.ExcludeMasks);
                Assert.True(loaded.FileList.ExcludeMasksEnabled);
                Assert.Equal(2, loaded.FileList.MaskSuggestions?.Count);
                Assert.Contains("*.mp3", loaded.FileList.MaskSuggestions!);
                Assert.Contains("*.flac", loaded.FileList.MaskSuggestions!);
                Assert.Equal(FileListViewMode.List, loaded.FileList.ViewMode);
                Assert.Equal(128, loaded.FileList.ThumbnailSize);
                Assert.NotNull(loaded.RenameList);
                Assert.NotNull(loaded.RenameList.SortFields);
                Assert.Single(loaded.RenameList.SortFields);
                Assert.Equal(RenameListTestHelpers.FullFileNameKey, loaded.RenameList.SortFields[0].FieldKey);
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
                Assert.NotNull(loaded.FilterEditor);
                Assert.False(loaded.FilterEditor.FormatTokenPickerExpanded);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Save_and_Load_round_trips_session_and_filter_default_in_one_file()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-unified-round-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.Session = new SessionState { FileList = new SessionStateFileList { FileMask = "*.flac" } };
                ConfigStore.Save(path);

                var store = FilterDefaultsStore.CreateEmpty();
                store.SetDefault(
                    new LettersCaseFilter(
                        new FileExtensionTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, [])
                    )
                );

                using (var doc = JsonDocument.Parse(File.ReadAllText(path)))
                {
                    Assert.Equal(
                        "more",
                        doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString()
                    );
                    Assert.Equal(
                        "*.flac",
                        doc.RootElement.GetProperty("session")
                            .GetProperty("fileList")
                            .GetProperty("fileMask")
                            .GetString()
                    );
                    Assert.True(doc.RootElement.GetProperty("filterDefaults").TryGetProperty("LettersCase", out _));
                    Assert.False(doc.RootElement.GetProperty("filterDefaults").TryGetProperty("defaults", out _));
                }

                ConfigStore.Load(path);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.Equal("*.flac", ConfigStore.Session.FileList?.FileMask);
                var reloaded = FilterDefaultsStore.OpenDefault();
                Assert.True(reloaded.TryGetDefault("LettersCase", out var filter));
                Assert.Equal(LettersCaseMode.UpperCase, Assert.IsType<LettersCaseFilter>(filter).Options.Mode);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void SetDefault_pin_save_rewrites_whole_config()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-pin-save-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;
                ConfigStore.Session = new SessionState { MainWindow = new SessionStateMainWindow { Width = 900 } };
                ConfigStore.Save(path);

                FilterDefaultsStore.CreateEmpty().SetDefault(new LettersCaseFilter());

                ConfigStore.Load(path);
                Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
                Assert.Equal(900, ConfigStore.Session.MainWindow?.Width);
                Assert.True(ConfigStore.FilterDefaultsJson.ContainsKey("LettersCase"));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_skips_invalid_ui_leaf_keeps_valid_leaves()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-soft-leaf-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path, /*lang=json,strict*/
                """
                {
                  "ui": {
                    "confirmationPrompts": "more",
                    "doubleClickAddsToRenameList": "not-a-bool"
                  },
                  "log": {
                    "maxSessionFiles": "50"
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(path);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.False(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
                Assert.Equal(50, ConfigStore.Config.Log.MaxSessionFiles);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        /// <summary>
        /// Verifies a missing prefs file delete is a no-op.
        /// </summary>
        [Fact]
        public void Delete_missing_is_noop()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-delete-missing-" + Guid.NewGuid() + ".json");
            ConfigStore.DeleteDefaultFile(path);
            Assert.False(File.Exists(path));
        }

        /// <summary>
        /// Verifies an existing prefs file is removed.
        /// </summary>
        [Fact]
        public void Delete_removes_existing_file()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-delete-" + Guid.NewGuid() + ".json");
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Save(path);
            try
            {
                Assert.True(File.Exists(path));
                ConfigStore.DeleteDefaultFile(path);
                Assert.False(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}

using System.Text.Json;
using Mfr.Filters.Case;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore"/> session sections / filterDefaults and soft-load dialect.
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
                Assert.Null(ConfigStore.MainWindow);
                Assert.Null(ConfigStore.FileList);
                Assert.Null(ConfigStore.RenameList);
                Assert.Null(ConfigStore.FilterEditor);
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
                Assert.Null(ConfigStore.MainWindow);
                Assert.Equal(ConfirmationPrompts.Normal, ConfigStore.Ui.ConfirmationPrompts);
                Assert.Empty(ConfigStore.FilterDefaultsJson);
            }
            finally
            {
                File.Delete(path);
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
                ConfigStore.MainWindow = new MainWindowPrefs
                {
                    X = 12,
                    Y = 34,
                    Width = 1100,
                    Height = 720,
                    State = "Maximized",
                    Splitters = new MainWindowSplitters
                    {
                        FileList = 0.35,
                        AvailableApplied = 0.45,
                        FilterLists = 0.55,
                        TopPanes = 0.65,
                    },
                };
                ConfigStore.FileList = new FileListPrefs
                {
                    LastOpenedDirectory = Path.Combine(Path.GetTempPath(), "music"),
                    FileMask = "*.mp3",
                    ExcludeMasks = ["*.wav", "*.ogg"],
                    ExcludeMasksEnabled = true,
                    MaskSuggestions = ["*.mp3", "*.flac"],
                    ViewMode = FileListViewMode.List,
                    ThumbnailSize = 128,
                    DoubleClickAddsToRenameList = true,
                };
                ConfigStore.RenameList = new RenameListPrefs
                {
                    SortFields = [new RenameListSortKey(RenameListTestHelpers.FullFileNameKey, Descending: true)],
                    VisibleColumns =
                    [
                        new RenameListVisibleColumnSpec(
                            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                            Width: 220
                        ),
                        new RenameListVisibleColumnSpec(
                            RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                        ),
                    ],
                };
                ConfigStore.FilterEditor = new FilterEditorPrefs { FormatTokenPickerExpanded = false };

                ConfigStore.Save(path);
                ConfigStore.Load(path);

                Assert.NotNull(ConfigStore.MainWindow);
                Assert.Equal(12, ConfigStore.MainWindow.X);
                Assert.Equal(34, ConfigStore.MainWindow.Y);
                Assert.Equal(1100, ConfigStore.MainWindow.Width);
                Assert.Equal(720, ConfigStore.MainWindow.Height);
                Assert.Equal("Maximized", ConfigStore.MainWindow.State);
                Assert.NotNull(ConfigStore.MainWindow.Splitters);
                Assert.Equal(0.35, ConfigStore.MainWindow.Splitters.FileList);
                Assert.Equal(0.45, ConfigStore.MainWindow.Splitters.AvailableApplied);
                Assert.Equal(0.55, ConfigStore.MainWindow.Splitters.FilterLists);
                Assert.Equal(0.65, ConfigStore.MainWindow.Splitters.TopPanes);
                Assert.NotNull(ConfigStore.FileList);
                Assert.Equal(Path.Combine(Path.GetTempPath(), "music"), ConfigStore.FileList.LastOpenedDirectory);
                Assert.Equal("*.mp3", ConfigStore.FileList.FileMask);
                Assert.Equal(["*.wav", "*.ogg"], ConfigStore.FileList.ExcludeMasks);
                Assert.True(ConfigStore.FileList.ExcludeMasksEnabled);
                Assert.Equal(2, ConfigStore.FileList.MaskSuggestions?.Count);
                Assert.Contains("*.mp3", ConfigStore.FileList.MaskSuggestions!);
                Assert.Contains("*.flac", ConfigStore.FileList.MaskSuggestions!);
                Assert.Equal(FileListViewMode.List, ConfigStore.FileList.ViewMode);
                Assert.Equal(128, ConfigStore.FileList.ThumbnailSize);
                Assert.True(ConfigStore.FileList.DoubleClickAddsToRenameList);
                Assert.NotNull(ConfigStore.RenameList);
                Assert.NotNull(ConfigStore.RenameList.SortFields);
                Assert.Single(ConfigStore.RenameList.SortFields);
                Assert.Equal(RenameListTestHelpers.FullFileNameKey, ConfigStore.RenameList.SortFields[0].FieldKey);
                Assert.True(ConfigStore.RenameList.SortFields[0].Descending);
                Assert.NotNull(ConfigStore.RenameList.VisibleColumns);
                Assert.Equal(2, ConfigStore.RenameList.VisibleColumns.Count);
                Assert.Equal(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
                    ConfigStore.RenameList.VisibleColumns[0].Key
                );
                Assert.Equal(220, ConfigStore.RenameList.VisibleColumns[0].Width);
                Assert.Equal(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    ConfigStore.RenameList.VisibleColumns[1].Key
                );
                Assert.Null(ConfigStore.RenameList.VisibleColumns[1].Width);
                Assert.NotNull(ConfigStore.FilterEditor);
                Assert.False(ConfigStore.FilterEditor.FormatTokenPickerExpanded);

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                Assert.False(doc.RootElement.TryGetProperty("session", out _));
                Assert.True(doc.RootElement.TryGetProperty("mainWindow", out _));
                Assert.True(doc.RootElement.TryGetProperty("fileList", out _));
                Assert.True(doc.RootElement.TryGetProperty("renameList", out _));
                Assert.True(doc.RootElement.TryGetProperty("filterEditor", out _));
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
                ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.FileList = new FileListPrefs { FileMask = "*.flac" };
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
                    Assert.Equal("*.flac", doc.RootElement.GetProperty("fileList").GetProperty("fileMask").GetString());
                    Assert.True(doc.RootElement.GetProperty("filterDefaults").TryGetProperty("LettersCase", out _));
                    Assert.False(doc.RootElement.GetProperty("filterDefaults").TryGetProperty("defaults", out _));
                }

                ConfigStore.Load(path);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
                Assert.Equal("*.flac", ConfigStore.FileList?.FileMask);
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
                ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
                ConfigStore.MainWindow = new MainWindowPrefs { Width = 900 };
                ConfigStore.Save(path);

                FilterDefaultsStore.CreateEmpty().SetDefault(new LettersCaseFilter());

                ConfigStore.Load(path);
                Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
                Assert.Equal(900, ConfigStore.MainWindow?.Width);
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
                    "doubleClickAddsToRenameList": "true"
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
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
                Assert.Null(ConfigStore.FileList);
                Assert.Equal(50, ConfigStore.Log.MaxSessionFiles);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_ignores_nested_session_object()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-legacy-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path, /*lang=json,strict*/
                """
                {
                  "session": {
                    "fileList": {
                      "fileMask": "*.legacy"
                    }
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(path);
                Assert.Null(ConfigStore.FileList);
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

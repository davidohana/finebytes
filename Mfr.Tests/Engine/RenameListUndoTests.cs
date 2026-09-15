using Mfr.App.Ui.Services.RenameLog;
using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Metadata;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Engine PrepareUndo: rebuild list from last op, sticky OldValues, Commit only when asked.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameListUndoTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        public RenameListUndoTests()
        {
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.RenameLog.Limit = 0;
            RenameLogStore.ClearLastOperation();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameLogStore.ClearLastOperation();
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies PrepareUndo seeds Preview OldValues without touching the filesystem.
        /// </summary>
        [Fact]
        public void PrepareUndo_seeds_preview_without_filesystem_change()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var goPlan = renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain);
            var goResults = renameList.Commit(goPlan, failFast: false);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(goResults).Status);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(renamedPath));
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.False(RenameLogStore.LastOperation.IsUndo);

            var prepare = renameList.PrepareUndo(RenameLogStore.LastOperation);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Equal(1, prepare.PreparedCount);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(renamedPath));
            Assert.False(RenameLogStore.LastOperation.IsUndo);

            var item = Assert.Single(renameList.RenameItems);
            Assert.Equal(renamedPath, item.Original.FullPath);
            Assert.Equal(sourcePath, item.Preview.FullPath);
            Assert.Equal(RenameStatus.PreviewOk, item.Status);
            Assert.NotNull(item.StickyUndoChanges);
            Assert.True(
                item.IsOverridden(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
                )
            );
        }

        /// <summary>
        /// Verifies PrepareUndo + Commit restores a prefix rename and marks the log as Undo.
        /// </summary>
        [Fact]
        public void PrepareUndo_then_Commit_restores_prefix_rename()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var goPlan = renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(goPlan, failFast: false)).Status);

            var prepare = renameList.PrepareUndo(RenameLogStore.LastOperation!);
            var results = renameList.Commit(prepare.Plan, failFast: false);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(results).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(renamedPath));
            Assert.Equal(sourcePath, Assert.Single(results).DestinationPath);
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.True(RenameLogStore.LastOperation.IsUndo);
            Assert.Equal(sourcePath, Assert.Single(RenameLogStore.LastOperation.Entries).DestinationPath);
            Assert.Contains(
                "Operation: Undo",
                RenameLogDisplay.FormatDetails(RenameLogStore.LastOperation),
                StringComparison.Ordinal
            );
            Assert.Null(Assert.Single(renameList.RenameItems).StickyUndoChanges);
        }

        /// <summary>
        /// Verifies empty-chain re-preview keeps sticky OldValues after PrepareUndo.
        /// </summary>
        [Fact]
        public void PrepareUndo_empty_chain_repreview_keeps_old_values()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            renameList.PrepareUndo(RenameLogStore.LastOperation!);
            renameList.Preview(FilterChain.CreateAllEnabled([]));

            var item = Assert.Single(renameList.RenameItems);
            Assert.Equal(renamedPath, item.Original.FullPath);
            Assert.Equal(sourcePath, item.Preview.FullPath);
            Assert.Equal(RenameStatus.PreviewOk, item.Status);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(renamedPath));
            Assert.True(
                item.IsOverridden(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
                )
            );
        }

        /// <summary>
        /// Verifies ForceValue mirrors rematerialize on re-preview after overrides were cleared (CommitError).
        /// </summary>
        [Fact]
        public void PrepareUndo_repreview_remirrors_force_value_after_overrides_cleared()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            renameList.PrepareUndo(RenameLogStore.LastOperation!);
            var item = Assert.Single(renameList.RenameItems);
            var nameKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            Assert.True(item.IsOverridden(nameKey));

            item.ClearAllOverrides();
            Assert.False(item.IsOverridden(nameKey));
            Assert.NotNull(item.StickyUndoChanges);

            renameList.Preview(FilterChain.CreateAllEnabled([]));

            Assert.Equal(sourcePath, item.Preview.FullPath);
            Assert.True(item.IsOverridden(nameKey));
            Assert.Equal(RenameStatus.PreviewOk, item.Status);
        }

        [Fact]
        public void PrepareUndo_then_Commit_restores_audio_tag_title_on_flac()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "metaflac.flac");
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");
            var sourcePath = dir.CombinePath("undo-flac.flac");
            File.Copy(fixturePath, sourcePath, overwrite: false);

            var originalTitle = AudioTagPersistence.Read(sourcePath).Semantic().Title;

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var goPlan = renameList.Preview(
                FilterChain.CreateAllEnabled([
                    new AudioTagSetterFilter(
                        new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "UndoFlacTitle"))
                    ),
                ])
            );
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(goPlan, failFast: false)).Status);
            Assert.Equal("UndoFlacTitle", AudioTagPersistence.Read(sourcePath).Semantic().Title);
            Assert.NotEqual("UndoFlacTitle", originalTitle);
            Assert.NotNull(RenameLogStore.LastOperation);

            var prepare = renameList.PrepareUndo(RenameLogStore.LastOperation);
            Assert.Equal("UndoFlacTitle", AudioTagPersistence.Read(sourcePath).Semantic().Title);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(prepare.Plan, failFast: false)).Status);
            Assert.Equal(originalTitle, AudioTagPersistence.Read(sourcePath).Semantic().Title);
        }

        /// <summary>
        /// Verifies PrepareUndo + Commit restores Hidden attribute on Windows.
        /// </summary>
        [WindowsFact]
        public void PrepareUndo_then_Commit_restores_hidden_attribute()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("attr.txt");
            File.WriteAllText(path, "x");
            Assert.False(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var goPlan = renameList.Preview(_SetHiddenAttributesPreset("go-attrs").Chain);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(goPlan, failFast: false)).Status);
            Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            Assert.NotNull(RenameLogStore.LastOperation);

            var prepare = renameList.PrepareUndo(RenameLogStore.LastOperation);
            Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            Assert.True(
                Assert
                    .Single(renameList.RenameItems)
                    .IsOverridden(
                        RenameListFieldKey.Preview(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs)
                    )
            );
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(prepare.Plan, failFast: false)).Status);
            Assert.False(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
        }

        /// <summary>
        /// Verifies Tag Remover strip-only rows are not undoable (list untouched).
        /// </summary>
        [Fact]
        public void PrepareUndo_skips_strip_all_embedded_tags_change()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("plain.txt");
            var keepPath = dir.CombinePath("keep.txt");
            File.WriteAllText(path, "x");
            File.WriteAllText(keepPath, "y");

            var log = new RenameLog(
                CommittedAt: DateTimeOffset.UtcNow,
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: path,
                        OriginalPath: path,
                        IsFolder: false,
                        Changes:
                        [
                            new RenamePropertyChange(
                                Property: RenamePropertyNames.StripAllEmbeddedTagsOnCommit,
                                OldValue: "false",
                                NewValue: "true"
                            ),
                        ]
                    ),
                ]
            );

            var renameList = new RenameList();
            renameList.AddSources([keepPath]);
            Assert.False(log.HasUndoableEntries);

            var prepare = renameList.PrepareUndo(log);
            Assert.Equal(0, prepare.PreparedCount);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Single(renameList.RenameItems);
            Assert.Equal(keepPath, renameList.RenameItems[0].Original.FullPath);
            Assert.True(File.Exists(path));
            Assert.True(File.Exists(keepPath));
        }

        /// <summary>
        /// Verifies PrepareUndo does not clear the current list when the log has no restorable rows.
        /// </summary>
        [Fact]
        public void PrepareUndo_non_undoable_log_does_not_clear_list()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var keepPath = dir.CombinePath("keep.txt");
            File.WriteAllText(keepPath, "y");

            var renameList = new RenameList();
            renameList.AddSources([keepPath]);

            var prepare = renameList.PrepareUndo(
                new RenameLog(
                    CommittedAt: DateTimeOffset.UtcNow,
                    Entries:
                    [
                        new RenameLogEntry(
                            DestinationPath: keepPath,
                            OriginalPath: keepPath,
                            IsFolder: false,
                            Changes: [new RenamePropertyChange("Prefix", "a", "b")],
                            Error: "prior commit failed"
                        ),
                    ]
                )
            );

            Assert.Equal(0, prepare.PreparedCount);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Single(renameList.RenameItems);
            Assert.Equal(keepPath, renameList.RenameItems[0].Original.FullPath);
        }

        /// <summary>
        /// Verifies PrepareUndo + Commit restores a prefix when the entry also has an unrestorable strip delta.
        /// </summary>
        [Fact]
        public void PrepareUndo_then_Commit_restores_prefix_when_entry_also_has_strip_change()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            var last = RenameLogStore.LastOperation!;
            var entry = Assert.Single(last.Entries);
            var withStrip = entry with
            {
                Changes =
                [
                    .. entry.Changes,
                    new RenamePropertyChange(
                        Property: RenamePropertyNames.StripAllEmbeddedTagsOnCommit,
                        OldValue: "false",
                        NewValue: "true"
                    ),
                ],
            };
            var log = last with { Entries = [withStrip] };

            var prepare = renameList.PrepareUndo(log);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(prepare.Plan, failFast: false)).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(renamedPath));
        }

        /// <summary>
        /// Verifies PrepareUndo with an empty entry list is a no-op.
        /// </summary>
        [Fact]
        public void PrepareUndo_empty_log_returns_empty_results()
        {
            var renameList = new RenameList();
            var prepare = renameList.PrepareUndo(new RenameLog(DateTimeOffset.UtcNow, Entries: []));
            Assert.Equal(0, prepare.PreparedCount);
            Assert.Equal(0, prepare.NotLoadedCount);
            Assert.Empty(renameList.RenameItems);
        }

        /// <summary>
        /// Verifies PrepareUndo reports NotLoadedCount when DestinationPath files are already gone.
        /// </summary>
        [Fact]
        public void PrepareUndo_missing_destination_reports_not_loaded()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var missingPath = dir.CombinePath("gone.txt");
            var keepPath = dir.CombinePath("keep.txt");
            File.WriteAllText(keepPath, "y");

            var renameList = new RenameList();
            renameList.AddSources([keepPath]);

            var prepare = renameList.PrepareUndo(
                new RenameLog(
                    CommittedAt: DateTimeOffset.UtcNow,
                    Entries:
                    [
                        new RenameLogEntry(
                            DestinationPath: missingPath,
                            OriginalPath: dir.CombinePath("old.txt"),
                            IsFolder: false,
                            Changes: [new RenamePropertyChange("Prefix", "old", "gone")]
                        ),
                    ]
                )
            );

            Assert.Equal(0, prepare.PreparedCount);
            Assert.Equal(1, prepare.NotLoadedCount);
            Assert.Empty(renameList.RenameItems);
        }

        /// <summary>
        /// Verifies PrepareUndo loads present rows and counts missing destinations separately.
        /// </summary>
        [Fact]
        public void PrepareUndo_partial_missing_destination_reports_not_loaded()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            var missingPath = dir.CombinePath("missing.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            var last = RenameLogStore.LastOperation!;
            var present = Assert.Single(last.Entries);
            var log = last with
            {
                Entries =
                [
                    present,
                    new RenameLogEntry(
                        DestinationPath: missingPath,
                        OriginalPath: dir.CombinePath("was.txt"),
                        IsFolder: false,
                        Changes: [new RenamePropertyChange("Prefix", "was", "missing")]
                    ),
                ],
            };

            var prepare = renameList.PrepareUndo(log);
            Assert.Equal(1, prepare.NotLoadedCount);
            Assert.Equal(1, prepare.PreparedCount);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(renamedPath));
            Assert.Equal(sourcePath, Assert.Single(renameList.RenameItems).Preview.FullPath);
        }

        /// <summary>
        /// Verifies RefreshOriginals clears the sticky undo seed.
        /// </summary>
        [Fact]
        public void RefreshOriginals_clears_sticky_undo_seed()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            renameList.PrepareUndo(RenameLogStore.LastOperation!);
            Assert.NotNull(Assert.Single(renameList.RenameItems).StickyUndoChanges);

            renameList.RefreshOriginals();
            Assert.Null(Assert.Single(renameList.RenameItems).StickyUndoChanges);
        }

        /// <summary>
        /// Verifies a dry-run Commit after PrepareUndo does not consume the IsUndo pending flag.
        /// </summary>
        [Fact]
        public void PrepareUndo_dry_run_Commit_does_not_consume_pending_IsUndo()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            var renamedPath = dir.CombinePath("new-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            var prepare = renameList.PrepareUndo(RenameLogStore.LastOperation!);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert.Single(renameList.Commit(prepare.Plan, failFast: false, dryRun: true)).Status
            );
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(renamedPath));
            Assert.False(RenameLogStore.LastOperation!.IsUndo);
            Assert.NotNull(Assert.Single(renameList.RenameItems).StickyUndoChanges);

            // Dry-run clears Preview; sticky seed must reapply via Preview before a real GO.
            var retryPlan = renameList.Preview(FilterChain.CreateAllEnabled([]));
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(retryPlan, failFast: false)).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.True(RenameLogStore.LastOperation.IsUndo);
        }

        /// <summary>
        /// Verifies RefreshOriginals clears pending IsUndo so a later GO is not marked Undo.
        /// </summary>
        [Fact]
        public void RefreshOriginals_clears_pending_IsUndo()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("old-name.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Equal(
                RenameStatus.CommitOk,
                Assert
                    .Single(
                        renameList.Commit(
                            renameList.Preview(_PrefixFormatterPreset("go", "new-name").Chain),
                            failFast: false
                        )
                    )
                    .Status
            );

            renameList.PrepareUndo(RenameLogStore.LastOperation!);
            renameList.RefreshOriginals();

            var goPlan = renameList.Preview(_PrefixFormatterPreset("again", "again-").Chain);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(renameList.Commit(goPlan, failFast: false)).Status);
            Assert.False(RenameLogStore.LastOperation!.IsUndo);
        }

        private static FilterPreset _PrefixFormatterPreset(string name, string prefix)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new FormatterFilter(Target: new FilePrefixTarget(), Options: new FormatterOptions(prefix)),
                ]),
            };
        }

        private static FilterPreset _SetHiddenAttributesPreset(string name)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = FilterChain.CreateAllEnabled([
                    new AttributesSetterFilter(
                        Options: new AttributesSetterOptions(
                            ReadOnly: AttributeTriState.Keep,
                            Hidden: AttributeTriState.Set,
                            Archive: AttributeTriState.Keep,
                            System: AttributeTriState.Keep
                        )
                    ),
                ]),
            };
        }
    }
}

using Mfr.Filters.Attributes;
using Mfr.Filters.Formatting;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Engine undo round-trips: rebuild list from last op, apply OldValues, re-Commit.
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
        /// Verifies Undo restores a prefix rename via OldValues + Commit.
        /// </summary>
        [Fact]
        public void Undo_restores_prefix_rename()
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

            var undoResults = renameList.Undo(RenameLogStore.LastOperation);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(undoResults).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(renamedPath));
            Assert.Equal(sourcePath, Assert.Single(undoResults).DestinationPath);
            Assert.NotNull(RenameLogStore.LastOperation);
            Assert.Equal(sourcePath, Assert.Single(RenameLogStore.LastOperation.Entries).DestinationPath);
        }

        /// <summary>
        /// Verifies Undo restores Hidden attribute on Windows (attribute-only GO).
        /// </summary>
        [WindowsFact]
        public void Undo_restores_hidden_attribute()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = dir.CombinePath("attr.txt");
            File.WriteAllText(path, "x");
            Assert.False(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));

            var renameList = new RenameList();
            renameList.AddSources([path]);
            var goPlan = renameList.Preview(_SetHiddenAttributesPreset("go-attrs").Chain);
            var goResults = renameList.Commit(goPlan, failFast: false);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(goResults).Status);
            Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            Assert.NotNull(RenameLogStore.LastOperation);

            var undoResults = renameList.Undo(RenameLogStore.LastOperation);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(undoResults).Status);
            Assert.False(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
        }

        /// <summary>
        /// Verifies Tag Remover strip-only rows are not undoable (list untouched, no CommitOk).
        /// </summary>
        [Fact]
        public void Undo_skips_strip_all_embedded_tags_change()
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
                                Property: "StripAllEmbeddedTagsOnCommit",
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

            var results = renameList.Undo(log);
            Assert.Empty(results);
            Assert.Single(renameList.RenameItems);
            Assert.Equal(keepPath, renameList.RenameItems[0].Original.FullPath);
            Assert.True(File.Exists(path));
            Assert.True(File.Exists(keepPath));
        }

        /// <summary>
        /// Verifies Undo does not clear the current list when the log has no restorable rows.
        /// </summary>
        [Fact]
        public void Undo_non_undoable_log_does_not_clear_list()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var keepPath = dir.CombinePath("keep.txt");
            File.WriteAllText(keepPath, "y");

            var renameList = new RenameList();
            renameList.AddSources([keepPath]);

            var results = renameList.Undo(
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

            Assert.Empty(results);
            Assert.Single(renameList.RenameItems);
            Assert.Equal(keepPath, renameList.RenameItems[0].Original.FullPath);
        }

        /// <summary>
        /// Verifies Undo restores a prefix rename when the same entry also logged an unrestorable strip delta.
        /// </summary>
        [Fact]
        public void Undo_restores_prefix_when_entry_also_has_strip_change()
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

            var last = RenameLogStore.LastOperation!;
            var entry = Assert.Single(last.Entries);
            var withStrip = entry with
            {
                Changes =
                [
                    .. entry.Changes,
                    new RenamePropertyChange(
                        Property: "StripAllEmbeddedTagsOnCommit",
                        OldValue: "false",
                        NewValue: "true"
                    ),
                ],
            };
            var log = last with { Entries = [withStrip] };

            var undoResults = renameList.Undo(log);
            Assert.Equal(RenameStatus.CommitOk, Assert.Single(undoResults).Status);
            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(renamedPath));
        }

        /// <summary>
        /// Verifies Undo with an empty entry list is a no-op.
        /// </summary>
        [Fact]
        public void Undo_empty_log_returns_empty_results()
        {
            var renameList = new RenameList();
            var results = renameList.Undo(new RenameLog(DateTimeOffset.UtcNow, Entries: []));
            Assert.Empty(results);
            Assert.Empty(renameList.RenameItems);
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

using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Path/filesystem rename-log property map and OldValue apply.
    /// </summary>
    public sealed class RenamePropertyFileMetaTests
    {
        /// <summary>
        /// Verifies path and Extended properties map to preview catalog keys.
        /// </summary>
        [Theory]
        [InlineData(RenamePropertyNames.Prefix, BasicRenameListField.Group, BasicRenameListFields.Key.Name)]
        [InlineData(RenamePropertyNames.Extension, BasicRenameListField.Group, BasicRenameListFields.Key.Extension)]
        [InlineData(RenamePropertyNames.DirectoryPath, BasicRenameListField.Group, BasicRenameListFields.Key.Folder)]
        [InlineData(RenamePropertyNames.Attributes, ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs)]
        [InlineData(
            RenamePropertyNames.CreationTime,
            ExtendedRenameListFields.Group,
            ExtendedRenameListFields.Key.CreationDate
        )]
        [InlineData(
            RenamePropertyNames.LastWriteTime,
            ExtendedRenameListFields.Group,
            ExtendedRenameListFields.Key.LastWriteDate
        )]
        [InlineData(
            RenamePropertyNames.LastAccessTime,
            ExtendedRenameListFields.Group,
            ExtendedRenameListFields.Key.LastAccessDate
        )]
        public void TryMapPreview_maps_path_and_extended(string property, string groupId, string propertyKey)
        {
            Assert.True(RenamePropertyFileMeta.TryMapPreview(property, out var key));
            Assert.Equal(RenameListFieldKey.Preview(groupId, propertyKey), key);
            Assert.True(RenameListFieldCatalog.TryGetField(key, out var field));
            Assert.True(field.SupportsWrite);
        }

        /// <summary>
        /// Verifies unrestorable strip and AudioTag.Block rows stay unmapped.
        /// </summary>
        [Theory]
        [InlineData(RenamePropertyNames.StripAllEmbeddedTagsOnCommit)]
        [InlineData("AudioTag.Block.Xiph.TITLE")]
        [InlineData("")]
        public void TryMapPreview_skips_unmapped(string property)
        {
            Assert.False(RenamePropertyFileMeta.TryMapPreview(property, out _));
        }

        /// <summary>
        /// Verifies logged Prefix/Extension/directory/Extended names use catalog labels (File Name, not Prefix).
        /// </summary>
        [Theory]
        [InlineData(RenamePropertyNames.Prefix, PathFieldLabels.FileName)]
        [InlineData(RenamePropertyNames.Extension, PathFieldLabels.FileExtension)]
        [InlineData(RenamePropertyNames.DirectoryPath, PathFieldLabels.ParentDirectory)]
        [InlineData(RenamePropertyNames.Attributes, "Attributes")]
        [InlineData(RenamePropertyNames.CreationTime, "Creation Date")]
        [InlineData(RenamePropertyNames.LastWriteTime, "Last Write Date")]
        [InlineData(RenamePropertyNames.LastAccessTime, "Last Access Date")]
        public void FormatDisplayName_uses_catalog_labels(string property, string expected)
        {
            Assert.Equal(expected, RenamePropertyFileMeta.FormatDisplayName(property));
        }

        /// <summary>
        /// Verifies unrestorable strip and AudioTag.Block rows keep their stored names.
        /// </summary>
        [Theory]
        [InlineData(RenamePropertyNames.StripAllEmbeddedTagsOnCommit)]
        [InlineData("AudioTag.Block.Xiph.TITLE")]
        public void FormatDisplayName_keeps_unmapped_names(string property)
        {
            Assert.Equal(property, RenamePropertyFileMeta.FormatDisplayName(property));
        }

        /// <summary>
        /// Verifies TryApplyOldValue writes path and filesystem scalars onto Preview.
        /// </summary>
        [Fact]
        public void TryApplyOldValue_writes_path_and_file_meta()
        {
            var preview = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: @"C:\old",
                prefix: "a",
                extension: "txt",
                attributes: FileAttributes.Normal,
                creationTime: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local),
                lastWriteTime: new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Local),
                lastAccessTime: new DateTime(2020, 1, 3, 0, 0, 0, DateTimeKind.Local)
            );

            Assert.True(RenamePropertyFileMeta.TryApplyOldValue(preview, RenamePropertyNames.Prefix, "b"));
            Assert.True(RenamePropertyFileMeta.TryApplyOldValue(preview, RenamePropertyNames.Extension, "bak"));
            Assert.True(RenamePropertyFileMeta.TryApplyOldValue(preview, RenamePropertyNames.DirectoryPath, @"C:\new"));
            Assert.True(RenamePropertyFileMeta.TryApplyOldValue(preview, RenamePropertyNames.Attributes, "Hidden"));
            Assert.True(
                RenamePropertyFileMeta.TryApplyOldValue(
                    preview,
                    RenamePropertyNames.CreationTime,
                    "2021-02-03T04:05:06.0000000"
                )
            );

            Assert.Equal("b", preview.Prefix);
            Assert.Equal("bak", preview.Extension);
            Assert.Equal(@"C:\new", preview.DirectoryPath);
            Assert.Equal(FileAttributes.Hidden, preview.Attributes);
            Assert.Equal(2021, preview.CreationTime.Year);
            Assert.False(RenamePropertyFileMeta.TryApplyOldValue(preview, "AudioTag.Block.Xiph.TITLE", "x"));
        }

        /// <summary>
        /// Verifies CollectPreviewKeys dedupes and skips unmapped properties.
        /// </summary>
        [Fact]
        public void CollectPreviewKeys_dedupes_and_skips_unmapped()
        {
            var keys = RenamePropertyFileMeta.CollectPreviewKeys([
                new RenamePropertyChange(RenamePropertyNames.Prefix, "a", "b"),
                new RenamePropertyChange(RenamePropertyNames.StripAllEmbeddedTagsOnCommit, "false", "true"),
                new RenamePropertyChange(RenamePropertyNames.Prefix, "a", "c"),
                new RenamePropertyChange(RenamePropertyNames.Attributes, "Normal", "Hidden"),
            ]);

            Assert.Equal(
                [
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies CollectPreviewKeysFromLog skips non-undoable entries and dedupes across entries.
        /// </summary>
        [Fact]
        public void CollectPreviewKeysFromLog_skips_non_undoable_and_dedupes()
        {
            var log = new RenameLog(
                CommittedAt: DateTimeOffset.UtcNow,
                Entries:
                [
                    new RenameLogEntry(
                        DestinationPath: @"C:\b.txt",
                        OriginalPath: @"C:\a.txt",
                        IsFolder: false,
                        Changes:
                        [
                            new RenamePropertyChange(RenamePropertyNames.Prefix, "a", "b"),
                            new RenamePropertyChange(RenamePropertyNames.Attributes, "Normal", "Hidden"),
                        ]
                    ),
                    new RenameLogEntry(
                        DestinationPath: @"C:\d.txt",
                        OriginalPath: @"C:\c.txt",
                        IsFolder: false,
                        Changes: [new RenamePropertyChange(RenamePropertyNames.Extension, "txt", "bak")],
                        Error: "failed"
                    ),
                    new RenameLogEntry(
                        DestinationPath: @"C:\f.txt",
                        OriginalPath: @"C:\e.txt",
                        IsFolder: false,
                        Changes: [new RenamePropertyChange(RenamePropertyNames.Prefix, "e", "f")]
                    ),
                ]
            );

            Assert.Equal(
                [
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs),
                ],
                RenamePropertyFileMeta.CollectPreviewKeysFromLog(log)
            );
        }
    }
}

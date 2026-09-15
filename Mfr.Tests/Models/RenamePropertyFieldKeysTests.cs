using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Maps rename-log property names to preview-side Rename List field keys.
    /// </summary>
    public sealed class RenamePropertyFieldKeysTests
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
            Assert.True(RenamePropertyFieldKeys.TryMapPreview(property, out var key));
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
            Assert.False(RenamePropertyFieldKeys.TryMapPreview(property, out _));
        }

        /// <summary>
        /// Verifies CollectPreviewKeys dedupes and skips unmapped properties.
        /// </summary>
        [Fact]
        public void CollectPreviewKeys_dedupes_and_skips_unmapped()
        {
            var keys = RenamePropertyFieldKeys.CollectPreviewKeys([
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
                RenamePropertyFieldKeys.CollectPreviewKeysFromLog(log)
            );
        }
    }
}

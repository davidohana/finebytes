using System.Globalization;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for Extended Attrs/date <see cref="FilterTarget"/> Get/SetTargetString wiring.
    /// </summary>
    public sealed class ExtendedWriteTargetTests
    {
        /// <summary>
        /// Verifies attributes round-trip through RAHS display form and preserve non-RAHS bits.
        /// </summary>
        [Fact]
        public void Attributes_get_set_round_trips_rahs_and_preserves_directory()
        {
            var meta = _CreateMeta(
                attributes: FileAttributes.Directory | FileAttributes.ReadOnly | FileAttributes.Archive
            );
            var target = new FileAttributesTarget();

            Assert.Equal("RA--", meta.GetTargetString(target));

            meta.SetTargetString(target, "-A-S");
            Assert.Equal(FileAttributes.Directory | FileAttributes.Archive | FileAttributes.System, meta.Attributes);
            Assert.Equal("-A-S", meta.GetTargetString(target));
        }

        /// <summary>
        /// Verifies attributes Set accepts rename-log <see cref="FileAttributes"/> enum names.
        /// </summary>
        [Fact]
        public void Attributes_set_accepts_enum_name_from_log()
        {
            var meta = _CreateMeta(attributes: FileAttributes.Normal);
            var target = new FileAttributesTarget();

            meta.SetTargetString(target, (FileAttributes.Hidden | FileAttributes.System).ToString());

            Assert.Equal(FileAttributes.Hidden | FileAttributes.System, meta.Attributes);
            Assert.Equal("--HS", meta.GetTargetString(target));
        }

        /// <summary>
        /// Verifies invalid attributes strings throw for Manual Override PreviewError.
        /// </summary>
        [Fact]
        public void Attributes_set_rejects_invalid_value()
        {
            var meta = _CreateMeta();
            Assert.Throws<ArgumentException>(() => meta.SetTargetString(new FileAttributesTarget(), "nope"));
            Assert.Throws<ArgumentException>(() => meta.SetTargetString(new FileAttributesTarget(), ""));
        }

        /// <summary>
        /// Verifies each timestamp target Get/Set round-trips culture display and log <c>O</c> form.
        /// </summary>
        [Theory]
        [InlineData(TimestampField.Creation)]
        [InlineData(TimestampField.LastWrite)]
        [InlineData(TimestampField.LastAccess)]
        public void Timestamp_get_set_round_trips_display_and_roundtrip_o(TimestampField field)
        {
            var stamp = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Unspecified);
            var meta = _CreateMeta(
                creationTime: stamp.AddDays(-1),
                lastWriteTime: stamp.AddDays(-2),
                lastAccessTime: stamp.AddDays(-3)
            );
            var target = new FileTimestampTarget(field);

            meta.SetTargetString(target, stamp.ToString("O", CultureInfo.InvariantCulture));
            Assert.Equal(stamp, meta.GetTimestamp(field));
            Assert.Equal(stamp.ToString("G", CultureInfo.CurrentCulture), meta.GetTargetString(target));

            var display = stamp.AddHours(1).ToString("G", CultureInfo.CurrentCulture);
            meta.SetTargetString(target, display);
            Assert.Equal(display, meta.GetTargetString(target));
        }

        /// <summary>
        /// Verifies blank and out-of-range timestamps throw for Manual Override PreviewError.
        /// </summary>
        [Theory]
        [InlineData("   ")]
        [InlineData("not-a-date")]
        [InlineData("1500-01-01")]
        [InlineData("3026-01-01")]
        public void Timestamp_set_rejects_blank_invalid_and_out_of_range(string value)
        {
            var meta = _CreateMeta();
            Assert.Throws<ArgumentException>(() =>
                meta.SetTargetString(new FileTimestampTarget(TimestampField.LastWrite), value)
            );
        }

        /// <summary>
        /// Verifies a preview-side Creation Date override survives re-preview via WriteTarget.
        /// </summary>
        [Fact]
        public void Preview_override_creation_date_survives_repreview()
        {
            var stamp = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Unspecified);
            var meta = _CreateMeta(creationTime: stamp.AddDays(-1));
            var item = new RenameItem(meta);
            var previewCreation = RenameListFieldKey.Preview(
                ExtendedRenameListFields.Group,
                ExtendedRenameListFields.Key.CreationDate
            );
            var overrideText = stamp.ToString("G", CultureInfo.CurrentCulture);

            item.SetOverride(previewCreation, overrideText);
            Assert.True(RenameListFieldOverrides.TryApplyToPreview(item, isPreview: true));
            Assert.Equal(overrideText, RenameListFieldCatalog.Resolve(item, previewCreation));

            item.Preview.CreationTime = stamp.AddYears(1);
            Assert.True(RenameListFieldOverrides.TryApplyToPreview(item, isPreview: true));
            Assert.Equal(overrideText, RenameListFieldCatalog.Resolve(item, previewCreation));
        }

        /// <summary>
        /// Verifies a preview-side Attrs override survives re-preview via WriteTarget.
        /// </summary>
        [Fact]
        public void Preview_override_attrs_survives_repreview()
        {
            var meta = _CreateMeta(attributes: FileAttributes.Archive);
            var item = new RenameItem(meta);
            var previewAttrs = RenameListFieldKey.Preview(
                ExtendedRenameListFields.Group,
                ExtendedRenameListFields.Key.Attrs
            );

            item.SetOverride(previewAttrs, "R---");
            Assert.True(RenameListFieldOverrides.TryApplyToPreview(item, isPreview: true));
            Assert.Equal(FileAttributes.ReadOnly, item.Preview.Attributes);

            item.Preview.Attributes = FileAttributes.Hidden;
            Assert.True(RenameListFieldOverrides.TryApplyToPreview(item, isPreview: true));
            Assert.Equal(FileAttributes.ReadOnly, item.Preview.Attributes);
            Assert.Equal("R---", RenameListFieldCatalog.Resolve(item, previewAttrs));
        }

        private static FileMeta _CreateMeta(
            FileAttributes attributes = FileAttributes.Normal,
            DateTime creationTime = default,
            DateTime lastWriteTime = default,
            DateTime lastAccessTime = default
        )
        {
            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: TestPaths.Absolute("album"),
                fileName: "track",
                extension: "mp3",
                attributes: attributes,
                creationTime: creationTime,
                lastWriteTime: lastWriteTime,
                lastAccessTime: lastAccessTime
            );
        }
    }
}

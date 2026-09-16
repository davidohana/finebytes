using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Misc;
using Mfr.Filters.Space;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="IFixedApplyToFilter"/> on fixed-domain filters.
    /// </summary>
    public sealed class FixedApplyToLabelTests
    {
        /// <summary>
        /// Verifies Attributes Setter uses the Extended Rename List Attributes column title.
        /// </summary>
        [Fact]
        public void Attributes_setter_uses_extended_attributes_column()
        {
            Assert.Equal("Attributes", new AttributesSetterFilter().FixedApplyToLabel);
        }

        /// <summary>
        /// Verifies Path Mover uses the Parent Directory path-field label.
        /// </summary>
        [Fact]
        public void Path_mover_uses_parent_directory_label()
        {
            Assert.Equal(PathFieldLabels.ParentDirectory, new PathMoverFilter().FixedApplyToLabel);
        }

        /// <summary>
        /// Verifies Date/Time Setter and Time Shifter reflect the configured timestamp field.
        /// </summary>
        [Theory]
        [InlineData(TimestampField.Creation, "Creation Date")]
        [InlineData(TimestampField.LastWrite, "Last Write Date")]
        [InlineData(TimestampField.LastAccess, "Last Access Date")]
        public void Timestamp_filters_use_extended_date_column(TimestampField field, string expected)
        {
            var dateTimeSetter = new DateTimeSetterFilter(
                new DateTimeSetterOptions(
                    TimestampField: field,
                    SetDate: true,
                    Date: new DateOnly(2024, 1, 1),
                    SetTime: false,
                    Time: default
                )
            );
            var timeShifter = new TimeShifterFilter(
                new TimeShifterOptions(TimestampField: field, Amount: 1, Unit: TimeShiftUnit.Days)
            );

            Assert.Equal(expected, dateTimeSetter.FixedApplyToLabel);
            Assert.Equal(expected, timeShifter.FixedApplyToLabel);
        }

        /// <summary>
        /// Verifies Audio Tag Setter and Tag Remover share the audio-tags label.
        /// </summary>
        [Fact]
        public void Audio_tag_filters_use_audio_tags_label()
        {
            Assert.Equal(FixedFilterApplyToLabels.AudioTags, new AudioTagSetterFilter().FixedApplyToLabel);
            Assert.Equal(FixedFilterApplyToLabels.AudioTags, new TagRemoverFilter().FixedApplyToLabel);
        }

        /// <summary>
        /// Verifies ID3v2 Field Setter uses the friendly frame label for the configured frame id.
        /// </summary>
        [Fact]
        public void Id3v2_field_setter_uses_frame_label()
        {
            Assert.Equal("TIT2 (Title)", new Id3v2FieldSetterFilter().FixedApplyToLabel);
            Assert.Equal(
                Id3v2FrameLabels.For("COMM"),
                new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "comm")).FixedApplyToLabel
            );
        }

        /// <summary>
        /// Verifies string-target and state-only filters do not implement <see cref="IFixedApplyToFilter"/>.
        /// </summary>
        [Fact]
        public void String_and_state_only_filters_do_not_implement_fixed_apply_to()
        {
            Assert.IsNotAssignableFrom<IFixedApplyToFilter>(new ShrinkSpacesFilter());
            Assert.IsNotAssignableFrom<IFixedApplyToFilter>(new SentenceEndCharactersFilter());
        }
    }
}

using Mfr.Filters.Attributes;

namespace Mfr.Tests.Models.Filters.Attributes
{
    /// <summary>
    /// Tests for <see cref="DateTimeSetterFilter"/>.
    /// </summary>
    public sealed class DateTimeSetterFilterTests
    {
        private static readonly DateTime s_Base = new(2024, 3, 15, 14, 5, 30, 123, DateTimeKind.Unspecified);

        public static TheoryData<TimestampField> TimestampFields { get; } =
        [TimestampField.Creation, TimestampField.LastWrite, TimestampField.LastAccess];

        [Theory]
        [MemberData(nameof(TimestampFields))]
        public void DateOnly_preserves_time_of_day_on_selected_field(TimestampField field)
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base,
                lastWriteTime: s_Base,
                lastAccessTime: s_Base
            );
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: field,
                    SetDate: true,
                    Date: new DateOnly(2020, 12, 25),
                    SetTime: false,
                    Time: new TimeOnly(0, 0, 0)
                )
            );
            filter.Setup();
            filter.Apply(item);

            var expected = new DateTime(2020, 12, 25, 14, 5, 30, 123, DateTimeKind.Unspecified);
            Assert.Equal(expected, _Read(item.Preview, field));
            _AssertOtherFieldsUnchanged(item.Preview, field);
        }

        [Theory]
        [MemberData(nameof(TimestampFields))]
        public void TimeOnly_preserves_calendar_date_on_selected_field(TimestampField field)
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base,
                lastWriteTime: s_Base,
                lastAccessTime: s_Base
            );
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: field,
                    SetDate: false,
                    Date: new DateOnly(2000, 1, 1),
                    SetTime: true,
                    Time: new TimeOnly(9, 0, 15)
                )
            );
            filter.Setup();
            filter.Apply(item);

            var expected = new DateTime(2024, 3, 15, 9, 0, 15, DateTimeKind.Unspecified);
            Assert.Equal(expected, _Read(item.Preview, field));
            _AssertOtherFieldsUnchanged(item.Preview, field);
        }

        [Fact]
        public void DateAndTime_LastAccess_sets_both()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: s_Base);
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastAccess,
                    SetDate: true,
                    Date: new DateOnly(2019, 1, 1),
                    SetTime: true,
                    Time: new TimeOnly(23, 59, 1)
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2019, 1, 1, 23, 59, 1, DateTimeKind.Unspecified), item.Preview.LastAccessTime);
        }

        [Fact]
        public void Neither_no_op()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: false,
                    Date: new DateOnly(2019, 1, 1),
                    SetTime: false,
                    Time: new TimeOnly(23, 59, 1)
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(s_Base, item.Preview.LastWriteTime);
        }

        [Fact]
        public void Preserves_DateTimeKind()
        {
            var localBase = new DateTime(2024, 3, 15, 14, 5, 30, DateTimeKind.Local);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: localBase);

            var setDate = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: true,
                    Date: new DateOnly(2020, 12, 25),
                    SetTime: false,
                    Time: new TimeOnly(0, 0, 0)
                )
            );
            setDate.Setup();
            setDate.Apply(item);
            Assert.Equal(DateTimeKind.Local, item.Preview.LastWriteTime.Kind);

            var setTime = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: false,
                    Date: new DateOnly(2000, 1, 1),
                    SetTime: true,
                    Time: new TimeOnly(9, 0, 15)
                )
            );
            setTime.Setup();
            setTime.Apply(item);

            Assert.Equal(DateTimeKind.Local, item.Preview.LastWriteTime.Kind);
            Assert.Equal(new DateTime(2020, 12, 25, 9, 0, 15, DateTimeKind.Local), item.Preview.LastWriteTime);
        }

        [Theory]
        [InlineData(1600, 12, 31)]
        [InlineData(3026, 9, 5)]
        [InlineData(2101, 1, 1)]
        public void Out_of_range_date_skips_date_but_still_applies_time(int year, int month, int day)
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: true,
                    Date: new DateOnly(year, month, day),
                    SetTime: true,
                    Time: new TimeOnly(18, 14, 0)
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 15, 18, 14, 0, DateTimeKind.Unspecified), item.Preview.LastWriteTime);
        }

        [Fact]
        public void Out_of_range_date_with_time_off_is_no_op()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: true,
                    Date: new DateOnly(1600, 12, 31),
                    SetTime: false,
                    Time: new TimeOnly(12, 0, 0)
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(s_Base, item.Preview.LastWriteTime);
        }

        [Fact]
        public void Max_supported_file_date_applies()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    SetDate: true,
                    Date: FileTimestampDateLimits.Max,
                    SetTime: false,
                    Time: new TimeOnly(0, 0, 0)
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(
                FileTimestampDateLimits.Max.ToDateTime(TimeOnly.FromDateTime(s_Base), s_Base.Kind),
                item.Preview.LastWriteTime
            );
        }

        private static void _AssertOtherFieldsUnchanged(FileMeta preview, TimestampField selected)
        {
            foreach (var field in Enum.GetValues<TimestampField>())
            {
                if (field == selected)
                {
                    continue;
                }

                Assert.Equal(s_Base, _Read(preview, field));
            }
        }

        private static DateTime _Read(FileMeta preview, TimestampField field)
        {
            return field switch
            {
                TimestampField.Creation => preview.CreationTime,
                TimestampField.LastWrite => preview.LastWriteTime,
                TimestampField.LastAccess => preview.LastAccessTime,
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
        }
    }
}

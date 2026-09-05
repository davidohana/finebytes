using Mfr.Filters.Attributes;

namespace Mfr.Tests.Models.Filters.Attributes
{
    /// <summary>
    /// Tests for <see cref="DateSetterFilter"/> and <see cref="TimeSetterFilter"/>.
    /// </summary>
    public sealed class DateTimeSetterFilterTests
    {
        private static readonly DateTime s_Base = new(2024, 3, 15, 14, 5, 30, 123, DateTimeKind.Unspecified);

        public static TheoryData<TimestampField> TimestampFields { get; } =
        [TimestampField.Creation, TimestampField.LastWrite, TimestampField.LastAccess];

        [Theory]
        [MemberData(nameof(TimestampFields))]
        public void DateSetter_preserves_time_of_day_on_selected_field(TimestampField field)
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base,
                lastWriteTime: s_Base,
                lastAccessTime: s_Base
            );
            var filter = new DateSetterFilter(
                Options: new DateSetterOptions(TimestampField: field, Date: new DateOnly(2020, 12, 25))
            );
            filter.Setup();
            filter.Apply(item);

            var expected = new DateTime(2020, 12, 25, 14, 5, 30, 123, DateTimeKind.Unspecified);
            Assert.Equal(expected, _Read(item.Preview, field));
            _AssertOtherFieldsUnchanged(item.Preview, field);
        }

        [Theory]
        [MemberData(nameof(TimestampFields))]
        public void TimeSetter_preserves_calendar_date_on_selected_field(TimestampField field)
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base,
                lastWriteTime: s_Base,
                lastAccessTime: s_Base
            );
            var filter = new TimeSetterFilter(
                Options: new TimeSetterOptions(TimestampField: field, Time: new TimeOnly(9, 0, 15))
            );
            filter.Setup();
            filter.Apply(item);

            var expected = new DateTime(2024, 3, 15, 9, 0, 15, DateTimeKind.Unspecified);
            Assert.Equal(expected, _Read(item.Preview, field));
            _AssertOtherFieldsUnchanged(item.Preview, field);
        }

        [Fact]
        public void DateSetter_and_TimeSetter_preserve_DateTimeKind()
        {
            var localBase = new DateTime(2024, 3, 15, 14, 5, 30, DateTimeKind.Local);
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: localBase);

            var setDate = new DateSetterFilter(
                Options: new DateSetterOptions(
                    TimestampField: TimestampField.LastWrite,
                    Date: new DateOnly(2020, 12, 25)
                )
            );
            setDate.Setup();
            setDate.Apply(item);
            Assert.Equal(DateTimeKind.Local, item.Preview.LastWriteTime.Kind);

            var setTime = new TimeSetterFilter(
                Options: new TimeSetterOptions(TimestampField: TimestampField.LastWrite, Time: new TimeOnly(9, 0, 15))
            );
            setTime.Setup();
            setTime.Apply(item);

            Assert.Equal(DateTimeKind.Local, item.Preview.LastWriteTime.Kind);
            Assert.Equal(new DateTime(2020, 12, 25, 9, 0, 15, DateTimeKind.Local), item.Preview.LastWriteTime);
        }

        [Fact]
        public void Chain_DateSetter_then_TimeSetter_composes_on_last_access()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: s_Base);
            var setDate = new DateSetterFilter(
                Options: new DateSetterOptions(
                    TimestampField: TimestampField.LastAccess,
                    Date: new DateOnly(2019, 1, 1)
                )
            );
            var setTime = new TimeSetterFilter(
                Options: new TimeSetterOptions(TimestampField: TimestampField.LastAccess, Time: new TimeOnly(23, 59, 1))
            );
            var chain = FilterChain.CreateAllEnabled([setDate, setTime]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal(new DateTime(2019, 1, 1, 23, 59, 1, DateTimeKind.Unspecified), item.Preview.LastAccessTime);
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

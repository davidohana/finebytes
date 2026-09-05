using Mfr.Filters.Attributes;

namespace Mfr.Tests.Models.Filters.Attributes
{
    /// <summary>
    /// Tests for <see cref="TimeShifterFilter"/>.
    /// </summary>
    public sealed class TimeShifterFilterTests
    {
        private static readonly DateTime s_Base = new(2024, 3, 15, 14, 5, 30, DateTimeKind.Unspecified);

        [Fact]
        public void TimeShifter_LastWrite_adds_one_day()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastWrite,
                    Amount: 1,
                    Unit: TimeShiftUnit.Days
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 16, 14, 5, 30, DateTimeKind.Unspecified), item.Preview.LastWriteTime);
            Assert.Equal(item.Original.CreationTime, item.Preview.CreationTime);
            Assert.Equal(item.Original.LastAccessTime, item.Preview.LastAccessTime);
        }

        [Fact]
        public void TimeShifter_Creation_negative_hours()
        {
            var item = FilterTestHelpers.CreateRenameItem(creationTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.Creation,
                    Amount: -2,
                    Unit: TimeShiftUnit.Hours
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 15, 12, 5, 30, DateTimeKind.Unspecified), item.Preview.CreationTime);
        }

        [Fact]
        public void Chain_DateTimeSetter_then_TimeShifter_composes_on_last_access()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: s_Base);
            var setDate = new DateTimeSetterFilter(
                Options: new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastAccess,
                    SetDate: true,
                    Date: new DateOnly(2019, 1, 1),
                    SetTime: false,
                    Time: new TimeOnly(0, 0, 0)
                )
            );
            var shift = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastAccess,
                    Amount: 3,
                    Unit: TimeShiftUnit.Days
                )
            );
            var chain = FilterChain.CreateAllEnabled([setDate, shift]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal(new DateTime(2019, 1, 4, 14, 5, 30, DateTimeKind.Unspecified), item.Preview.LastAccessTime);
        }

        [Fact]
        public void Zero_amount_is_no_op()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastWrite,
                    Amount: 0,
                    Unit: TimeShiftUnit.Years
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(s_Base, item.Preview.LastWriteTime);
        }

        [Fact]
        public void Shift_past_product_max_clamps_date_and_keeps_time()
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastWrite,
                    Amount: 100,
                    Unit: TimeShiftUnit.Years
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(
                FileTimestampDateLimits.Max.ToDateTime(TimeOnly.FromDateTime(s_Base), s_Base.Kind),
                item.Preview.LastWriteTime
            );
        }

        [Fact]
        public void Shift_before_product_min_clamps_date_and_keeps_time()
        {
            var nearMin = FileTimestampDateLimits.Min.ToDateTime(new TimeOnly(8, 15, 0), DateTimeKind.Unspecified);
            var item = FilterTestHelpers.CreateRenameItem(creationTime: nearMin);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.Creation,
                    Amount: -1,
                    Unit: TimeShiftUnit.Days
                )
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(nearMin, item.Preview.CreationTime);
        }

        [Theory]
        [InlineData(TimeShiftUnit.Days, 10_000_000)]
        [InlineData(TimeShiftUnit.Months, 10_000_000)]
        [InlineData(TimeShiftUnit.Years, 10_000_000)]
        public void Legal_spinner_max_amount_clamps_without_throwing(TimeShiftUnit unit, int amount)
        {
            var item = FilterTestHelpers.CreateRenameItem(lastWriteTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(TimestampField: TimestampField.LastWrite, Amount: amount, Unit: unit)
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(
                FileTimestampDateLimits.Max.ToDateTime(TimeOnly.FromDateTime(s_Base), s_Base.Kind),
                item.Preview.LastWriteTime
            );
        }

        [Theory]
        [InlineData(TimeShiftUnit.Days, -10_000_000)]
        [InlineData(TimeShiftUnit.Months, -10_000_000)]
        [InlineData(TimeShiftUnit.Years, -10_000_000)]
        public void Legal_spinner_min_amount_clamps_without_throwing(TimeShiftUnit unit, int amount)
        {
            var item = FilterTestHelpers.CreateRenameItem(lastAccessTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(TimestampField: TimestampField.LastAccess, Amount: amount, Unit: unit)
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(
                FileTimestampDateLimits.Min.ToDateTime(TimeOnly.FromDateTime(s_Base), s_Base.Kind),
                item.Preview.LastAccessTime
            );
        }
    }
}

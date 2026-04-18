using Mfr.Filters.Attributes;
using Mfr.Models;

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
            var item = FilterTestHelpers.CreateRenameItem(
                lastWriteTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastWrite,
                    Amount: 1,
                    Unit: TimeShiftUnit.Days));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 16, 14, 5, 30, DateTimeKind.Unspecified), item.Preview.LastWriteTime);
            Assert.Equal(item.Original.CreationTime, item.Preview.CreationTime);
            Assert.Equal(item.Original.LastAccessTime, item.Preview.LastAccessTime);
        }

        [Fact]
        public void TimeShifter_Creation_negative_hours()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base);
            var filter = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.Creation,
                    Amount: -2,
                    Unit: TimeShiftUnit.Hours));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 15, 12, 5, 30, DateTimeKind.Unspecified), item.Preview.CreationTime);
        }

        [Fact]
        public void Chain_DateSetter_then_TimeShifter_composes_on_last_access()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                lastAccessTime: s_Base);
            var setDate = new DateSetterFilter(
                Options: new DateSetterOptions(
                    TimestampField: TimestampField.LastAccess,
                    Date: new DateOnly(2019, 1, 1)));
            var shift = new TimeShifterFilter(
                Options: new TimeShifterOptions(
                    TimestampField: TimestampField.LastAccess,
                    Amount: 3,
                    Unit: TimeShiftUnit.Days));
            var chain = FilterChain.CreateAllEnabled([setDate, shift]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal(new DateTime(2019, 1, 4, 14, 5, 30, DateTimeKind.Unspecified), item.Preview.LastAccessTime);
        }
    }
}

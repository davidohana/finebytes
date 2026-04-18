using Mfr.Filters.Attributes;
using Mfr.Models;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models.Filters.Attributes
{
    /// <summary>
    /// Tests for <see cref="DateSetterFilter"/> and <see cref="TimeSetterFilter"/>.
    /// </summary>
    public sealed class DateTimeSetterFilterTests
    {
        private static readonly DateTime s_Base = new(2024, 3, 15, 14, 5, 30, DateTimeKind.Unspecified);

        [Fact]
        public void DateSetter_LastWriteDate_preserves_time_of_day()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                lastWriteTime: s_Base);
            var filter = new DateSetterFilter(
                Target: new LastWriteDateTarget(),
                Options: new DateSetterOptions(Date: new DateOnly(2020, 12, 25)));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2020, 12, 25, 14, 5, 30, DateTimeKind.Unspecified), item.Preview.LastWriteTime);
            Assert.Equal(item.Original.CreationTime, item.Preview.CreationTime);
        }

        [Fact]
        public void TimeSetter_CreationDate_preserves_calendar_date()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                creationTime: s_Base);
            var filter = new TimeSetterFilter(
                Target: new CreationDateTarget(),
                Options: new TimeSetterOptions(Time: new TimeOnly(9, 0, 15)));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal(new DateTime(2024, 3, 15, 9, 0, 15, DateTimeKind.Unspecified), item.Preview.CreationTime);
        }

        [Fact]
        public void Chain_DateSetter_then_TimeSetter_composes_on_last_access()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                lastAccessTime: s_Base);
            var setDate = new DateSetterFilter(
                Target: new LastAccessDateTarget(),
                Options: new DateSetterOptions(Date: new DateOnly(2019, 1, 1)));
            var setTime = new TimeSetterFilter(
                Target: new LastAccessDateTarget(),
                Options: new TimeSetterOptions(Time: new TimeOnly(23, 59, 1)));
            var chain = FilterChain.CreateAllEnabled([setDate, setTime]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal(new DateTime(2019, 1, 1, 23, 59, 1, DateTimeKind.Unspecified), item.Preview.LastAccessTime);
        }

        [Fact]
        public void Setup_wrong_target_throws()
        {
            var filter = new DateSetterFilter(
                Target: new AttributesTarget(),
                Options: new DateSetterOptions(Date: new DateOnly(2024, 1, 1)));
            var ex = Assert.Throws<InvalidOperationException>(() => filter.Setup());
            Assert.Contains("CreationDate", ex.Message);
        }
    }
}

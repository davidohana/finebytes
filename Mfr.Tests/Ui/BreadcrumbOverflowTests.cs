using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests address-bar overflow: keep the current folder, hide ancestors from the left.
    /// </summary>
    public sealed class BreadcrumbOverflowTests
    {
        /// <summary>
        /// Verifies an empty trail has no overflow.
        /// </summary>
        [Fact]
        public void Empty_Trail_Starts_At_Zero()
        {
            Assert.Equal(0, BreadcrumbOverflow.PickVisibleStart([], 100, BreadcrumbOverflow.ButtonWidth));
        }

        /// <summary>
        /// Verifies a trail that fits keeps every segment.
        /// </summary>
        [Fact]
        public void Fitting_Trail_Shows_All_Segments()
        {
            Assert.Equal(0, BreadcrumbOverflow.PickVisibleStart([20, 20, 20], 80, 22));
        }

        /// <summary>
        /// Verifies unconstrained width keeps every segment.
        /// </summary>
        [Fact]
        public void Infinite_Width_Shows_All_Segments()
        {
            Assert.Equal(0, BreadcrumbOverflow.PickVisibleStart([80, 80, 80], double.PositiveInfinity, 22));
        }

        /// <summary>
        /// Verifies a single wide folder stays visible instead of overflowing.
        /// </summary>
        [Fact]
        public void Single_Wide_Segment_Does_Not_Overflow()
        {
            Assert.Equal(0, BreadcrumbOverflow.PickVisibleStart([200], 50, 22));
        }

        /// <summary>
        /// Verifies ancestors collapse from the left until the current folder fits.
        /// </summary>
        [Fact]
        public void Narrow_Bar_Keeps_Trailing_Segments()
        {
            Assert.Equal(1, BreadcrumbOverflow.PickVisibleStart([30, 30, 30], 89, 22));
        }

        /// <summary>
        /// Verifies only the current folder remains when ancestors plus overflow cannot fit.
        /// </summary>
        [Fact]
        public void Very_Narrow_Bar_Keeps_Only_Current_Folder()
        {
            Assert.Equal(2, BreadcrumbOverflow.PickVisibleStart([30, 30, 30], 80, 22));
        }

        /// <summary>
        /// Verifies reserving the overflow button can hide one more ancestor.
        /// </summary>
        [Fact]
        public void Overflow_Button_Width_Can_Hide_An_Extra_Segment()
        {
            Assert.Equal(1, BreadcrumbOverflow.PickVisibleStart([30, 30, 30], 70, 0));
            Assert.Equal(2, BreadcrumbOverflow.PickVisibleStart([30, 30, 30], 70, 22));
        }
    }
}

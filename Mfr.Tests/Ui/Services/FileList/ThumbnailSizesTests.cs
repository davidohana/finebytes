using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.Tests.Ui.Thumbnails
{
    /// <summary>
    /// Tests discrete thumbnail size snapping used by the File List.
    /// </summary>
    public sealed class ThumbnailSizesTests
    {
        /// <summary>
        /// Verifies allowed steps are 48 through 256 and default to Medium.
        /// </summary>
        [Fact]
        public void Steps_Are_Ascending_And_Default_Is_Medium()
        {
            Assert.Equal(
                [
                    ThumbnailSizes.ExtraSmall,
                    ThumbnailSizes.Small,
                    ThumbnailSizes.Medium,
                    ThumbnailSizes.Large,
                    ThumbnailSizes.ExtraLarge,
                    ThumbnailSizes.Huge,
                ],
                ThumbnailSizes.Steps
            );
            Assert.Equal(96, ThumbnailSizes.Default);
            Assert.Equal(ThumbnailSizes.Medium, ThumbnailSizes.Default);
            Assert.Equal(ThumbnailSizes.Huge, App.Ui.Services.FileList.ImageThumbnailLoader.DecodeWidth);
        }

        /// <summary>
        /// Verifies values snap to the nearest step, rounding up on a tie.
        /// </summary>
        [Theory]
        [InlineData(0, 48)]
        [InlineData(48, 48)]
        [InlineData(55, 48)]
        [InlineData(56, 64)]
        [InlineData(80, 96)]
        [InlineData(100, 96)]
        [InlineData(160, 192)]
        [InlineData(224, 256)]
        [InlineData(1000, 256)]
        public void Clamp_Snaps_To_Nearest_Step(int size, int expected)
        {
            Assert.Equal(expected, ThumbnailSizes.Clamp(size));
        }

        /// <summary>
        /// Verifies zoom-in walks the step list and stops at Huge.
        /// </summary>
        [Fact]
        public void LargerThan_Walks_Steps_Then_Stops()
        {
            Assert.Equal(64, ThumbnailSizes.LargerThan(48));
            Assert.Equal(96, ThumbnailSizes.LargerThan(64));
            Assert.Equal(128, ThumbnailSizes.LargerThan(96));
            Assert.Equal(192, ThumbnailSizes.LargerThan(128));
            Assert.Equal(256, ThumbnailSizes.LargerThan(192));
            Assert.Equal(256, ThumbnailSizes.LargerThan(256));
            Assert.Equal(128, ThumbnailSizes.LargerThan(100));
        }

        /// <summary>
        /// Verifies zoom-out walks the step list and stops at Extra Small.
        /// </summary>
        [Fact]
        public void SmallerThan_Walks_Steps_Then_Stops()
        {
            Assert.Equal(192, ThumbnailSizes.SmallerThan(256));
            Assert.Equal(128, ThumbnailSizes.SmallerThan(192));
            Assert.Equal(96, ThumbnailSizes.SmallerThan(128));
            Assert.Equal(64, ThumbnailSizes.SmallerThan(96));
            Assert.Equal(48, ThumbnailSizes.SmallerThan(64));
            Assert.Equal(48, ThumbnailSizes.SmallerThan(48));
            Assert.Equal(64, ThumbnailSizes.SmallerThan(100));
        }
    }
}

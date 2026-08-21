using Mfr.App.Ui.Services;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests for <see cref="WindowBounds"/>.
    /// </summary>
    public sealed class WindowBoundsTests
    {
        [Fact]
        public void TryNormalize_clamps_below_minimum_size()
        {
            var ok = WindowBounds.TryNormalize(
                10,
                20,
                100,
                50,
                workAreas: [],
                minWidth: 800,
                minHeight: 500,
                out var x,
                out var y,
                out var w,
                out var h);

            Assert.True(ok);
            Assert.Equal(10, x);
            Assert.Equal(20, y);
            Assert.Equal(800, w);
            Assert.Equal(500, h);
        }

        [Fact]
        public void TryNormalize_rejects_off_screen_when_work_areas_present()
        {
            var workAreas = new[] { new WindowBounds.ScreenRect(0, 0, 1920, 1080) };
            var ok = WindowBounds.TryNormalize(
                5000,
                5000,
                1100,
                720,
                workAreas,
                minWidth: 800,
                minHeight: 500,
                out _,
                out _,
                out _,
                out _);

            Assert.False(ok);
        }

        [Fact]
        public void TryNormalize_accepts_intersecting_rect()
        {
            var workAreas = new[] { new WindowBounds.ScreenRect(0, 0, 1920, 1080) };
            var ok = WindowBounds.TryNormalize(
                100,
                80,
                1100,
                720,
                workAreas,
                minWidth: 800,
                minHeight: 500,
                out var x,
                out var y,
                out var w,
                out var h);

            Assert.True(ok);
            Assert.Equal(100, x);
            Assert.Equal(80, y);
            Assert.Equal(1100, w);
            Assert.Equal(720, h);
        }

        [Fact]
        public void TryNormalize_rejects_non_positive_size()
        {
            var ok = WindowBounds.TryNormalize(
                0,
                0,
                0,
                720,
                workAreas: [],
                minWidth: 800,
                minHeight: 500,
                out _,
                out _,
                out _,
                out _);

            Assert.False(ok);
        }
    }
}

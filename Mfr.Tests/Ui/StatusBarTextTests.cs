using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests <see cref="StatusBarText"/> severity helpers.
    /// </summary>
    public sealed class StatusBarTextTests
    {
        /// <summary>
        /// Verifies Neutral leaves the run uncolored.
        /// </summary>
        [Fact]
        public void Neutral_Has_No_Foreground_Key()
        {
            var display = StatusBarText.Neutral("ok");
            Assert.Equal("ok", display.ToPlainText());
            Assert.Null(Assert.Single(display.Runs).ForegroundResourceKey);
        }

        /// <summary>
        /// Verifies Warning and Error set their brush resource keys.
        /// </summary>
        [Fact]
        public void Warning_And_Error_Set_Foreground_Keys()
        {
            Assert.Equal(
                StatusBarText.WarningForegroundResourceKey,
                Assert.Single(StatusBarText.Warning("warn").Runs).ForegroundResourceKey
            );
            Assert.Equal(
                StatusBarText.ErrorForegroundResourceKey,
                Assert.Single(StatusBarText.Error("err").Runs).ForegroundResourceKey
            );
        }

        /// <summary>
        /// Verifies Combine concatenates runs and skips empty parts.
        /// </summary>
        [Fact]
        public void Combine_Joins_NonEmpty_Parts()
        {
            var display = StatusBarText.Combine(
                StatusBarText.Neutral("a "),
                StyledTextDisplay.Empty,
                StatusBarText.Error("b")
            );

            Assert.Equal("a b", display.ToPlainText());
            Assert.Equal(2, display.Runs.Count);
            Assert.Null(display.Runs[0].ForegroundResourceKey);
            Assert.Equal(StatusBarText.ErrorForegroundResourceKey, display.Runs[1].ForegroundResourceKey);
        }
    }
}

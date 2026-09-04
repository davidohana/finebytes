using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.Tests.Ui.Controls
{
    /// <summary>
    /// Headless layout tests for <see cref="CompactNumericUpDown"/>.
    /// </summary>
    public sealed class CompactNumericUpDownTests
    {
        /// <summary>
        /// Verifies a standalone spinner shows its value with stacked compact arrows.
        /// </summary>
        [AvaloniaFact]
        public void Shows_value_with_stacked_compact_arrows()
        {
            var spinner = new CompactNumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                FormatString = "0",
                Value = 5,
            };
            var window = new Window
            {
                Width = 200,
                Height = 80,
                Content = spinner,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                CompactNumericUpDownAssert.ShowsStackedValue(spinner, expectedText: "5");
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Verifies the decrease arrow stays visually idle at the minimum value.
        /// </summary>
        [AvaloniaFact]
        public void Decrease_button_has_no_disabled_fill_at_minimum()
        {
            var spinner = new CompactNumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                FormatString = "0",
                Value = 1,
            };
            var window = new Window
            {
                Width = 200,
                Height = 80,
                Content = spinner,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                CompactNumericUpDownAssert.ShowsStackedValue(spinner, expectedText: "1");
                CompactNumericUpDownAssert.DecreaseButtonHasTransparentFill(spinner);
            }
            finally
            {
                window.Close();
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Data;
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
            var (window, spinner) = _ShowSpinner(value: 5);

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
            var (window, spinner) = _ShowSpinner(value: 1);

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

        /// <summary>
        /// Verifies values below Minimum clip instead of raising a validation exception.
        /// </summary>
        [AvaloniaFact]
        public void Clips_value_below_minimum_without_validation_error()
        {
            var (window, spinner) = _ShowSpinner(value: 1, maximum: 200);

            try
            {
                Assert.True(spinner.ClipValueToMinMax);
                spinner.Value = 0;
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(1m, spinner.Value);
                Assert.Empty(DataValidationErrors.GetErrors(spinner) ?? []);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Verifies clearing the value (Delete → null) coerces to Minimum and keeps a non-nullable binding healthy.
        /// </summary>
        [AvaloniaFact]
        public void Empty_value_coerces_to_minimum_for_non_nullable_binding()
        {
            var vm = new SpinnerHost { Count = 5 };
            var (window, spinner) = _ShowSpinner(
                value: 5,
                maximum: 200,
                configure: control =>
                {
                    control.DataContext = vm;
                    control[!NumericUpDown.ValueProperty] = new Binding(nameof(SpinnerHost.Count))
                    {
                        Mode = BindingMode.TwoWay,
                    };
                }
            );

            try
            {
                Assert.Equal(5m, spinner.Value);

                spinner.Value = null;
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.Equal(1m, spinner.Value);
                Assert.Equal(1m, vm.Count);
                Assert.Empty(DataValidationErrors.GetErrors(spinner) ?? []);
            }
            finally
            {
                window.Close();
            }
        }

        private static (Window Window, CompactNumericUpDown Spinner) _ShowSpinner(
            decimal value,
            decimal maximum = 9999,
            Action<CompactNumericUpDown>? configure = null
        )
        {
            var spinner = new CompactNumericUpDown
            {
                Minimum = 1,
                Maximum = maximum,
                FormatString = "0",
                Value = value,
            };
            configure?.Invoke(spinner);

            var window = new Window
            {
                Width = 200,
                Height = 80,
                Content = spinner,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return (window, spinner);
        }

        private sealed class SpinnerHost
        {
            public decimal Count { get; set; }
        }
    }
}

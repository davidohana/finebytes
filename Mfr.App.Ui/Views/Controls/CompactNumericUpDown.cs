using Avalonia.Controls;
using Avalonia.Layout;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Numeric stepper with a compact stacked up/down spinner.
    /// <para>
    /// Fluent's default <see cref="NumericUpDown"/> places large side-by-side arrows that
    /// hide the value in compact fields. Use this type for Filter Options and filter editors.
    /// </para>
    /// </summary>
    public sealed class CompactNumericUpDown : NumericUpDown
    {
        static CompactNumericUpDown()
        {
            WidthProperty.OverrideDefaultValue<CompactNumericUpDown>(80);
            MinWidthProperty.OverrideDefaultValue<CompactNumericUpDown>(80);
            HeightProperty.OverrideDefaultValue<CompactNumericUpDown>(26);
            MinHeightProperty.OverrideDefaultValue<CompactNumericUpDown>(26);
            HorizontalAlignmentProperty.OverrideDefaultValue<CompactNumericUpDown>(HorizontalAlignment.Left);
            VerticalAlignmentProperty.OverrideDefaultValue<CompactNumericUpDown>(VerticalAlignment.Center);
            // Out-of-range typed values (e.g. 0 with Minimum=1) throw unless clipped.
            ClipValueToMinMaxProperty.OverrideDefaultValue<CompactNumericUpDown>(true);
        }

        /// <summary>
        /// Initializes a compact stacked-spinner numeric field.
        /// </summary>
        public CompactNumericUpDown()
        {
            Classes.Add("compact-numeric");
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(NumericUpDown);

        /// <inheritdoc />
        /// <remarks>
        /// Empty text (Delete/Backspace) sets <see cref="NumericUpDown.Value"/> to null. Our editors bind
        /// non-nullable <c>decimal</c>, which surfaces as <see cref="InvalidOperationException"/> validation.
        /// Coerce empty to <see cref="NumericUpDown.Minimum"/> so all compact spinners stay bindable.
        /// </remarks>
        protected override decimal? OnCoerceValue(decimal? baseValue)
        {
            if (baseValue is null)
            {
                return Minimum;
            }

            if (ClipValueToMinMax)
            {
                return Math.Clamp(baseValue.Value, Minimum, Maximum);
            }

            return base.OnCoerceValue(baseValue);
        }
    }
}

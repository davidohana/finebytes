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
            // Typed values outside Minimum/Maximum throw unless clipped (see OnCoerceValue).
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

        /// <summary>
        /// Keeps <see cref="NumericUpDown.Value"/> bindable to non-nullable <c>decimal</c>.
        /// <para>
        /// Empty text (Delete/Backspace) would set Value to null and fail the binding; coerce to
        /// <see cref="NumericUpDown.Minimum"/>. When <see cref="NumericUpDown.ClipValueToMinMax"/> is on,
        /// also clamp out-of-range values (Avalonia only clips on text parse, not every Value set).
        /// </para>
        /// </summary>
        /// <param name="baseValue">Candidate value before store.</param>
        /// <returns>Coerced value.</returns>
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

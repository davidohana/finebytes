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
    }
}

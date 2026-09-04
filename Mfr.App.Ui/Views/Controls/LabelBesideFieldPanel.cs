using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Lays out a wrapping label beside a field, keeping the field snug to the text and vertically centered.
    /// <para>
    /// Expects two children: label then field. The label is measured with the width left after the field so it
    /// wraps instead of pushing the field off-row or stretching to the far edge.
    /// </para>
    /// </summary>
    public sealed class LabelBesideFieldPanel : Panel
    {
        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<
            LabelBesideFieldPanel,
            double
        >(nameof(Spacing), defaultValue: 4);

        static LabelBesideFieldPanel()
        {
            AffectsMeasure<LabelBesideFieldPanel>(SpacingProperty);
        }

        /// <summary>
        /// Gets or sets the gap between the label and the field.
        /// </summary>
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            if (!_TryGetLabelAndField(out var label, out var field))
            {
                return default;
            }

            field.Measure(availableSize);
            var spacing = Spacing;
            var labelMaxWidth = double.IsInfinity(availableSize.Width)
                ? double.PositiveInfinity
                : Math.Max(0, availableSize.Width - field.DesiredSize.Width - spacing);
            label.Measure(new Size(labelMaxWidth, availableSize.Height));

            var width = label.DesiredSize.Width + spacing + field.DesiredSize.Width;
            if (!double.IsInfinity(availableSize.Width))
            {
                width = Math.Min(width, availableSize.Width);
            }

            var height = Math.Max(label.DesiredSize.Height, field.DesiredSize.Height);
            return new Size(width, height);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!_TryGetLabelAndField(out var label, out var field))
            {
                return finalSize;
            }

            var spacing = Spacing;
            var fieldWidth = field.DesiredSize.Width;
            var fieldHeight = field.DesiredSize.Height;
            var labelMaxWidth = Math.Max(0, finalSize.Width - fieldWidth - spacing);
            var labelWidth = Math.Min(label.DesiredSize.Width, labelMaxWidth);
            var labelHeight = label.DesiredSize.Height;

            var labelY = _CenterOffset(finalSize.Height, labelHeight);
            var fieldY = _CenterOffset(finalSize.Height, fieldHeight);
            label.Arrange(new Rect(0, labelY, labelWidth, labelHeight));
            field.Arrange(new Rect(labelWidth + spacing, fieldY, fieldWidth, fieldHeight));
            return finalSize;
        }

        private bool _TryGetLabelAndField(out Control label, out Control field)
        {
            if (Children is [Control first, Control second, ..])
            {
                label = first;
                field = second;
                return true;
            }

            label = null!;
            field = null!;
            return false;
        }

        private static double _CenterOffset(double container, double child)
        {
            return Math.Max(0, (container - child) / 2);
        }
    }
}

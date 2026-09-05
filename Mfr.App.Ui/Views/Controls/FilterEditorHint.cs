using Avalonia;
using Avalonia.Controls.Primitives;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Compact bulb + text tip shown under filter-editor options for important discoverability notes.
    /// </summary>
    public sealed class FilterEditorHint : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<
            FilterEditorHint,
            string?
        >(nameof(Text));

        /// <summary>
        /// Gets or sets the hint body shown beside the bulb.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FilterEditorHint);
    }
}

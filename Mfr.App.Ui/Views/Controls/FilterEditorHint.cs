using Avalonia;
using Avalonia.Controls.Primitives;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Compact bulb + text tip shown under filter-editor options for important discoverability notes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set <see cref="Text"/> for plain tip copy, and/or <see cref="LinkText"/> with
    /// <see cref="NavigateUri"/> for a clickable public-docs link beside the bulb.
    /// </para>
    /// </remarks>
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
        /// Defines the <see cref="LinkText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> LinkTextProperty = AvaloniaProperty.Register<
            FilterEditorHint,
            string?
        >(nameof(LinkText));

        /// <summary>
        /// Defines the <see cref="NavigateUri"/> property.
        /// </summary>
        public static readonly StyledProperty<Uri?> NavigateUriProperty = AvaloniaProperty.Register<
            FilterEditorHint,
            Uri?
        >(nameof(NavigateUri));

        /// <summary>
        /// Gets or sets the plain hint body shown beside the bulb.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the hyperlink label shown beside the bulb when <see cref="NavigateUri"/> is set.
        /// </summary>
        public string? LinkText
        {
            get => GetValue(LinkTextProperty);
            set => SetValue(LinkTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the public URI opened when the hint link is clicked.
        /// </summary>
        public Uri? NavigateUri
        {
            get => GetValue(NavigateUriProperty);
            set => SetValue(NavigateUriProperty, value);
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FilterEditorHint);
    }
}

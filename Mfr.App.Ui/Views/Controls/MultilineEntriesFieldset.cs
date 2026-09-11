using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Shared Entries fieldset chrome for multiline list editors (tip, optional label, editor, hint).
    /// </summary>
    /// <remarks>
    /// Put the multiline editor (plain <c>TextBox</c> or FormatEditor) in
    /// <see cref="Avalonia.Controls.ContentControl.Content"/>; set <see cref="HeaderedContentControl.Header"/>,
    /// optional <see cref="Label"/>, <see cref="Hint"/>, and <see cref="Tip"/> (typically a
    /// <see cref="RichToolTip"/>).
    /// </remarks>
    public sealed class MultilineEntriesFieldset : HeaderedContentControl
    {
        static MultilineEntriesFieldset()
        {
            HorizontalContentAlignmentProperty.OverrideDefaultValue<MultilineEntriesFieldset>(
                HorizontalAlignment.Stretch
            );
        }

        /// <summary>
        /// Defines the <see cref="Label"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<
            MultilineEntriesFieldset,
            string?
        >(nameof(Label));

        /// <summary>
        /// Defines the <see cref="Hint"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<
            MultilineEntriesFieldset,
            string?
        >(nameof(Hint));

        /// <summary>
        /// Defines the <see cref="Tip"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> TipProperty = AvaloniaProperty.Register<
            MultilineEntriesFieldset,
            object?
        >(nameof(Tip));

        /// <summary>
        /// Gets or sets the optional label above the editor (for example <c>Entries:</c>).
        /// </summary>
        public string? Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets or sets the bulb hint under the editor.
        /// </summary>
        public string? Hint
        {
            get => GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <summary>
        /// Gets or sets the rich tooltip content shown over the entries body.
        /// </summary>
        public object? Tip
        {
            get => GetValue(TipProperty);
            set => SetValue(TipProperty, value);
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(MultilineEntriesFieldset);
    }
}

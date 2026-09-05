using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Radio button with a tight glyph-to-label gap for compact dialogs and filter editors.
    /// <para>
    /// Fluent's default template reserves a 20px glyph column and 8px padding; this type
    /// keeps the Fluent visuals but packs the label closer to the circle (4px padding).
    /// </para>
    /// </summary>
    public sealed class CompactRadioButton : RadioButton
    {
        private const double _GlyphSlot = 12;
        private const double _MinHeightValue = 22;

        static CompactRadioButton()
        {
            PaddingProperty.OverrideDefaultValue<CompactRadioButton>(new Thickness(4, 0, 0, 0));
            MinHeightProperty.OverrideDefaultValue<CompactRadioButton>(_MinHeightValue);
            VerticalAlignmentProperty.OverrideDefaultValue<CompactRadioButton>(VerticalAlignment.Center);
        }

        /// <summary>
        /// Initializes a compact radio button.
        /// </summary>
        public CompactRadioButton()
        {
            Classes.Add("compact-radio");
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(RadioButton);

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _TightenGlyphColumn(e.NameScope.Find<Border>("RootBorder"));
        }

        private static void _TightenGlyphColumn(Border? rootBorder)
        {
            if (rootBorder?.Child is not Grid layout)
            {
                return;
            }

            layout.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            if (layout.Children is not [Grid glyphHost, ..])
            {
                return;
            }

            glyphHost.Width = _GlyphSlot;
            glyphHost.Height = _MinHeightValue;
        }
    }
}

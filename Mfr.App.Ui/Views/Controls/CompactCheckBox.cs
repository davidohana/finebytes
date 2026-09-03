using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Check box with a tight glyph-to-label gap for compact dialogs and filter editors.
    /// <para>
    /// Fluent's default template reserves a 20px glyph column and 8px padding; this type
    /// keeps the Fluent visuals but packs the label closer to the box.
    /// </para>
    /// </summary>
    public sealed class CompactCheckBox : CheckBox
    {
        private const double _GlyphSlot = 12;
        private const double _MinHeightValue = 22;

        static CompactCheckBox()
        {
            PaddingProperty.OverrideDefaultValue<CompactCheckBox>(new Thickness(6, 0, 0, 0));
            MinHeightProperty.OverrideDefaultValue<CompactCheckBox>(_MinHeightValue);
            VerticalAlignmentProperty.OverrideDefaultValue<CompactCheckBox>(VerticalAlignment.Center);
        }

        /// <summary>
        /// Initializes a compact check box.
        /// </summary>
        public CompactCheckBox()
        {
            Classes.Add("compact-check");
        }

        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(CheckBox);

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _TightenGlyphColumn(e.NameScope.Find<Grid>("RootGrid"));
        }

        private static void _TightenGlyphColumn(Grid? rootGrid)
        {
            if (rootGrid is null)
            {
                return;
            }

            rootGrid.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            var glyphHost = rootGrid.Children.OfType<Grid>().FirstOrDefault();
            if (glyphHost is null)
            {
                return;
            }

            glyphHost.Width = _GlyphSlot;
            glyphHost.Height = _MinHeightValue;
        }
    }
}

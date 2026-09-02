using Avalonia.Controls.Primitives;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Classic fieldset with the header sitting on the top border line (MFR7 / WinForms GroupBox style).
    /// </summary>
    public class FieldsetGroup : HeaderedContentControl
    {
        /// <inheritdoc />
        protected override Type StyleKeyOverride => typeof(FieldsetGroup);
    }
}

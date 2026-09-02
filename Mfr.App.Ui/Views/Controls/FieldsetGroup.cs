using Avalonia.Controls.Primitives;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Classic fieldset with the header sitting on the top border line (MFR7 / WinForms GroupBox style).
    /// </summary>
    public class FieldsetGroup : HeaderedContentControl
    {
        /// <summary>
        /// Initializes a fieldset group with the shared <c>fieldset-group</c> style class.
        /// </summary>
        public FieldsetGroup()
        {
            Classes.Add("fieldset-group");
        }
    }
}

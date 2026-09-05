using Avalonia.Controls;
using Mfr.Filters.Attributes;

namespace Mfr.App.Ui.Views.FilterEditors.Attributes
{
    /// <summary>
    /// Shared option editor for <see cref="DateSetterFilter"/> and <see cref="TimeSetterFilter"/>.
    /// </summary>
    public partial class DateTimeSetterFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the date/time setter option editor.
        /// </summary>
        public DateTimeSetterFilterEditorView()
        {
            InitializeComponent();
        }
    }
}

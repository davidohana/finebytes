using Avalonia.Controls;

namespace Mfr.App.Ui.Views.FilterEditors.Space
{
    /// <summary>
    /// Shared option editor for <see cref="Filters.Space.SpaceAfterFilter"/> and
    /// <see cref="Filters.Space.SpaceAroundFilter"/>.
    /// </summary>
    public partial class SpaceTriggerFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the space-trigger option editor.
        /// </summary>
        public SpaceTriggerFilterEditorView()
        {
            InitializeComponent();
        }
    }
}

using Avalonia.Controls;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.Views.FilterEditors.Case
{
    /// <summary>
    /// Shared option editor for <see cref="CapitalizeAfterFilter"/> and <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    public partial class CharacterListFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the character-list option editor.
        /// </summary>
        public CharacterListFilterEditorView()
        {
            InitializeComponent();
        }
    }
}

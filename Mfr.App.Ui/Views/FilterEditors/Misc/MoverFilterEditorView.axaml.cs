using Avalonia.Controls;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;

namespace Mfr.App.Ui.Views.FilterEditors.Misc
{
    /// <summary>
    /// Option editor for <see cref="Filters.Misc.MoverFilter"/>.
    /// </summary>
    public partial class MoverFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the Mover option editor.
        /// </summary>
        public MoverFilterEditorView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is MoverFilterEditorViewModel vm)
            {
                vm.PickRootFolderAsync = (currentRoot, cancellationToken) =>
                    FolderPicker.PickFolderAsync(
                        this,
                        suggestedStartPath: currentRoot,
                        title: "Select root folder",
                        cancellationToken: cancellationToken
                    );
            }
        }
    }
}

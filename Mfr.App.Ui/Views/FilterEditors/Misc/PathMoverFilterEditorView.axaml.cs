using Avalonia.Controls;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;

namespace Mfr.App.Ui.Views.FilterEditors.Misc
{
    /// <summary>
    /// Option editor for <see cref="Filters.Misc.PathMoverFilter"/>.
    /// </summary>
    public partial class PathMoverFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the Path Mover option editor.
        /// </summary>
        public PathMoverFilterEditorView()
        {
            InitializeComponent();
            FilterEditorFileDrop.AttachFolderDrop(RootFolderDropTarget, _ApplyDroppedRootFolder);
        }

        /// <inheritdoc />
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is PathMoverFilterEditorViewModel vm)
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

        private void _ApplyDroppedRootFolder(string folderPath)
        {
            if (DataContext is PathMoverFilterEditorViewModel vm)
            {
                vm.RootFolder = folderPath;
            }
        }
    }
}

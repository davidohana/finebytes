using Avalonia.Controls;
using Avalonia.Platform.Storage;
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
                vm.PickRootFolderAsync = _PickRootFolderAsync;
            }
        }

        private async Task<string?> _PickRootFolderAsync(string? currentRoot, CancellationToken cancellationToken)
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null)
            {
                return null;
            }

            IStorageFolder? startLocation = null;
            if (!string.IsNullOrWhiteSpace(currentRoot))
            {
                startLocation = await storage.TryGetFolderFromPathAsync(currentRoot).ConfigureAwait(true);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var folders = await storage
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title = "Select root folder",
                        AllowMultiple = false,
                        SuggestedStartLocation = startLocation,
                    }
                )
                .ConfigureAwait(true);

            if (folders.Count == 0)
            {
                return null;
            }

            return folders[0].TryGetLocalPath();
        }
    }
}

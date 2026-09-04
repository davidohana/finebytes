using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.Filters.Case;

namespace Mfr.App.Ui.Views.FilterEditors.Case
{
    /// <summary>
    /// Option editor for <see cref="CasingListFilter"/>.
    /// </summary>
    public partial class CasingListFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the Casing List option editor.
        /// </summary>
        public CasingListFilterEditorView()
        {
            InitializeComponent();
        }

        private async void _BrowseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CasingListFilterEditorViewModel viewModel)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is not { } storage)
            {
                return;
            }

            var files = await storage.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select Casing List File",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                        new FilePickerFileType("All Files") { Patterns = ["*.*"] },
                    ],
                }
            );

            if (files.Count == 0)
            {
                return;
            }

            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            viewModel.FilePath = path;
        }
    }
}

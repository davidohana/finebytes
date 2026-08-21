using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;
using Mfr.Utils;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Modal dialog for an unexpected process fault.
    /// </summary>
    public partial class CrashDialog : Window
    {
        /// <summary>
        /// Initializes the crash dialog (designer / default).
        /// </summary>
        public CrashDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the crash dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Dialog content.</param>
        public CrashDialog(CrashDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private async void _OnCopyDetailsClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CrashDialogViewModel viewModel)
                return;

            var clipboard = Clipboard;
            if (clipboard is null)
                return;

            await clipboard.SetTextAsync(viewModel.Details);
        }

        private void _OnOpenLogFolderClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CrashDialogViewModel viewModel)
                return;

            if (viewModel.LogDirectoryPath.IsBlank() || !Directory.Exists(viewModel.LogDirectoryPath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = viewModel.LogDirectoryPath,
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                // Opening Explorer (or the platform equivalent) is best-effort.
            }
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

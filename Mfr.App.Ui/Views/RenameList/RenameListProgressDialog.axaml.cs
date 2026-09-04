using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal progress dialog shown while a long Rename List background operation is running.
    /// </summary>
    public partial class RenameListProgressDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public RenameListProgressDialog()
        {
            InitializeComponent();
            Closing += _OnClosing;
        }

        /// <summary>
        /// Initializes the dialog bound to progress state.
        /// </summary>
        /// <param name="viewModel">Progress counts and cancel command.</param>
        public RenameListProgressDialog(RenameListProgressViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private void _OnClosing(object? sender, WindowClosingEventArgs e)
        {
            // Match MFR7: ignore close while the worker is still running (Cancel requests stop instead).
            if (DataContext is RenameListProgressViewModel { IsBusy: true } viewModel)
            {
                e.Cancel = true;
                viewModel.CancelCommand.Execute(null);
            }
        }
    }
}

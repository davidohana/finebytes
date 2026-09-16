using Avalonia.Controls;
using Avalonia.Input;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.LogDialog;

namespace Mfr.App.Ui.Views.LogDialog
{
    /// <summary>
    /// Modal Rename Log dialog (list, details, Undo, Erase).
    /// </summary>
    public partial class RenameLogDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public RenameLogDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            DialogSession.Attach(this, DialogIds.RenameLog);
        }

        /// <summary>
        /// Initializes the dialog with list state.
        /// </summary>
        /// <param name="viewModel">Rename Log dialog state.</param>
        public RenameLogDialog(RenameLogDialogViewModel viewModel)
            : this()
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            DataContext = viewModel;
            viewModel.CloseRequested = accepted => Close(accepted);
            viewModel.ShowErrorAsync = async (title, message) =>
            {
                await new OkMessageDialog(title, message).ShowDialog(this).ConfigureAwait(true);
            };
        }

        private void _OnLogsListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
            {
                return;
            }

            if (DataContext is not RenameLogDialogViewModel viewModel)
            {
                return;
            }

            if (!viewModel.EraseCommand.CanExecute(null))
            {
                return;
            }

            viewModel.EraseCommand.Execute(null);
            e.Handled = true;
        }
    }
}

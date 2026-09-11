using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Presets;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Modal checklist for choosing curated sample presets to import.
    /// </summary>
    public partial class ImportSamplePresetsDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public ImportSamplePresetsDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with checklist state.
        /// </summary>
        /// <param name="viewModel">Sample checklist state.</param>
        public ImportSamplePresetsDialog(ImportSamplePresetsDialogViewModel viewModel)
            : this()
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            DataContext = viewModel;
        }

        private void _OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ImportSamplePresetsDialogViewModel viewModel)
            {
                viewModel.SelectAll();
            }
        }

        private void _OnSelectNoneClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ImportSamplePresetsDialogViewModel viewModel)
            {
                viewModel.SelectNone();
            }
        }

        private void _OnImportClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ImportSamplePresetsDialogViewModel { CanImport: true })
            {
                Close(true);
            }
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}

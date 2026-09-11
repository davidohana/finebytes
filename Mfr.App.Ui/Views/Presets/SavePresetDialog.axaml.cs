using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Presets;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Modal dialog for Save Preset (Update last-loaded or Save as new).
    /// <para>
    /// Closes with <see cref="SavePresetDialogMode.Update"/>, <see cref="SavePresetDialogMode.SaveAs"/>,
    /// or <see langword="null"/> for Cancel / Escape.
    /// </para>
    /// </summary>
    public partial class SavePresetDialog : Window
    {
        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public SavePresetDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft name, description, and columns checkbox.</param>
        public SavePresetDialog(SavePresetDialogViewModel viewModel)
            : this()
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            DataContext = viewModel;
        }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            NameBox.Focus();
            NameBox.SelectAll();
        }

        private void _OnUpdateClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SavePresetDialogViewModel { CanUpdateAction: false })
            {
                return;
            }

            Close(SavePresetDialogMode.Update);
        }

        private void _OnSaveAsNewClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SavePresetDialogViewModel { CanSaveAsAction: false })
            {
                return;
            }

            Close(SavePresetDialogMode.SaveAs);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}

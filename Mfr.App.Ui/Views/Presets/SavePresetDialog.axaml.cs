using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Presets;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Modal dialog for Save Preset As (name, description, optional Rename List columns).
    /// <para>Closes with <see langword="true"/> for OK and <see langword="false"/> for Cancel or Escape.</para>
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

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SavePresetDialogViewModel { CanConfirm: false })
            {
                return;
            }

            Close(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}

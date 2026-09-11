using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.Presets;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Modal dialog for Save Preset (name, description, optional Rename List columns).
    /// <para>Closes with <see langword="true"/> for Save and <see langword="false"/> for Cancel or Escape.</para>
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
            var editable = NameBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            if (editable is not null)
            {
                editable.Focus();
                editable.SelectAll();
                return;
            }

            NameBox.Focus();
        }

        private void _OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SavePresetDialogViewModel { CanSave: false })
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

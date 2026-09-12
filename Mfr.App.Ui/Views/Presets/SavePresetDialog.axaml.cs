using Avalonia.Controls;
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
            NameBox.SelectionChanged += _OnNameSuggestionSelected;
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
            ModalDialogTextFocus.FocusAndSelectAll(NameBox);
        }

        /// <summary>
        /// Prefills description/columns only when the user picks a Name suggestion (not free typing).
        /// </summary>
        private void _OnNameSuggestionSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SavePresetDialogViewModel viewModel)
            {
                return;
            }

            if (e.AddedItems.Count == 0)
            {
                return;
            }

            viewModel.ApplySuggestion(e.AddedItems[0] as string);
        }
    }
}

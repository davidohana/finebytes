using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Modal dialog for applied-filter name and Apply-To targets.
    /// </summary>
    /// <remarks>
    /// Opens height-to-content, then locks height so only width remains resizable. Relocks when
    /// Apply-on scope panels show or hide. Focuses the Name box on first open (MFR7 parity).
    /// </remarks>
    public partial class FilterOptionsDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public FilterOptionsDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            ModalDialogHorizontalResize.Attach(this);
            ModalDialogHorizontalResize.RelockOnDataContextProperties(
                this,
                nameof(FilterOptionsDialogViewModel.ShowSubstringOptions),
                nameof(FilterOptionsDialogViewModel.ShowTokenOptions),
                nameof(FilterOptionsDialogViewModel.HasId3v2MultiInstanceFields),
                nameof(FilterOptionsDialogViewModel.HasId3v2Language),
                nameof(FilterOptionsDialogViewModel.HasAncestorFolderLevel)
            );
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft name and Apply-To fields.</param>
        public FilterOptionsDialog(FilterOptionsDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            ModalDialogTextFocus.FocusAndSelectAll(NameBox);
        }
    }
}

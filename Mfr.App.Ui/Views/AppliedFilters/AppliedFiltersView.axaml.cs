using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Applied Filters pane host.
    /// </summary>
    public partial class AppliedFiltersView : UserControl
    {
        /// <summary>
        /// Initializes the Applied Filters pane.
        /// </summary>
        public AppliedFiltersView()
        {
            InitializeComponent();
            _WireSelectionHandlers();
            DataContextChanged += (_, _) =>
            {
                if (DataContext is AppliedFiltersViewModel viewModel)
                {
                    _OnDataContextAttached(viewModel);
                }
            };
        }
    }
}

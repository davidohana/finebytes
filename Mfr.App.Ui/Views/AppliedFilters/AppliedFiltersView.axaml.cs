using System.Windows.Input;
using Avalonia;
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
        /// Append-from-palette command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddFromPaletteCommandProperty = AvaloniaProperty.Register<
            AppliedFiltersView,
            ICommand?
        >(nameof(AddFromPaletteCommand));

        /// <summary>
        /// Gets or sets the command that appends the selected Available Filters row.
        /// </summary>
        public ICommand? AddFromPaletteCommand
        {
            get => GetValue(AddFromPaletteCommandProperty);
            set => SetValue(AddFromPaletteCommandProperty, value);
        }

        /// <summary>
        /// Initializes the Applied Filters pane.
        /// </summary>
        public AppliedFiltersView()
        {
            InitializeComponent();
            _WireSelectionHandlers();
            _WireKeyHandlers();
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

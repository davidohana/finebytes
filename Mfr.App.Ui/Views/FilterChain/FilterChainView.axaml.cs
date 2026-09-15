using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.FilterChain;

namespace Mfr.App.Ui.Views.FilterChain
{
    /// <summary>
    /// Filter Chain pane host.
    /// </summary>
    public partial class FilterChainView : UserControl
    {
        /// <summary>
        /// Append-from-palette command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddFromPaletteCommandProperty = AvaloniaProperty.Register<
            FilterChainView,
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
        /// Initializes the Filter Chain pane.
        /// </summary>
        public FilterChainView()
        {
            InitializeComponent();
            _WireSelectionHandlers();
            _WireKeyHandlers();
            _WireDragDropHandlers();
            _WireFilterOptionsHandlers();
            _WirePresetHandlers();
            DataContextChanged += (_, _) =>
            {
                if (DataContext is FilterChainViewModel viewModel)
                {
                    _OnDataContextAttached(viewModel);
                }
            };
        }
    }
}

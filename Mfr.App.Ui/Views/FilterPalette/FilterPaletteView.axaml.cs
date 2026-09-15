using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FilterPalette;

namespace Mfr.App.Ui.Views.FilterPalette
{
    /// <summary>
    /// Available Filters pane host.
    /// </summary>
    public partial class FilterPaletteView : UserControl
    {
        /// <summary>
        /// Filter Chain append command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddSelectedToFilterChainCommandProperty =
            AvaloniaProperty.Register<FilterPaletteView, ICommand?>(nameof(AddSelectedToFilterChainCommand));

        /// <summary>
        /// Gets or sets the command that appends the selected catalog row to Filter Chain.
        /// </summary>
        public ICommand? AddSelectedToFilterChainCommand
        {
            get => GetValue(AddSelectedToFilterChainCommandProperty);
            set => SetValue(AddSelectedToFilterChainCommandProperty, value);
        }

        /// <summary>
        /// Filter Chain remove-by-index command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> RemoveFilterChainStepsCommandProperty =
            AvaloniaProperty.Register<FilterPaletteView, ICommand?>(nameof(RemoveFilterChainStepsCommand));

        /// <summary>
        /// Gets or sets the command that removes applied steps dragged back to Available Filters.
        /// </summary>
        public ICommand? RemoveFilterChainStepsCommand
        {
            get => GetValue(RemoveFilterChainStepsCommandProperty);
            set => SetValue(RemoveFilterChainStepsCommandProperty, value);
        }

        /// <summary>
        /// Initializes the Available Filters pane.
        /// </summary>
        public FilterPaletteView()
        {
            InitializeComponent();
            _WireDragDropHandlers();
        }

        private void _OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _TryAddSelectedToFilterChain())
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && _TryClearSearch())
            {
                e.Handled = true;
            }
        }

        private void _OnFilterListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _TryAddSelectedToFilterChain())
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && _TryClearSearch())
            {
                e.Handled = true;
                return;
            }

            var text = e.KeySymbol;
            if (string.IsNullOrEmpty(text) || text.Length != 1)
            {
                return;
            }

            var ch = text[0];
            if (char.IsControl(ch) || char.IsWhiteSpace(ch))
            {
                return;
            }

            if (DataContext is not FilterPaletteViewModel palette)
            {
                return;
            }

            palette.SearchText += text;
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text?.Length ?? 0;
            e.Handled = true;
        }

        private void _OnFilterListDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (_TryAddSelectedToFilterChain())
            {
                e.Handled = true;
            }
        }

        private bool _TryAddSelectedToFilterChain()
        {
            var command = AddSelectedToFilterChainCommand;
            if (command is null || !command.CanExecute(null))
            {
                return false;
            }

            command.Execute(null);
            return true;
        }

        private bool _TryClearSearch()
        {
            if (DataContext is not FilterPaletteViewModel viewModel)
            {
                return false;
            }

            if (string.IsNullOrEmpty(viewModel.SearchText))
            {
                return false;
            }

            viewModel.SearchText = string.Empty;
            return true;
        }
    }
}

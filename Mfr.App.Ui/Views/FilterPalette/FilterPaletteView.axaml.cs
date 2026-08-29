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
        /// Applied Filters append command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> AddSelectedToAppliedCommandProperty =
            AvaloniaProperty.Register<FilterPaletteView, ICommand?>(nameof(AddSelectedToAppliedCommand));

        /// <summary>
        /// Gets or sets the command that appends the selected catalog row to Applied Filters.
        /// </summary>
        public ICommand? AddSelectedToAppliedCommand
        {
            get => GetValue(AddSelectedToAppliedCommandProperty);
            set => SetValue(AddSelectedToAppliedCommandProperty, value);
        }

        /// <summary>
        /// Initializes the Available Filters pane.
        /// </summary>
        public FilterPaletteView()
        {
            InitializeComponent();
        }

        private void _OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_TryAddSelectedToApplied())
                {
                    e.Handled = true;
                }

                return;
            }

            if (e.Key != Key.Escape)
            {
                return;
            }

            if (DataContext is not FilterPaletteViewModel viewModel)
            {
                return;
            }

            if (string.IsNullOrEmpty(viewModel.SearchText))
            {
                return;
            }

            viewModel.SearchText = string.Empty;
            e.Handled = true;
        }

        private void _OnFilterListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_TryAddSelectedToApplied())
                {
                    e.Handled = true;
                }

                return;
            }

            if (e.Key == Key.Escape)
            {
                if (
                    DataContext is FilterPaletteViewModel escapeViewModel
                    && !string.IsNullOrEmpty(escapeViewModel.SearchText)
                )
                {
                    escapeViewModel.SearchText = string.Empty;
                    e.Handled = true;
                }

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
            if (_TryAddSelectedToApplied())
            {
                e.Handled = true;
            }
        }

        private bool _TryAddSelectedToApplied()
        {
            var command = AddSelectedToAppliedCommand;
            if (command is null || !command.CanExecute(null))
            {
                return false;
            }

            command.Execute(null);
            return true;
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Mfr.App.Ui.ViewModels.FilterPalette;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Available Filters pane host.
    /// </summary>
    public partial class FilterPaletteView : UserControl
    {
        /// <summary>
        /// Initializes the Available Filters pane.
        /// </summary>
        public FilterPaletteView()
        {
            InitializeComponent();
        }

        private void _OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
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
    }
}

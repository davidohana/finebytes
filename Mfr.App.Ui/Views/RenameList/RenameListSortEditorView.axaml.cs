using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Flyout editor for Rename List Auto-Sort keys.
    /// </summary>
    public partial class RenameListSortEditorView : UserControl
    {
        /// <summary>
        /// Initializes the sort editor view.
        /// </summary>
        public RenameListSortEditorView()
        {
            InitializeComponent();
        }

        private RenameListViewModel? _ViewModel => DataContext as RenameListViewModel;

        private void _OnColumnComboBoxLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.DataContext is not RenameListSortEditorRow row)
            {
                return;
            }

            comboBox.SelectedItem = RenameListSortDisplay.EditorColumnOptions.First(option =>
                option.Column == row.Key.Column
            );
        }

        private void _OnColumnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.DataContext is not RenameListSortEditorRow row)
            {
                return;
            }

            if (comboBox.SelectedItem is not RenameListSortColumnOption option)
            {
                return;
            }

            _ViewModel?.SetSortKeyColumn(row.Index, option.Column);
        }

        private void _OnToggleDirectionClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not RenameListSortEditorRow row)
            {
                return;
            }

            _ViewModel?.ToggleSortKeyDirection(row.Index);
        }
    }
}

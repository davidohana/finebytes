using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FilterEditors;

namespace Mfr.App.Ui.Views.FilterEditors
{
    /// <summary>
    /// Option editor for <see cref="Filters.Space.SpaceCharacterFilter"/>.
    /// </summary>
    public partial class SpaceCharacterFilterEditorView : UserControl
    {
        private bool _isSyncingDefinition;
        private SpaceCharacterFilterEditorViewModel? _viewModel;

        /// <summary>
        /// Initializes the Space Character option editor.
        /// </summary>
        public SpaceCharacterFilterEditorView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => _OnDataContextChanged();
        }

        /// <summary>
        /// Sets the word-separator definition from a radio button.
        /// </summary>
        /// <param name="sender">Checked radio button.</param>
        /// <param name="e">Event args.</param>
        private void _OnDefinitionRadioChecked(object? sender, RoutedEventArgs e)
        {
            if (_isSyncingDefinition || DataContext is not SpaceCharacterFilterEditorViewModel viewModel)
            {
                return;
            }

            if (sender == SpaceDefinitionRadio)
            {
                viewModel.Definition = SpaceCharacterDefinition.Space;
                return;
            }

            if (sender == UnderscoreDefinitionRadio)
            {
                viewModel.Definition = SpaceCharacterDefinition.Underscore;
                return;
            }

            if (sender == OtherDefinitionRadio)
            {
                viewModel.Definition = SpaceCharacterDefinition.Other;
            }
        }

        private void _OnDataContextChanged()
        {
            _viewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _viewModel = DataContext as SpaceCharacterFilterEditorViewModel;
            _viewModel?.PropertyChanged += _OnViewModelPropertyChanged;

            _SyncDefinitionRadios();
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SpaceCharacterFilterEditorViewModel.Definition))
            {
                _SyncDefinitionRadios();
            }
        }

        private void _SyncDefinitionRadios()
        {
            if (DataContext is not SpaceCharacterFilterEditorViewModel viewModel)
            {
                return;
            }

            _isSyncingDefinition = true;
            try
            {
                SpaceDefinitionRadio.IsChecked = viewModel.Definition == SpaceCharacterDefinition.Space;
                UnderscoreDefinitionRadio.IsChecked = viewModel.Definition == SpaceCharacterDefinition.Underscore;
                OtherDefinitionRadio.IsChecked = viewModel.Definition == SpaceCharacterDefinition.Other;
            }
            finally
            {
                _isSyncingDefinition = false;
            }
        }
    }
}

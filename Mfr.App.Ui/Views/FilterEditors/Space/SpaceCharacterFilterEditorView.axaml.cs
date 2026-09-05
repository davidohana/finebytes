using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;

namespace Mfr.App.Ui.Views.FilterEditors.Space
{
    /// <summary>
    /// Option editor for <see cref="Filters.Space.SpaceCharacterFilter"/>.
    /// </summary>
    public partial class SpaceCharacterFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the Space Character option editor.
        /// </summary>
        public SpaceCharacterFilterEditorView()
        {
            InitializeComponent();
            OtherDefinitionRadio.IsCheckedChanged += _OnOtherDefinitionCheckedChanged;
            OtherCharacterBox.GotFocus += _OnOtherCharacterBoxGotFocus;
            OtherCharacterBox.AddHandler(TextInputEvent, _OnOtherCharacterTextInput, RoutingStrategies.Tunnel);
        }

        private void _OnOtherDefinitionCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (OtherDefinitionRadio.IsChecked == true)
            {
                OtherCharacterBox.Focus();
                OtherCharacterBox.SelectAll();
            }
        }

        private void _OnOtherCharacterBoxGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (DataContext is SpaceCharacterFilterEditorViewModel vm)
            {
                vm.Definition = SpaceCharacterDefinition.Other;
            }

            OtherCharacterBox.SelectAll();
        }

        /// <summary>
        /// Replaces the Other character on each keypress (MFR7 <c>tbOtherChar_KeyPress</c> parity).
        /// </summary>
        private void _OnOtherCharacterTextInput(object? sender, TextInputEventArgs e)
        {
            if (e.Text is not { Length: > 0 })
            {
                return;
            }

            OtherCharacterBox.Text = e.Text[0].ToString();
            e.Handled = true;
        }
    }
}

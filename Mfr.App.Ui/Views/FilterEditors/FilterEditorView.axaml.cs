using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.FilterEditors
{
    /// <summary>
    /// Filter Configuration pane host.
    /// </summary>
    public partial class FilterEditorView : UserControl
    {
        /// <summary>
        /// Reset-to-defaults command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> ResetSelectedToDefaultsCommandProperty =
            AvaloniaProperty.Register<FilterEditorView, ICommand?>(nameof(ResetSelectedToDefaultsCommand));

        /// <summary>
        /// Save-as-default command, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> SaveSelectedAsDefaultCommandProperty =
            AvaloniaProperty.Register<FilterEditorView, ICommand?>(nameof(SaveSelectedAsDefaultCommand));

        /// <summary>
        /// Gets or sets the command that restores the selected Applied Filters step to catalog defaults.
        /// </summary>
        public ICommand? ResetSelectedToDefaultsCommand
        {
            get => GetValue(ResetSelectedToDefaultsCommandProperty);
            set => SetValue(ResetSelectedToDefaultsCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command that saves the selected step’s options as the per-type add default.
        /// </summary>
        public ICommand? SaveSelectedAsDefaultCommand
        {
            get => GetValue(SaveSelectedAsDefaultCommandProperty);
            set => SetValue(SaveSelectedAsDefaultCommandProperty, value);
        }

        /// <summary>
        /// Initializes the Filter Configuration pane.
        /// </summary>
        public FilterEditorView()
        {
            InitializeComponent();
        }
    }
}

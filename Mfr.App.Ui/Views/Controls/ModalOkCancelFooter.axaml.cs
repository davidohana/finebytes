using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Shared OK/Cancel footer that closes the owning window with <see langword="true"/> / <see langword="false"/>.
    /// </summary>
    public partial class ModalOkCancelFooter : UserControl
    {
        /// <summary>
        /// Defines the <see cref="AcceptContent"/> property.
        /// </summary>
        public static readonly StyledProperty<string> AcceptContentProperty = AvaloniaProperty.Register<
            ModalOkCancelFooter,
            string
        >(nameof(AcceptContent), defaultValue: "OK");

        /// <summary>
        /// Defines the <see cref="IsAcceptEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAcceptEnabledProperty = AvaloniaProperty.Register<
            ModalOkCancelFooter,
            bool
        >(nameof(IsAcceptEnabled), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="AcceptDisabledTip"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> AcceptDisabledTipProperty = AvaloniaProperty.Register<
            ModalOkCancelFooter,
            object?
        >(nameof(AcceptDisabledTip));

        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<
            ModalOkCancelFooter,
            double
        >(nameof(Spacing), defaultValue: 8);

        /// <summary>
        /// Initializes the footer control.
        /// </summary>
        public ModalOkCancelFooter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Text on the accept button (default <c>OK</c>; Save Preset uses <c>Save</c>).
        /// </summary>
        public string AcceptContent
        {
            get => GetValue(AcceptContentProperty);
            set => SetValue(AcceptContentProperty, value);
        }

        /// <summary>
        /// Whether the accept button is enabled.
        /// </summary>
        public bool IsAcceptEnabled
        {
            get => GetValue(IsAcceptEnabledProperty);
            set => SetValue(IsAcceptEnabledProperty, value);
        }

        /// <summary>
        /// Optional tooltip while the accept button is disabled.
        /// </summary>
        public object? AcceptDisabledTip
        {
            get => GetValue(AcceptDisabledTipProperty);
            set => SetValue(AcceptDisabledTipProperty, value);
        }

        /// <summary>
        /// Horizontal gap between OK and Cancel (default 8).
        /// </summary>
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Accept button for headless tests and hosts that need a direct reference.
        /// </summary>
        public Button AcceptButton => OkButton;

        /// <summary>
        /// Cancel button for headless tests and hosts that need a direct reference.
        /// </summary>
        public Button DismissButton => CancelButton;

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (!IsAcceptEnabled)
            {
                return;
            }

            _CloseOwner(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            _CloseOwner(false);
        }

        private void _CloseOwner(bool result)
        {
            var window = this.FindAncestorOfType<Window>();
            window?.Close(result);
        }
    }
}

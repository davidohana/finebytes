using Avalonia;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Renders <see cref="StyledTextDisplay"/> as a single TextBlock with styled inlines.
    /// </summary>
    public partial class StyledTextView : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Display"/> property.
        /// </summary>
        public static readonly StyledProperty<StyledTextDisplay?> DisplayProperty = AvaloniaProperty.Register<
            StyledTextView,
            StyledTextDisplay?
        >(nameof(Display));

        /// <summary>
        /// Initializes the styled text view.
        /// </summary>
        public StyledTextView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the rich text to render.
        /// </summary>
        public StyledTextDisplay? Display
        {
            get => GetValue(DisplayProperty);
            set => SetValue(DisplayProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DisplayProperty)
            {
                _RebuildInlines();
            }
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _RebuildInlines();
        }

        private void _RebuildInlines()
        {
            var display = Display;
            if (display is null || display.IsEmpty)
            {
                DisplayTextBlock.Inlines?.Clear();
                return;
            }

            StyledTextInlines.Apply(this, DisplayTextBlock, display);
        }
    }
}

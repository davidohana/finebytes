using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Renders <see cref="StatusHintDisplay"/> as a single TextBlock with styled inlines.
    /// </summary>
    public partial class StatusHintView : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Hint"/> property.
        /// </summary>
        public static readonly StyledProperty<StatusHintDisplay?> HintProperty = AvaloniaProperty.Register<
            StatusHintView,
            StatusHintDisplay?
        >(nameof(Hint));

        /// <summary>
        /// Initializes the status hint view.
        /// </summary>
        public StatusHintView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the hint to render.
        /// </summary>
        public StatusHintDisplay? Hint
        {
            get => GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HintProperty)
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
            HintTextBlock.Inlines?.Clear();

            var hint = Hint;
            if (hint is null || hint.IsEmpty)
            {
                return;
            }

            foreach (var run in hint.Runs)
            {
                var inline = new Run { Text = run.Text };
                if (run.FontWeight.HasValue)
                {
                    inline.FontWeight = run.FontWeight.Value;
                }

                if (
                    !string.IsNullOrEmpty(run.ForegroundResourceKey)
                    && TryGetResource(run.ForegroundResourceKey, ActualThemeVariant, out var resource)
                    && resource is IBrush brush
                )
                {
                    inline.Foreground = brush;
                }

                HintTextBlock.Inlines!.Add(inline);
            }
        }
    }
}

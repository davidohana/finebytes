using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Main-window pane splitter that keeps the drag after the pointer leaves the bar.
    /// </summary>
    public sealed class PaneSplitter : GridSplitter
    {
        static PaneSplitter()
        {
            // A non-null brush is required for hit-testing; Transparent still receives pointer input.
            BackgroundProperty.OverrideDefaultValue<PaneSplitter>(Brushes.Transparent);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                e.Pointer.Capture(this);

            base.OnPointerPressed(e);
        }
    }
}

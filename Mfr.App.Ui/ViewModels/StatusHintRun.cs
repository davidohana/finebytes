using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// One styled segment in a status-bar rich-text hint.
    /// </summary>
    /// <remarks>
    /// Initializes a text run with default styling.
    /// </remarks>
    /// <param name="text">Segment text.</param>
    public sealed class StatusHintRun(string text)
    {

        /// <summary>
        /// Gets the segment text.
        /// </summary>
        public string Text { get; } = text;

        /// <summary>
        /// Gets an optional font weight. When null, the status bar default is used.
        /// </summary>
        public FontWeight? FontWeight { get; init; }

        /// <summary>
        /// Gets an optional application resource key used for the run foreground brush.
        /// </summary>
        public string? ForegroundResourceKey { get; init; }
    }
}

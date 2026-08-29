using Avalonia.Media;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Gray brush for Rename List metadata load-error cells (MFR7 <c>ForeErrorColor</c>).
    /// </summary>
    internal static class RenameListLoadErrorForeground
    {
        /// <summary>
        /// Gray foreground for metadata load-error cells (MFR7 <c>ForeErrorColor</c>).
        /// </summary>
        internal static IBrush Brush { get; } = new SolidColorBrush(Color.Parse("#808080"));
    }
}

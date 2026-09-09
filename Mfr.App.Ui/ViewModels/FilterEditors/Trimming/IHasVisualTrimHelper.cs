namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter options editor that hosts a Visual Trim Helper.
    /// </summary>
    internal interface IHasVisualTrimHelper
    {
        /// <summary>
        /// Gets the Visual Trim Helper for this editor.
        /// </summary>
        VisualTrimHelperViewModel TrimHelper { get; }
    }
}

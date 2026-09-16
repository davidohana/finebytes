namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Which geometry fields <see cref="DialogSession"/> restores for a modal dialog.
    /// </summary>
    internal enum DialogGeometryMode
    {
        /// <summary>
        /// Restore and capture width, height, and screen position.
        /// </summary>
        SizeAndPosition,

        /// <summary>
        /// Restore width and screen position only; height stays content-locked.
        /// </summary>
        WidthAndPosition,
    }
}

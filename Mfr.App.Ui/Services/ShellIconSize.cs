namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Shell icon pixel size requested from <see cref="ISystemIconProvider"/>.
    /// </summary>
    public enum ShellIconSize
    {
        /// <summary>
        /// 16×16 small icon, used by Report, List, and Small Icons.
        /// </summary>
        Small,

        /// <summary>
        /// 32×32 large icon, used by Large Icons, Tiles, and thumbnail fallbacks.
        /// </summary>
        Large,
    }
}

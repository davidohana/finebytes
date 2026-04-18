namespace Mfr.Models
{
    /// <summary>
    /// Resolved settings for the current process (see <see cref="ConfigLoader.Settings"/>).
    /// </summary>
    public sealed class MfrSettings
    {
        /// <summary>
        /// Maximum line length (characters) for name-list, casing-list, and replace-list text files.
        /// Defaults to <c>1000</c> when omitted from <c>mfr.config.json</c>.
        /// </summary>
        public int MaxListFileLineLength = 1000;
    }
}

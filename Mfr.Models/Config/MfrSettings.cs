using Mfr.Utils.Config;

namespace Mfr.Models
{
    /// <summary>
    /// Filter-related settings loaded from the <c>filters</c> section of the config file.
    /// </summary>
    public sealed class FilterSettings
    {
        /// <summary>
        /// Maximum line length (characters) for name-list, casing-list, and replace-list text files.
        /// </summary>
        [ConfigIntRange(1, 60000)]
        public int MaxListFileLineLength = 1000;
    }

    /// <summary>
    /// CLI logging settings loaded from the <c>log</c> section of the config file.
    /// </summary>
    public sealed class LogSettings
    {
        /// <summary>
        /// Maximum number of per-session log files to retain in the CLI log directory (oldest deleted first).
        /// </summary>
        [ConfigIntRange(1, 10000)]
        public int MaxSessionFiles = 100;

        /// <summary>
        /// Filename prefix for CLI session log files (before the timestamp).
        /// </summary>
        [ConfigStringMaxLength(200)]
        public string FilePrefix = "session-";

        /// <summary>
        /// Filename extension for CLI session log files, including the leading dot when a conventional extension is desired.
        /// </summary>
        [ConfigStringMaxLength(32)]
        public string FileExtension = ".log";

        /// <summary>
        /// Serilog output template for the CLI console sink.
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string ConsoleOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Serilog output template for the CLI file sink.
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string FileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }

    /// <summary>
    /// Resolved settings for the current process (see <see cref="ConfigLoader.Settings"/>).
    /// </summary>
    public sealed class MfrSettings
    {
        /// <summary>
        /// Settings for list-based filters (name, casing, replace lists).
        /// </summary>
        [ConfigSection]
        public FilterSettings Filters = new();

        /// <summary>
        /// CLI logging options.
        /// </summary>
        [ConfigSection]
        public LogSettings Log = new();
    }
}

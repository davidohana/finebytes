using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Resolved settings for the current process (see <see cref="ConfigLoader.Settings"/>).
    /// </summary>
    public sealed class MfrSettings
    {
        /// <summary>
        /// Maximum line length (characters) for name-list, casing-list, and replace-list text files.
        /// </summary>
        [ConfigIntRange(1, 60000)]
        public int MaxListFileLineLength = 1000;

        /// <summary>
        /// Maximum number of per-session log files to retain in the CLI log directory (oldest deleted first).
        /// </summary>
        [ConfigIntRange(1, 10000)]
        public int LogMaxSessionFiles = 100;

        /// <summary>
        /// Filename prefix for CLI session log files (before the timestamp).
        /// </summary>
        [ConfigStringMaxLength(200)]
        public string LogFilePrefix = "session-";

        /// <summary>
        /// Filename extension for CLI session log files, including the leading dot when a conventional extension is desired.
        /// </summary>
        [ConfigStringMaxLength(32)]
        public string LogFileExtension = ".log";

        /// <summary>
        /// Serilog output template for the CLI console sink.
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string LogConsoleOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Serilog output template for the CLI file sink.
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string LogFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }
}

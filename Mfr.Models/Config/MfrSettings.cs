using Mfr.Utils.Config;

namespace Mfr.Models.Config
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
    /// Diagnostic session-log settings loaded from the <c>log</c> section of the config file.
    /// <para>Used by both the CLI and the UI. The console template applies to CLI console output only.</para>
    /// </summary>
    public sealed class LogSettings
    {
        /// <summary>
        /// Directory for session log files.
        /// <para>
        /// When blank, <c>%LocalApplicationData%/finebytes/mfr/logs</c> is used.
        /// </para>
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string DirectoryPath = string.Empty;

        /// <summary>
        /// Maximum number of per-session log files to retain (oldest deleted first).
        /// </summary>
        [ConfigIntRange(1, 10000)]
        public int MaxSessionFiles = 100;

        /// <summary>
        /// Filename prefix for session log files (before the timestamp).
        /// </summary>
        [ConfigStringMaxLength(200)]
        public string FilePrefix = "session-";

        /// <summary>
        /// Filename extension for session log files, including the leading dot when a conventional extension is desired.
        /// </summary>
        [ConfigStringMaxLength(32)]
        public string FileExtension = ".log";

        /// <summary>
        /// Serilog output template for CLI console output.
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string ConsoleOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Serilog output template for the session log file (CLI and UI).
        /// </summary>
        [ConfigStringMaxLength(4096)]
        public string FileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }

    /// <summary>
    /// UI-related preferences loaded from the <c>ui</c> section of the config file.
    /// <para>Options dialog will expose these later; until then edit <c>mfr.config.json</c> or use CLI <c>--set</c>.</para>
    /// </summary>
    public sealed class UiSettings
    {
        /// <summary>
        /// When true, restore and save main-window size, position, and maximized state across launches.
        /// </summary>
        public bool RememberWindowState = true;

        /// <summary>
        /// When true, restore and save the last File Explorer folder across launches.
        /// </summary>
        public bool RememberLastFolder = true;
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
        /// Diagnostic session-log options (CLI file/console and UI file).
        /// </summary>
        [ConfigSection]
        public LogSettings Log = new();

        /// <summary>
        /// UI preferences (window/folder restore toggles and future Options settings).
        /// </summary>
        [ConfigSection]
        public UiSettings Ui = new();
    }
}

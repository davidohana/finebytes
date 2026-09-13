using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// UI prefs from the Options dialog, loaded from the <c>ui</c> section of the config file.
    /// <para>
    /// <see cref="ConfirmationPrompts"/> gates optional confirms via <see cref="ConfirmationPolicy"/>.
    /// </para>
    /// </summary>
    public sealed class UiConfig
    {
        /// <summary>
        /// How often the UI asks for confirmation before gated actions (Fewer / Normal / More).
        /// <para>
        /// Default <see cref="ConfirmationPrompts.Normal"/>. Replaces the former
        /// <c>ui.presets.confirmReplaceAppliedFiltersOnLoad</c> bool (no migration).
        /// </para>
        /// </summary>
        public ConfirmationPrompts ConfirmationPrompts = ConfirmationPrompts.Normal;
    }

    /// <summary>
    /// Diagnostic session-log config loaded from the <c>log</c> section of the config file.
    /// <para>Used by both the CLI and the UI. The console template applies to CLI console output only.</para>
    /// </summary>
    public sealed class LogConfig
    {
        /// <summary>
        /// Directory for session log files.
        /// <para>
        /// When blank, <see cref="AppDataPaths.LocalRoot"/> + <c>logs</c> is used.
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
        public string FileOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }
}

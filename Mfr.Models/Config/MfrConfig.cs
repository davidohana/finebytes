using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Filter-related config loaded from the <c>filters</c> section of the config file.
    /// </summary>
    public sealed class FilterConfig
    {
        /// <summary>
        /// Maximum length (characters) for list-based filter text.
        /// <para>
        /// Applies to each embedded name-list line, each casing-list word, and each replace-list search/replacement.
        /// </para>
        /// </summary>
        [ConfigIntRange(1, 60000)]
        public int MaxListFileLineLength = 1000;
    }

    /// <summary>
    /// UI-related config loaded from the <c>ui</c> section of the config file.
    /// </summary>
    public sealed class UiConfig
    {
        /// <summary>
        /// How often the UI asks for confirmation before gated actions.
        /// <para>Default <see cref="ConfirmationPrompts.Normal"/>.</para>
        /// </summary>
        public ConfirmationPrompts ConfirmationPrompts = ConfirmationPrompts.Normal;

        /// <summary>
        /// When <see langword="true"/>, double-click in the File List adds the selection to the Rename List.
        /// <para>Default <see langword="false"/> (open / navigate instead).</para>
        /// </summary>
        public bool DoubleClickAddsToRenameList;
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

    /// <summary>
    /// Resolved config for the current process (see <see cref="ConfigStore.Config"/>).
    /// </summary>
    public sealed class MfrConfig
    {
        /// <summary>
        /// Config for list-based filters (name, casing, replace lists).
        /// </summary>
        [ConfigSection]
        public FilterConfig Filters = new();

        /// <summary>
        /// Diagnostic session-log options (CLI file/console and UI file).
        /// </summary>
        [ConfigSection]
        public LogConfig Log = new();

        /// <summary>
        /// UI options (Options dialog + hand-edit <c>config.json</c> / CLI <c>--set</c>).
        /// </summary>
        [ConfigSection]
        public UiConfig Ui = new();
    }
}

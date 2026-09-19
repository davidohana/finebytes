using Mfr.Utils.Config;

namespace Mfr.Models.Config
{
    /// <summary>
    /// Options-dialog prefs from the <c>options</c> section of the config file (app config leaf section).
    /// <para>
    /// Bound via <see cref="ConfigJsonApplier"/> string leaves. <see cref="SuppressedConfirmations"/> gates
    /// optional confirms via Engine <c>ConfirmationPolicy</c>. Obsolete <c>options.confirmationPrompts</c> /
    /// root <c>ui</c> / Options fields formerly under <c>fileList</c> / <c>renameList</c> are ignored on soft-load
    /// (no migration).
    /// </para>
    /// </summary>
    public sealed class OptionsConfig
    {
        /// <summary>
        /// Confirmation kinds the user chose not to see again (empty = show all suppressible confirms).
        /// <para>
        /// Persisted as <c>options.suppressedConfirmations</c> (JSON array of camelCase enum names). Unknown members
        /// are skipped on soft-load. Default empty.
        /// </para>
        /// </summary>
        public List<ConfirmationKind> SuppressedConfirmations = [];

        /// <summary>
        /// When true, restore and save main-window size/position/splitters and dialog geometries across launches.
        /// <para>Persisted as <c>options.rememberWindowState</c> (JSON string <c>true</c>/<c>false</c>).</para>
        /// </summary>
        public bool RememberWindowState = true;

        /// <summary>
        /// When true, restore and save the last File List folder across launches.
        /// <para>Persisted as <c>options.rememberLastFolder</c> (JSON string <c>true</c>/<c>false</c>).</para>
        /// </summary>
        public bool RememberLastFolder = true;

        /// <summary>
        /// When <see langword="true"/>, double-click in the File List adds the selection to the Rename List.
        /// <para>
        /// Default <see langword="false"/> (open / navigate instead). Persisted as
        /// <c>options.doubleClickAddsToRenameList</c> (JSON string <c>true</c>/<c>false</c>).
        /// </para>
        /// </summary>
        public bool DoubleClickAddsToRenameList;

        /// <summary>
        /// Which path kinds become Rename List rows when adding from the File List.
        /// <para>Persisted as <c>options.addMode</c> (JSON string camelCase enum name).</para>
        /// </summary>
        public RenameListAddMode AddMode = RenameListAddMode.Files;

        /// <summary>
        /// When true, folder sources recurse: matching files in subfolders, and descendant folder rows when
        /// <see cref="AddMode"/> includes folders.
        /// <para>Persisted as <c>options.addFolderContents</c> (JSON string <c>true</c>/<c>false</c>).</para>
        /// </summary>
        public bool AddFolderContents = true;

        /// <summary>
        /// When true, show Hidden|System items in the File List and include them when adding to the Rename List.
        /// <para>Persisted as <c>options.includeHidden</c> (JSON string <c>true</c>/<c>false</c>). Default off.</para>
        /// </summary>
        public bool IncludeHidden;

        /// <summary>
        /// When true, store Rename List column pixel widths and reuse them when a column is shown again.
        /// <para>Persisted as <c>options.rememberColumnWidths</c> (JSON string <c>true</c>/<c>false</c>). Default on.</para>
        /// </summary>
        public bool RememberColumnWidths = true;

        /// <summary>
        /// GeoNames API username for nearby place lookups.
        /// <para>
        /// Defaults to the built-in FineBytes account (<see cref="Media.GeoNamesDefaults.Username"/>).
        /// Change it to use your own quota. Blank values resolve to that default at lookup time and
        /// are normalized back to the default when Options is saved. Persisted as
        /// <c>options.geoNamesUsername</c>.
        /// </para>
        /// </summary>
        [ConfigStringMaxLength(200)]
        public string GeoNamesUsername = Media.GeoNamesDefaults.Username;
    }

    /// <summary>
    /// Diagnostic session-log config from the <c>log</c> section of the config file (app config leaf section).
    /// <para>Used by both the CLI and the UI. The console template applies to CLI console output only.</para>
    /// </summary>
    public sealed class LogConfig
    {
        /// <summary>
        /// Directory for session log files.
        /// <para>
        /// When blank, the UI uses <c>logs/ui</c> and the console uses <c>logs/cli</c> under
        /// <see cref="AppDataPaths.LocalRoot"/>. An explicit path is shared by both hosts as-is.
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
    /// Rename-commit undo log retention from the <c>renameLog</c> section of the config file (app config leaf section).
    /// <para>
    /// Separate from diagnostic Serilog <c>log</c> / <c>logs/</c>. Disk files live under
    /// <see cref="AppDataPaths.LocalRoot"/> + <c>rename-logs</c> as JSON <c>.mfrlog</c>.
    /// Edited by the Options dialog Undo &amp; Rename Log section (Disabled / Limited / Unlimited).
    /// </para>
    /// </summary>
    public sealed class RenameLogConfig
    {
        /// <summary>
        /// Default Limited retention count (Options spinner + field initializer).
        /// </summary>
        public const int DefaultLimit = 10;

        /// <summary>
        /// How many on-disk rename logs to keep.
        /// <para>
        /// <c>0</c> = disk off (in-memory last operation still captured for Undo Last);
        /// <see cref="int.MaxValue"/> = unlimited; default <see cref="DefaultLimit"/>. Options maps these to
        /// Disabled / Unlimited / Limited radios and prunes on OK when the limit shrinks.
        /// </para>
        /// </summary>
        [ConfigIntRange(0, int.MaxValue)]
        public int Limit = DefaultLimit;
    }
}

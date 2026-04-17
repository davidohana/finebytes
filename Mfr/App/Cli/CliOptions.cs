using Serilog.Events;

namespace Mfr.App.Cli
{
    /// <summary>
    /// Immutable options parsed from the command line and carried into the CLI execution.
    /// </summary>
    /// <param name="PresetName">Name of the preset to apply for the file renaming.</param>
    /// <param name="Sources">List of source files, folders, or wildcard patterns to process.</param>
    /// <param name="IncludeFiles">Whether file entries should be included from resolved sources.</param>
    /// <param name="IncludeFolders">Whether folder entries should be included from resolved sources.</param>
    /// <param name="RecursiveDirectoryFileAdd">Whether directory sources should add files recursively when folder inclusion is disabled.</param>
    /// <param name="OutputFilePath">Optional output file path where JSON results are written.</param>
    /// <param name="IncludeHidden">Whether to include hidden and system files.</param>
    /// <param name="ContinueOnRenameError">Whether commit should continue after per-item rename errors.</param>
    /// <param name="ConfirmBeforeCommit">Whether to prompt for confirmation before applying each rename.</param>
    /// <param name="DryRun">Whether commit operations should be simulated without modifying the filesystem.</param>
    /// <param name="LogLevel">Minimum log level to emit.</param>
    /// <param name="LogDirectoryPath">Optional log directory override.</param>
    /// <param name="PresetsFilePath">Path to the JSON file containing presets.</param>
    public sealed record CliOptions(
        string PresetName,
        IReadOnlyList<string> Sources,
        bool IncludeFiles,
        bool IncludeFolders,
        bool RecursiveDirectoryFileAdd,
        string? OutputFilePath,
        bool IncludeHidden,
        bool ContinueOnRenameError,
        bool ConfirmBeforeCommit,
        bool DryRun,
        LogEventLevel LogLevel,
        string? LogDirectoryPath,
        string PresetsFilePath);
}

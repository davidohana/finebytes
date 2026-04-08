using Serilog.Events;

namespace Mfr.Cli
{
    /// <summary>
    /// Immutable options parsed from the command line and carried into the CLI execution.
    /// </summary>
    /// <param name="PresetName">Name of the preset to apply for the file renaming.</param>
    /// <param name="Sources">List of source files, folders, or wildcard patterns to process.</param>
    /// <param name="OutputFilePath">Optional output file path where JSON results are written.</param>
    /// <param name="IncludeHidden">Whether to include hidden and system files.</param>
    /// <param name="ContinueOnRenameError">Whether commit should continue after per-item rename errors.</param>
    /// <param name="LogLevel">Minimum log level to emit.</param>
    /// <param name="LogDirectoryPath">Optional log directory override.</param>
    /// <param name="PresetsFilePath">Path to the JSON file containing presets.</param>
    public sealed record CliOptions(
        string PresetName,
        IReadOnlyList<string> Sources,
        string? OutputFilePath,
        bool IncludeHidden,
        bool ContinueOnRenameError,
        LogEventLevel LogLevel,
        string? LogDirectoryPath,
        string PresetsFilePath)
    ;
}

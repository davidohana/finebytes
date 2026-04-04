using Mfr.Models;

namespace Mfr.Cli
{
    /// <summary>
    /// Immutable options parsed from the command line and carried into the CLI execution.
    /// </summary>
    /// <param name="PresetName">Name of the preset to apply for the file renaming.</param>
    /// <param name="Sources">List of source files, folders, or wildcard patterns to process.</param>
    /// <param name="OutputFormat">The format in which output should be printed (table, JSON, CSV).</param>
    /// <param name="IncludeHidden">Whether to include hidden and system files.</param>
    /// <param name="FailFast">Whether to stop processing on the first preview or rename error.</param>
    /// <param name="Silent">Suppress all output except for the process exit code.</param>
    /// <param name="Verbose">Reserved for future verbose output and diagnostics.</param>
    /// <param name="PresetsFilePath">Path to the JSON file containing presets.</param>
    public sealed record CliOptions(
        string PresetName,
        IReadOnlyList<string> Sources,
        OutputFormat OutputFormat,
        bool IncludeHidden,
        bool FailFast,
        bool Silent,
        bool Verbose,
        string PresetsFilePath)
    ;
}

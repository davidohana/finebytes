using Mfr.Models;

namespace Mfr.Cli
{
    // Data carried from argument parsing into the CLI execution.
    public sealed record CliOptions(
        string PresetName,
        IReadOnlyList<string> Sources,
        OutputFormat OutputFormat,
        bool IncludeHidden,
        bool ContinueOnPreviewErrors,
        bool Silent,
        bool Verbose,
        string PresetsFilePath)
    ;

}

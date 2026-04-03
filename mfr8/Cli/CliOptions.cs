using Mfr8.Core;

namespace Mfr8.Cli
{
    // Data carried from argument parsing into the CLI execution.
    public sealed record CliOptions(
        String PresetName,
        IReadOnlyList<String> Sources,
        OutputFormat OutputFormat,
        Boolean IncludeHidden,
        Boolean ContinueOnPreviewErrors,
        Boolean Silent,
        Boolean Verbose,
        String PresetsDirectory)
    ;

}

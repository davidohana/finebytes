using CommandLine;
using CommandLine.Text;

using Mfr8.Core;

namespace Mfr8.Cli
{
    internal static class CliCommandFactory
    {
        /// <summary>
        /// Parses command-line arguments into <see cref="CliOptions"/> using <c>CommandLineParser</c>.
        /// Prints help and validation errors to the console when parsing fails.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="exitCode">Exit code when parsing cannot produce options (help or error).</param>
        /// <returns>Parsed <see cref="CliOptions"/> on success; otherwise <c>null</c>.</returns>
        public static CliOptions? ParseArgs(string[] args, out int exitCode)
        {
            CliOptions? parsedOptions = null;
            var localExitCode = 0;

            var parser = new Parser(settings =>
            {
                // We control help / error output via HelpText.
                settings.HelpWriter = null;
            });

            var result = parser.ParseArguments<CliArguments>(args);

            _ = result
                .WithParsed(a =>
                {
                    try
                    {
                        parsedOptions = a.ToOptions();
                        localExitCode = 0;
                    }
                    catch (ArgumentException ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                        localExitCode = 1;
                    }
                })
                .WithNotParsed(errors =>
                {
                    var helpText = HelpText.AutoBuild(result, h =>
                    {
                        h.Heading = "mfr8 - Magic File Renamer";
                        h.AdditionalNewLineAfterOption = true;
                        h.AddDashesToOption = true;
                        return HelpText.DefaultParsingErrorsHandler(result, h);
                    },
                    e => e);

                    Console.WriteLine(helpText);

                    // Help requests exit with 0, other errors with 1.
                    localExitCode = errors.Any(e =>
                            e is { Tag: ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError })
                        ? 0
                        : 1;
                });

            exitCode = localExitCode;
            return parsedOptions;
        }

        private sealed class CliArguments
        {
            [Value(0, MetaName = "sources", Required = true, HelpText = "Files, folders, or wildcards to rename (e.g. C:\\Music\\*.mp3).")]
            public IEnumerable<string> Sources { get; set; } = [];

            [Option("preset", Required = true, HelpText = "Preset name or id (matches preset JSON 'name' or 'id').")]
            public string PresetName { get; set; } = string.Empty;

            [Option("presets-dir", HelpText = "Override presets directory (for development/testing).")]
            public string? PresetsDirectory { get; set; }

            [Option("output", HelpText = "Output format: table | json | csv.", Default = "table")]
            public string Output { get; set; } = "table";

            [Option("include-hidden", HelpText = "Include hidden/system files.")]
            public bool IncludeHidden { get; set; }

            [Option("continue-on-preview-errors", HelpText = "Continue even if preview errors exist.")]
            public bool ContinueOnPreviewErrors { get; set; }

            [Option("silent", HelpText = "Only exit code, no output.")]
            public bool Silent { get; set; }

            [Option("verbose", HelpText = "Reserved for future verbose diagnostics.")]
            public bool Verbose { get; set; }

            internal CliOptions ToOptions()
            {
                var format = _ParseOutputFormat(Output);

                var presetsDir = string.IsNullOrWhiteSpace(PresetsDirectory)
                    ? PresetLoader.DefaultPresetsDirectory()
                    : PresetsDirectory;

                return new CliOptions(
                    PresetName: PresetName,
                    Sources: [.. Sources],
                    OutputFormat: format,
                    IncludeHidden: IncludeHidden,
                    ContinueOnPreviewErrors: ContinueOnPreviewErrors,
                    Silent: Silent,
                    Verbose: Verbose,
                    PresetsDirectory: presetsDir);
            }

            private static OutputFormat _ParseOutputFormat(string? value)
            {
                var normalized = string.IsNullOrWhiteSpace(value)
                    ? throw new ArgumentException("Unknown output ''. Use table|json|csv.")
                    : value.Trim().ToLowerInvariant();

                return normalized switch
                {
                    "table" => OutputFormat.Table,
                    "json" => OutputFormat.Json,
                    "csv" => OutputFormat.Csv,
                    _ => throw new ArgumentException($"Unknown output '{value}'. Use table|json|csv.")
                };
            }
        }
    }

}

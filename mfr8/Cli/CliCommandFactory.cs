using CommandLine;

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
        /// <returns>Parsed <see cref="CliOptions"/> on success; <c>null</c> when the user requested help (<c>--help</c> / <c>-h</c>).</returns>
        /// <exception cref="ArgumentException">Thrown when argument parsing cannot produce options.</exception>
        public static CliOptions? ParseArgs(string[] args)
        {
            var result = Parser.Default.ParseArguments<CliArguments>(args);
            return result.MapResult(_MapParsed, _MapNotParsed);

            CliOptions _MapParsed(CliArguments parsedArgs)
            {
                return parsedArgs.ToOptions();
            }

            CliOptions? _MapNotParsed(IEnumerable<Error> errors)
            {
                var list = errors.ToList();
                return list.All(e => e.Tag is ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError)
                    ? null
                    : throw new ArgumentException("Invalid arguments.");
            }

        }

        private sealed class CliArguments
        {
            [Value(0, MetaName = "sources", Required = true, HelpText = "Files, folders, or wildcards to rename (e.g. C:\\Music\\*.mp3).")]
            public IEnumerable<string> Sources { get; set; } = [];

            [Option('p', "preset", Required = true, HelpText = "Preset name or id (matches preset JSON 'name' or 'id').")]
            public string PresetName { get; set; } = string.Empty;

            [Option('d', "presets-dir", HelpText = "Override presets directory (for development/testing).")]
            public string? PresetsDirectory { get; set; }

            [Option('o', "output", HelpText = "Output format: table | json | csv.", Default = "table")]
            public string Output { get; set; } = "table";

            [Option('i', "include-hidden", HelpText = "Include hidden/system files.")]
            public bool IncludeHidden { get; set; }

            [Option('c', "continue-on-preview-errors", HelpText = "Continue even if preview errors exist.")]
            public bool ContinueOnPreviewErrors { get; set; }

            [Option('s', "silent", HelpText = "Only exit code, no output.")]
            public bool Silent { get; set; }

            [Option('v', "verbose", HelpText = "Reserved for future verbose diagnostics.")]
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

using Mfr8.Core;
using Mfr8.Models;

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
        /// <exception cref="UserException">Thrown when argument parsing cannot produce options.</exception>
        public static CliOptions? ParseArgs(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<CliArguments>(args);
            return result switch
            {
                CommandLine.Parsed<CliArguments> parsed => parsed.Value.ToOptions(),
                CommandLine.NotParsed<CliArguments> notParsed => _MapNotParsed(notParsed.Errors),
                _ => throw new UserException("Invalid arguments."),
            };

            static CliOptions? _MapNotParsed(IEnumerable<CommandLine.Error> errors)
            {
                var errorList = errors.ToList();
                return errorList.All(e => e.Tag is CommandLine.ErrorType.HelpRequestedError or CommandLine.ErrorType.HelpVerbRequestedError)
                    ? null
                    : throw new UserException(_BuildErrorMessage(errorList));
            }
        }

        private static string _BuildErrorMessage(IReadOnlyList<CommandLine.Error> errorList)
        {
            return errorList.Any(_IsMissingRequiredSource)
                ? "Missing required argument: sources. Provide one or more files, folders, or wildcards."
                : "Invalid arguments.";
        }

        private static bool _IsMissingRequiredSource(CommandLine.Error error)
        {
            return (error is CommandLine.MissingValueOptionError missingValueError
                && missingValueError.NameInfo.Equals(CommandLine.NameInfo.EmptyName))
                || (error is CommandLine.MissingRequiredOptionError missingRequiredOptionError
                && missingRequiredOptionError.NameInfo.Equals(CommandLine.NameInfo.EmptyName));
        }

        private sealed class CliArguments
        {
            [CommandLine.Value(0, MetaName = "sources", Required = true, HelpText = "Files, folders, or wildcards to rename (e.g. C:\\Music\\*.mp3).")]
            public IEnumerable<string> Sources { get; set; } = [];

            [CommandLine.Option('p', "preset", Required = true, HelpText = "Preset name (must be unique inside presets JSON).")]
            public string PresetName { get; set; } = string.Empty;

            [CommandLine.Option('d', "presets-file", HelpText = "Override presets JSON file path.")]
            public string? PresetsFilePath { get; set; }

            [CommandLine.Option('o', "output", HelpText = "Output format: table | json | csv.", Default = "table")]
            public string Output { get; set; } = "table";

            [CommandLine.Option('i', "include-hidden", HelpText = "Include hidden/system files.")]
            public bool IncludeHidden { get; set; }

            [CommandLine.Option('c', "continue-on-preview-errors", HelpText = "Continue even if preview errors exist.")]
            public bool ContinueOnPreviewErrors { get; set; }

            [CommandLine.Option('s', "silent", HelpText = "Only exit code, no output.")]
            public bool Silent { get; set; }

            [CommandLine.Option('v', "verbose", HelpText = "Reserved for future verbose diagnostics.")]
            public bool Verbose { get; set; }

            internal CliOptions ToOptions()
            {
                var format = _ParseOutputFormat(Output);

                var presetsFilePath = string.IsNullOrWhiteSpace(PresetsFilePath)
                    ? PresetManager.DefaultPresetsFilePath()
                    : PresetsFilePath;

                return new CliOptions(
                    PresetName: PresetName,
                    Sources: [.. Sources],
                    OutputFormat: format,
                    IncludeHidden: IncludeHidden,
                    ContinueOnPreviewErrors: ContinueOnPreviewErrors,
                    Silent: Silent,
                    Verbose: Verbose,
                    PresetsFilePath: presetsFilePath);
            }

            private static OutputFormat _ParseOutputFormat(string? value)
            {
                var normalized = string.IsNullOrWhiteSpace(value)
                    ? throw new UserException("Unknown output ''. Use table|json|csv.")
                    : value.Trim().ToLowerInvariant();

                return normalized switch
                {
                    "table" => OutputFormat.Table,
                    "json" => OutputFormat.Json,
                    "csv" => OutputFormat.Csv,
                    _ => throw new UserException($"Unknown output '{value}'. Use table|json|csv.")
                };
            }
        }
    }

}

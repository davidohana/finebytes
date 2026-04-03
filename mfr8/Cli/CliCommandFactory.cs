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

            result
                .WithParsed(a =>
                {
                    if (!_TryParseOutputFormat(a.Output, out var format))
                    {
                        Console.Error.WriteLine($"Unknown output '{a.Output}'. Use table|json|csv.");
                        localExitCode = 1;
                        return;
                    }

                    var presetsDir = string.IsNullOrWhiteSpace(a.PresetsDirectory)
                        ? PresetLoader.DefaultPresetsDirectory()
                        : a.PresetsDirectory;

                    parsedOptions = new CliOptions(
                        PresetName: a.PresetName,
                        Sources: a.Sources.ToArray(),
                        OutputFormat: format,
                        IncludeHidden: a.IncludeHidden,
                        ContinueOnPreviewErrors: a.ContinueOnPreviewErrors,
                        Silent: a.Silent,
                        Verbose: a.Verbose,
                        PresetsDirectory: presetsDir);

                    localExitCode = 0;
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
                            e.Tag == ErrorType.HelpRequestedError ||
                            e.Tag == ErrorType.HelpVerbRequestedError)
                        ? 0
                        : 1;
                });

            exitCode = localExitCode;
            return parsedOptions;
        }

        private static bool _TryParseOutputFormat(string? value, out OutputFormat format)
        {
            format = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "table":
                    format = OutputFormat.Table;
                    return true;
                case "json":
                    format = OutputFormat.Json;
                    return true;
                case "csv":
                    format = OutputFormat.Csv;
                    return true;
                default:
                    return false;
            }
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
        }
    }

}

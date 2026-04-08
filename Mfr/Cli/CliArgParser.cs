using CommandLine.Text;

using Mfr.Core;
using Mfr.Utils;

namespace Mfr.Cli
{
    internal static class CliArgParser
    {
        /// <summary>
        /// Parses command-line arguments into <see cref="CliOptions"/> using <c>CommandLineParser</c>.
        /// Prints help and validation errors to the console when parsing fails.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Parsed <see cref="CliOptions"/> on success; <c>null</c> when the user requested help or version output.</returns>
        /// <exception cref="UserException">Thrown when argument parsing cannot produce options.</exception>
        public static CliOptions? ParseArgs(string[] args)
        {
            var normalizedArgs = args.Select(_NormalizeDashOptionAlias).ToArray();
            var result = CommandLine.Parser.Default.ParseArguments<CliArguments>(normalizedArgs);
            return result switch
            {
                CommandLine.Parsed<CliArguments> parsed => parsed.Value.ToOptions(),
                CommandLine.NotParsed<CliArguments> notParsed => _MapNotParsed(notParsed.Errors),
                _ => throw new UserException("Invalid arguments."),
            };

            // Support multi-letter single-dash abbreviations (-core) by normalizing to long options.
            static string _NormalizeDashOptionAlias(string arg)
            {
                return arg switch
                {
                    "-core" => "--core",
                    _ => arg
                };
            }

            static CliOptions? _MapNotParsed(IEnumerable<CommandLine.Error> errors)
            {
                var errorList = errors.ToList();
                return errorList.All(e => e.Tag is CommandLine.ErrorType.HelpRequestedError
                    or CommandLine.ErrorType.HelpVerbRequestedError
                    or CommandLine.ErrorType.VersionRequestedError)
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
            [CommandLine.Value(0, MetaName = "sources", Required = true, HelpText = "Source paths or wildcards to rename (e.g. C:\\Music\\*.mp3).")]
            public IEnumerable<string> Sources { get; set; } = [];

            [CommandLine.Option('p', "preset", Required = true, HelpText = "Preset name (must be unique inside presets JSON).")]
            public string PresetName { get; set; } = string.Empty;

            [CommandLine.Option('d', "presets-file", HelpText = "Override presets JSON file path.")]
            public string? PresetsFilePath { get; set; }

            [CommandLine.Option('f', "output-file", HelpText = "Optional JSON output file path.")]
            public string? OutputFilePath { get; set; }

            [CommandLine.Option('i', "include-hidden", HelpText = "Include hidden/system files.")]
            public bool IncludeHidden { get; set; }

            [CommandLine.Option("files", HelpText = "Include files from sources: yes | no (default: yes).", Default = "yes")]
            public string IncludeFiles { get; set; } = "yes";

            [CommandLine.Option("folders", HelpText = "Include folders from sources: yes | no (default: no).", Default = "no")]
            public string IncludeFolders { get; set; } = "no";

            [CommandLine.Option("core", HelpText = "Continue on rename error (default: false).")]
            public bool ContinueOnRenameError { get; set; }

            [CommandLine.Option('l', "log-level", HelpText = "Minimum log level: debug | info | warn | error.", Default = CliLogging.DefaultLogLevelName)]
            public string LogLevel { get; set; } = CliLogging.DefaultLogLevelName;

            [CommandLine.Option("log-dir", HelpText = "Optional log directory path override.")]
            public string? LogDirectoryPath { get; set; }

            [Usage(ApplicationAlias = "mfr")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "Run with wildcard source",
                        new CliArguments
                        {
                            Sources = [@"C:\Music\*.mp3"],
                            PresetName = "clean-filenames"
                        });

                    yield return new Example(
                        "Run with JSON output file",
                        new CliArguments
                        {
                            Sources = [@"C:\Music\*.mp3"],
                            PresetName = "clean-filenames",
                            OutputFilePath = @"C:\temp\mfr-result.json"
                        });
                }
            }

            internal CliOptions ToOptions()
            {
                var presetsFilePath = PresetsFilePath.IsBlank()
                    ? PresetManager.DefaultPresetsFilePath()
                    : PresetsFilePath;
                var includeFiles = _ParseYesNoOption(optionName: "--files", value: IncludeFiles);
                var includeFolders = _ParseYesNoOption(optionName: "--folders", value: IncludeFolders);
                if (!includeFiles && !includeFolders)
                {
                    throw new UserException("At least one of --files or --folders must be yes.");
                }

                return new CliOptions(
                    PresetName: PresetName,
                    Sources: [.. Sources],
                    IncludeFiles: includeFiles,
                    IncludeFolders: includeFolders,
                    OutputFilePath: OutputFilePath.IsBlank() ? null : OutputFilePath.Trim(),
                    IncludeHidden: IncludeHidden,
                    ContinueOnRenameError: ContinueOnRenameError,
                    LogLevel: CliLogging.ParseLogLevel(LogLevel),
                    LogDirectoryPath: LogDirectoryPath.IsBlank() ? null : LogDirectoryPath.Trim(),
                    PresetsFilePath: presetsFilePath);
            }

            private static bool _ParseYesNoOption(string optionName, string value)
            {
                var normalizedValue = value.Trim().ToLowerInvariant();
                return normalizedValue switch
                {
                    "yes" => true,
                    "no" => false,
                    _ => throw new UserException($"Invalid value for {optionName}: '{value}'. Expected yes or no.")
                };
            }
        }
    }

}

using System.ComponentModel;

using Spectre.Console.Cli;

using Mfr.Core;
using Mfr.Utils;

namespace Mfr.Cli
{
    internal static class CliArgParser
    {
        /// <summary>
        /// Parses command-line arguments into <see cref="CliOptions"/>.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Parsed <see cref="CliOptions"/> on success; <c>null</c> when the user requested help output.</returns>
        /// <exception cref="UserException">Thrown when argument parsing cannot produce options, including unknown flags or options.</exception>
        public static CliOptions? ParseArgs(string[] args)
        {
            ParseCommandSettings? parsedSettings = null;
            try
            {
                ParseCommand.CaptureSettings = settings => parsedSettings = settings;
                var app = new CommandApp<ParseCommand>();
                app.Configure(_ConfigureCommandApp);
                _ = app.WithDescription($"Magic File Renamer v{_GetAssemblyVersionString()}.");
                _ = app.Run(args);
            }
            catch (CommandParseException exception)
            {
                var parserMessage = exception.Message.IsBlank()
                    ? "Invalid arguments."
                    : exception.Message;
                throw new UserException(parserMessage);
            }
            catch (CommandRuntimeException exception)
            {
                var parserMessage = exception.Message.IsBlank()
                    ? "Invalid arguments."
                    : exception.Message;
                throw new UserException(parserMessage);
            }
            finally
            {
                ParseCommand.CaptureSettings = null;
            }

            if (parsedSettings is null)
            {
                return null;
            }

            var presetName = _GetValueOrDefault(parsedSettings.PresetName, defaultValue: string.Empty);
            if (presetName.IsBlank())
            {
                throw new UserException("Missing required argument: --preset.");
            }

            var includeFilesOptionValue = _GetValueOrDefault(parsedSettings.IncludeFiles, defaultValue: "yes");
            var includeFoldersOptionValue = _GetValueOrDefault(parsedSettings.IncludeFolders, defaultValue: "no");
            var includeFiles = _ParseYesNoOption(optionName: "--files", value: includeFilesOptionValue);
            var includeFolders = _ParseYesNoOption(optionName: "--folders", value: includeFoldersOptionValue);
            if (!includeFiles && !includeFolders)
            {
                throw new UserException("At least one of --files or --folders must be yes.");
            }

            var sources = parsedSettings.Sources
                .Where(source => !source.IsBlank())
                .Select(source => source.Trim())
                .ToList();
            if (sources.Count == 0)
            {
                throw new UserException("Missing required argument: SOURCES. Provide one or more source paths or wildcards.");
            }

            var rawPresetsFilePath = _GetValueOrDefault(parsedSettings.PresetsFilePath, defaultValue: string.Empty);
            var presetsFilePath = rawPresetsFilePath.IsBlank()
                ? PresetManager.DefaultPresetsFilePath()
                : rawPresetsFilePath.Trim();
            var outputFilePath = _GetValueOrDefault(parsedSettings.OutputFilePath, defaultValue: string.Empty);
            var logLevel = _GetValueOrDefault(parsedSettings.LogLevel, defaultValue: CliLogging.DefaultLogLevelName);
            var logDirectoryPath = _GetValueOrDefault(parsedSettings.LogDirectoryPath, defaultValue: string.Empty);
            return new CliOptions(
                PresetName: presetName.Trim(),
                Sources: sources,
                IncludeFiles: includeFiles,
                IncludeFolders: includeFolders,
                RecursiveDirectoryFileAdd: parsedSettings.RecursiveDirectoryFileAdd,
                OutputFilePath: outputFilePath.IsBlank() ? null : outputFilePath.Trim(),
                IncludeHidden: parsedSettings.IncludeHidden,
                ContinueOnRenameError: parsedSettings.ContinueOnRenameError,
                ConfirmBeforeCommit: parsedSettings.ConfirmBeforeCommit,
                DryRun: parsedSettings.DryRun,
                LogLevel: CliLogging.ParseLogLevel(logLevel),
                LogDirectoryPath: logDirectoryPath.IsBlank() ? null : logDirectoryPath.Trim(),
                PresetsFilePath: presetsFilePath);
        }

        private static void _ConfigureCommandApp(IConfigurator configuration)
        {
            _ = configuration
                .SetApplicationName("mfr")
                .PropagateExceptions()
                .UseStrictParsing()
                .AddExample(["C:\\Music\\*.mp3", "-p", "clean", "--dry-run"])
                .AddExample(["C:\\Music\\**\\*.flac", "-p", "lowercase-extension", "--log-level", "debug"])
                .AddExample(["C:\\Music", "-p", "name_from_id3", "-r"])
                .AddExample(["C:\\Music", "C:\\Podcasts", "-p", "my_preset", "--files", "yes", "--folders", "yes", "--output-file", "C:\\Temp\\mfr-results.json"]);
        }

        private static string _GetAssemblyVersionString()
        {
            return typeof(CliApp).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        }

        private static string _GetValueOrDefault(string? value, string defaultValue)
        {
            return value.IsBlank() ? defaultValue : value;
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

        private sealed class ParseCommand : Command<ParseCommandSettings>
        {
            public static Action<ParseCommandSettings>? CaptureSettings { get; set; }

            protected override int Execute(CommandContext context, ParseCommandSettings settings, CancellationToken cancellationToken)
            {
                CaptureSettings?.Invoke(settings);
                return 0;
            }
        }

        private sealed class ParseCommandSettings : CommandSettings
        {
            [CommandArgument(0, "<SOURCES>")]
            [Description("Files, directories, filemasks, globs to rename (space-seperated).")]
            public string[] Sources { get; init; } = [];

            [CommandOption("-p|--preset <NAME>")]
            [Description("Preset name")]
            public string? PresetName { get; init; }

            [CommandOption("--presets-file <PATH>")]
            [Description("Override presets file path.")]
            public string? PresetsFilePath { get; init; }

            [CommandOption("-o|--output-file <PATH>")]
            [Description("Optional JSON output file path for rename results.")]
            public string? OutputFilePath { get; init; }

            [CommandOption("--include-hidden")]
            [Description("Include hidden and system files.")]
            public bool IncludeHidden { get; init; }

            [CommandOption("--files <VALUE>")]
            [Description("Include files from sources: yes | no (default: yes).")]
            public string? IncludeFiles { get; init; }

            [CommandOption("--folders <VALUE>")]
            [Description("Include folders from sources: yes | no (default: no).")]
            public string? IncludeFolders { get; init; }

            [CommandOption("-r|--recursive")]
            [Description("Expand directory sources recursively (when 'add folders' disabled).")]
            public bool RecursiveDirectoryFileAdd { get; init; }

            [CommandOption("--core")]
            [Description("Continue-On-Rename-Errors instead of stopping at the first failure.")]
            public bool ContinueOnRenameError { get; init; }

            [CommandOption("-c|--confirm")]
            [Description("Prompt for confirmation before applying each rename.")]
            public bool ConfirmBeforeCommit { get; init; }

            [CommandOption("--dry-run")]
            [Description("Preview and report commit outcomes without changing files on disk.")]
            public bool DryRun { get; init; }

            [CommandOption("-l|--log-level <LEVEL>")]
            [Description("Minimum log level: debug | info | warn | error.")]
            public string? LogLevel { get; init; }

            [CommandOption("--log-dir <PATH>")]
            [Description("Optional log directory path override.")]
            public string? LogDirectoryPath { get; init; }
        }
    }

}

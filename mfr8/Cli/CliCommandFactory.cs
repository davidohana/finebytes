using System.CommandLine;

using Mfr8.Core;

namespace Mfr8.Cli;

internal static class CliCommandFactory
{
    /// <summary>
    /// Creates and configures the root <see cref="RootCommand"/> for the CLI.
    /// </summary>
    /// <param name="execute">Callback that receives parsed <see cref="CliOptions"/> and returns an exit code.</param>
    /// <returns>A configured <see cref="RootCommand"/> instance.</returns>
    public static RootCommand CreateRootCommand(Func<CliOptions, int> execute)
    {
        var presetOption = new Option<string>("--preset")
        {
            Description = "Preset name or id (matches preset JSON 'name' or 'id')."
        };

        var presetsDirOption = new Option<string>("--presets-dir")
        {
            Description = "Override presets directory (for development/testing)."
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table | json | csv."
        };

        var includeHiddenOption = new Option<bool>("--include-hidden")
        {
            Description = "Include hidden/system files."
        };

        var continueOption = new Option<bool>("--continue-on-preview-errors")
        {
            Description = "Continue even if preview errors exist."
        };

        var silentOption = new Option<bool>("--silent")
        {
            Description = "Silent mode (only exit code)."
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Verbose mode (reserved in phase 1)."
        };

        var sourcesArgument = new Argument<string[]>("sources");
        sourcesArgument.Arity = ArgumentArity.OneOrMore;

        var root = new RootCommand("mfr8 (CLI-only): rename using JSON presets and filename-only filters.");
        root.Add(presetOption);
        root.Add(presetsDirOption);
        root.Add(outputOption);
        root.Add(includeHiddenOption);
        root.Add(continueOption);
        root.Add(silentOption);
        root.Add(verboseOption);
        root.Add(sourcesArgument);

        root.SetAction(parseResult =>
        {
            var preset = parseResult.GetValue(presetOption);
            var sources = parseResult.GetValue(sourcesArgument);
            var output = parseResult.GetValue(outputOption) ?? "table";
            var presetsDir = parseResult.GetValue(presetsDirOption) ?? PresetLoader.DefaultPresetsDirectory();

            var includeHidden = parseResult.GetValue(includeHiddenOption);
            var continueOnPreviewErrors = parseResult.GetValue(continueOption);
            var silent = parseResult.GetValue(silentOption);
            var verbose = parseResult.GetValue(verboseOption);

            if (string.IsNullOrWhiteSpace(preset))
            {
                Console.Error.WriteLine("Missing required option: --preset.");
                return 1;
            }

            if (!_TryParseOutputFormat(output, out var outFormat))
            {
                Console.Error.WriteLine($"Unknown output '{output}'. Use table|json|csv.");
                return 1;
            }

            var options = new CliOptions(
                PresetName: preset,
                Sources: sources ?? Array.Empty<string>(),
                OutputFormat: outFormat,
                IncludeHidden: includeHidden,
                ContinueOnPreviewErrors: continueOnPreviewErrors,
                Silent: silent,
                Verbose: verbose,
                PresetsDirectory: presetsDir);

            return execute(options);
        });

        return root;
    }

    private static bool _TryParseOutputFormat(string? value, out OutputFormat format)
    {
        format = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

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
}


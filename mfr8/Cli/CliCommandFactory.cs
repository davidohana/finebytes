using System.CommandLine;

using Mfr8.Core;

namespace Mfr8.Cli;

internal static class CliCommandFactory
{
    /// <summary>
    /// Parses command-line arguments into <see cref="CliOptions"/>, or returns a process exit code for help/errors.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="exitCode">Exit code when parsing cannot produce options (help or error).</param>
    /// <returns>Parsed <see cref="CliOptions"/> on success; otherwise <c>null</c>.</returns>
    public static CliOptions? ParseArgs(string[] args, out int exitCode)
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

        // If user explicitly asks for help, don't try to bind required arguments.
        if (args.Any(a => a == "--help" || a == "-h" || a == "/?"))
        {
            Console.WriteLine("Usage: mfr8 <sources>... --preset <name-or-id> [options]");
            Console.WriteLine();
            Console.WriteLine("  <sources>   Files, folders, or wildcards to rename (e.g. C:\\Music\\*.mp3).");
            Console.WriteLine("  --preset    Preset name or id (matches preset JSON 'name' or 'id').");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --presets-dir <dir>              Override presets directory.");
            Console.WriteLine("  --output <table|json|csv>        Output format (default: table).");
            Console.WriteLine("  --include-hidden                 Include hidden/system files.");
            Console.WriteLine("  --continue-on-preview-errors     Continue even if preview errors exist.");
            Console.WriteLine("  --silent                         Only exit code, no output.");
            Console.WriteLine("  --verbose                        Reserved for future verbose diagnostics.");

            exitCode = 0;
            return null;
        }

        var parseResult = root.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
                Console.Error.WriteLine(error.Message);
            exitCode = 1;
            return null;
        }

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
            exitCode = 1;
            return null;
        }

        if (!_TryParseOutputFormat(output, out var outFormat))
        {
            Console.Error.WriteLine($"Unknown output '{output}'. Use table|json|csv.");
            exitCode = 1;
            return null;
        }

        exitCode = 0;
        return new CliOptions(
            PresetName: preset,
            Sources: sources ?? Array.Empty<string>(),
            OutputFormat: outFormat,
            IncludeHidden: includeHidden,
            ContinueOnPreviewErrors: continueOnPreviewErrors,
            Silent: silent,
            Verbose: verbose,
            PresetsDirectory: presetsDir);
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


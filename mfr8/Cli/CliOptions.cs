namespace Mfr8.Cli;

using Mfr8.Core;

// Phase 1 uses minimal argument parsing (no GUI) to keep the project bootstrapped.
public sealed record CliOptions(
    string PresetName,
    IReadOnlyList<string> Sources,
    OutputFormat OutputFormat,
    bool IncludeHidden,
    bool ContinueOnPreviewErrors,
    bool Silent,
    bool Verbose,
    string PresetsDirectory)
{
    public static CliOptions Parse(string[] args)
    {
        // Minimal parser: treat tokens as `/X:Value` or standalone `/FLAG` tokens.
        string? presetName = null;
        var sources = new List<string>();

        var outputFormat = OutputFormat.Table;
        var includeHidden = false;
        var continueOnPreviewErrors = false;
        var silent = false;
        var verbose = false;
        var presetsDirectory = PresetLoader.DefaultPresetsDirectory();

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (string.IsNullOrWhiteSpace(token))
                continue;

            if (token.StartsWith("/P:", StringComparison.OrdinalIgnoreCase))
            {
                presetName = token.Substring(3).Trim();
                continue;
            }

            if (token.StartsWith("/A:", StringComparison.OrdinalIgnoreCase))
            {
                sources.Add(token.Substring(3).Trim());
                continue;
            }

            if (token.Equals("/H+", StringComparison.OrdinalIgnoreCase))
            {
                includeHidden = true;
                continue;
            }

            if (token.Equals("/COPE", StringComparison.OrdinalIgnoreCase))
            {
                continueOnPreviewErrors = true;
                continue;
            }

            if (token.Equals("/S", StringComparison.OrdinalIgnoreCase))
            {
                silent = true;
                continue;
            }

            if (token.Equals("/V", StringComparison.OrdinalIgnoreCase))
            {
                verbose = true;
                continue;
            }

            if (token.StartsWith("--output", StringComparison.OrdinalIgnoreCase))
            {
                var parts = token.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    outputFormat = ParseOutputFormat(parts[1]);
                    continue;
                }

                if (i + 1 >= args.Length) throw new ArgumentException("--output requires a value.");
                outputFormat = ParseOutputFormat(args[++i]);
                continue;
            }

            if (token.Equals("--presets-dir", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length) throw new ArgumentException("--presets-dir requires a value.");
                presetsDirectory = args[++i];
                continue;
            }

            if (token.Equals("/Help", StringComparison.OrdinalIgnoreCase) || token.Equals("--help", StringComparison.OrdinalIgnoreCase) || token.Equals("/?"))
            {
                throw new InvalidOperationException("HELP");
            }

            // Unknown tokens are treated as errors in phase 1 to keep behavior deterministic.
            throw new InvalidOperationException($"Unknown argument '{token}'.");
        }

        if (string.IsNullOrWhiteSpace(presetName))
            throw new InvalidOperationException("Missing required '/P:<preset-name>'.");

        if (sources.Count == 0)
            throw new InvalidOperationException("Missing required '/A:<path>' sources.");

        return new CliOptions(
            PresetName: presetName!,
            Sources: sources,
            OutputFormat: outputFormat,
            IncludeHidden: includeHidden,
            ContinueOnPreviewErrors: continueOnPreviewErrors,
            Silent: silent,
            Verbose: verbose,
            PresetsDirectory: presetsDirectory);
    }

    private static OutputFormat ParseOutputFormat(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "table" => OutputFormat.Table,
            "json" => OutputFormat.Json,
            "csv" => OutputFormat.Csv,
            _ => throw new InvalidOperationException($"Unknown output format '{value}'. Use table|json|csv.")
        };
    }
}


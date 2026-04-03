using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Mfr8.Core;

namespace Mfr8.Cli;

public static class CliApp
{
    public static int Run(string[] args)
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

            if (!TryParseOutputFormat(output, out var outFormat))
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

            return Execute(options);
        });

        var parseResult = root.Parse(args);
        return parseResult.InvokeAsync().GetAwaiter().GetResult();
    }

    private static bool TryParseOutputFormat(string? value, out OutputFormat format)
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

    private static int Execute(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PresetsDirectory))
            return 1;

        FilterPreset preset;
        try
        {
            var loader = new PresetLoader(options.PresetsDirectory);
            preset = loader.Load(options.PresetName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 3;
        }

        IReadOnlyList<FileEntryLite> files;
        try
        {
            files = FileScanner.ScanSources(options.Sources, includeHidden: options.IncludeHidden);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        if (files.Count == 0)
        {
            Console.Error.WriteLine("No files matched the provided sources.");
            return 2;
        }

        var result = FilterEngine.PreviewAndCommit(
            preset: preset,
            files: files,
            continueOnErrors: options.ContinueOnPreviewErrors);

        if (!options.Silent)
            PrintResult(result, options.OutputFormat);

        if (result.Errors > 0 && !options.ContinueOnPreviewErrors)
            return 4;

        return 0;
    }

    private static void PrintResult(RenameBatchResult result, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Table:
                PrintTable(result);
                break;
            case OutputFormat.Json:
                PrintJson(result);
                break;
            case OutputFormat.Csv:
                PrintCsv(result);
                break;
            default:
                PrintTable(result);
                break;
        }
    }

    private static void PrintTable(RenameBatchResult result)
    {
        Console.WriteLine($"Preset: {result.PresetName}");
        Console.WriteLine($"Total: {result.TotalFiles}  Renamed: {result.Renamed}  Skipped: {result.Skipped}  Conflicts: {result.Conflicts}  Errors: {result.Errors}");
        Console.WriteLine();
        Console.WriteLine(string.Format("{0,-60} {1,-60} {2,-16} {3}", "Original", "Result", "Status", "Error"));

        foreach (var item in result.Results)
        {
            Console.WriteLine($"{Trunc(item.OriginalPath, 60),-60} {Trunc(item.ResultPath, 60),-60} {item.Status,-16} {item.Error ?? ""}");
        }
    }

    private static void PrintJson(RenameBatchResult result)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("preset", result.PresetName);
        writer.WriteNumber("totalFiles", result.TotalFiles);
        writer.WriteNumber("renamed", result.Renamed);
        writer.WriteNumber("skipped", result.Skipped);
        writer.WriteNumber("errors", result.Errors);
        writer.WriteNumber("conflicts", result.Conflicts);

        writer.WritePropertyName("results");
        writer.WriteStartArray();
        foreach (var r in result.Results)
        {
            writer.WriteStartObject();
            writer.WriteString("original", r.OriginalPath);
            writer.WriteString("result", r.ResultPath);
            writer.WriteString("status", r.Status.ToString());
            if (r.Error is null) writer.WriteNull("error");
            else writer.WriteString("error", r.Error);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.Flush();
        Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }

    private static void PrintCsv(RenameBatchResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("original,result,status,error");
        foreach (var item in result.Results)
        {
            sb.AppendLine($"{CsvEscape(item.OriginalPath)},{CsvEscape(item.ResultPath)},{CsvEscape(item.Status.ToString())},{CsvEscape(item.Error ?? "")}");
        }
        Console.WriteLine(sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string Trunc(string s, int max)
    {
        if (s.Length <= max) return s;
        return s.Substring(0, max);
    }
}


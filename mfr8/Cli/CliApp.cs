using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Mfr8.Core;

namespace Mfr8.Cli;

public static class CliApp
{
    public static int Run(string[] args)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (InvalidOperationException ex) when (ex.Message == "HELP")
        {
            PrintHelp();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Use --help for usage.");
            return 1;
        }

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
            Console.Error.WriteLine("No files matched the provided /A: sources.");
            return 2;
        }

        var result = FilterEngine.PreviewAndCommit(
            preset: preset,
            files: files,
            continueOnErrors: options.ContinueOnPreviewErrors);

        if (!options.Silent)
            PrintResult(result, options.OutputFormat);

        // Exit code policy (Phase 1):
        // - If /COPE is NOT set and we had preview/commit errors -> exit 4.
        // - Otherwise exit 0 (errors are reflected in the output).
        if (result.Errors > 0 && !options.ContinueOnPreviewErrors)
            return 4;

        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("mfr8 (CLI-only): rename using JSON presets and filename-only filters.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  mfr8 /P:<preset-name> /A:<path> [ /A:<path> ... ] [options]");
        Console.WriteLine();
        Console.WriteLine("Required:");
        Console.WriteLine("  /P:<preset-name>   Named preset (matches preset JSON 'name' or 'id').");
        Console.WriteLine("  /A:<path>          File, folder, or wildcard (repeatable).");
        Console.WriteLine();
        Console.WriteLine("Optional:");
        Console.WriteLine("  /H+                 Include hidden/system files.");
        Console.WriteLine("  /COPE               Continue on preview errors.");
        Console.WriteLine("  /S                   Silent mode (only exit code).");
        Console.WriteLine("  /V                   Verbose mode (reserved in phase 1).");
        Console.WriteLine("  --output <fmt>      table | json | csv. Default: table");
        Console.WriteLine("  --presets-dir <d>  Override preset directory (for development/testing).");
        Console.WriteLine();
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


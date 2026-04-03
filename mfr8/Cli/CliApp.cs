using System.Text;
using System.Text.Json;

using Mfr8.Core;

namespace Mfr8.Cli
{
    public static class CliApp
    {
        /// <summary>
        /// Runs the CLI entry point for <c>mfr8</c>.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The process exit code.</returns>
        public static CliExitCode Run(string[] args)
        {
            try
            {
                var options = CliCommandFactory.ParseArgs(args);
                return options is null ? CliExitCode.Success : _Execute(options);
            }
            catch (UserException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return CliExitCode.UserError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return CliExitCode.AppError;
            }
        }

        private static CliExitCode _Execute(CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.PresetsDirectory))
            {
                throw new UserException("Presets directory is required.");
            }

            var loader = new PresetLoader(options.PresetsDirectory);
            var preset = loader.Load(options.PresetName);

            var files = FileScanner.ScanSources(options.Sources, includeHidden: options.IncludeHidden);

            if (files.Count == 0)
            {
                throw new UserException("No files matched the provided sources.");
            }

            var result = FilterEngine.PreviewAndCommit(
                preset: preset,
                files: files,
                continueOnErrors: options.ContinueOnPreviewErrors);

            if (!options.Silent)
            {
                _PrintResult(result, options.OutputFormat);
            }

            return result.Errors > 0 && !options.ContinueOnPreviewErrors ? CliExitCode.UserError : CliExitCode.Success;
        }

        private static void _PrintResult(RenameBatchResult result, OutputFormat format)
        {
            switch (format)
            {
                case OutputFormat.Table:
                    _PrintTable(result);
                    break;
                case OutputFormat.Json:
                    _PrintJson(result);
                    break;
                case OutputFormat.Csv:
                    _PrintCsv(result);
                    break;
                default:
                    _PrintTable(result);
                    break;
            }
        }

        private static void _PrintTable(RenameBatchResult result)
        {
            Console.WriteLine($"Preset: {result.PresetName}");
            Console.WriteLine($"Total: {result.TotalFiles}  Renamed: {result.Renamed}  Skipped: {result.Skipped}  Conflicts: {result.Conflicts}  Errors: {result.Errors}");
            Console.WriteLine();
            Console.WriteLine(string.Format("{0,-60} {1,-60} {2,-16} {3}", "Original", "Result", "Status", "Error"));

            foreach (var item in result.Results)
            {
                Console.WriteLine($"{_Trunc(item.OriginalPath, 60),-60} {_Trunc(item.ResultPath, 60),-60} {item.Status,-16} {item.Error ?? ""}");
            }
        }

        private static void _PrintJson(RenameBatchResult result)
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
                if (r.Error is null)
                {
                    writer.WriteNull("error");
                }
                else
                {
                    writer.WriteString("error", r.Error);
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.Flush();
            Console.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
        }

        private static void _PrintCsv(RenameBatchResult result)
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine("original,result,status,error");
            foreach (var item in result.Results)
            {
                _ = sb.AppendLine($"{_CsvEscape(item.OriginalPath)},{_CsvEscape(item.ResultPath)},{_CsvEscape(item.Status.ToString())},{_CsvEscape(item.Error ?? "")}");
            }
            Console.WriteLine(sb.ToString());
        }

        private static string _CsvEscape(string value)
        {
            return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private static string _Trunc(string s, int max)
        {
            return s.Length <= max ? s : s[..max];
        }
    }

}

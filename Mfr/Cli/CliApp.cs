using System.Text;
using System.Text.Json;

using Mfr.Core;
using Mfr.Models;

namespace Mfr.Cli
{
    /// <summary>
    /// Provides the command-line entry point and output formatting for the Mfr application.
    /// </summary>
    public static class CliApp
    {
        /// <summary>
        /// Runs the CLI entry point for <c>Mfr</c>.
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
                _PrintError(ex.Message);
                return CliExitCode.UserError;
            }
            catch (Exception ex)
            {
                _PrintError(ex.ToString());
                return CliExitCode.AppError;
            }
        }

        private static CliExitCode _Execute(CliOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.PresetsFilePath))
            {
                throw new UserException("Presets file path is required.");
            }

            var presetManager = new PresetManager(options.PresetsFilePath);
            if (string.IsNullOrWhiteSpace(options.PresetName))
            {
                throw new UserException("Preset name is required.");
            }
            presetManager.LoadPresets();

            var preset = presetManager.NameToPreset.TryGetValue(options.PresetName, out var loadedPreset)
                ? loadedPreset
                : throw new UserException($"Preset not found: '{options.PresetName}'.");

            var renameList = new RenameList(includeHidden: options.IncludeHidden);
            renameList.AddSources(options.Sources);
            var renameItems = renameList.RenameItems;

            if (renameItems.Count == 0)
            {
                throw new UserException("No files matched the provided sources.");
            }

            FilterEngine.Preview(
                preset: preset,
                renameItems: renameItems,
                failFast: options.FailFast);
            var previewStats = _BuildPreviewResults(renameItems);
            var previewErrors = _CountErrors(previewStats);
            if (options.FailFast && previewErrors > 0)
            {
                _PrintResult(
                    presetName: preset.Name,
                    totalFiles: renameItems.Count,
                    results: previewStats,
                    format: options.OutputFormat,
                    silent: options.Silent);
                return CliExitCode.UserError;
            }

            var stats = FilterEngine.Commit(
                renameItem: renameItems,
                failFast: options.FailFast);

            _PrintResult(
                presetName: preset.Name,
                totalFiles: renameItems.Count,
                results: stats,
                format: options.OutputFormat,
                silent: options.Silent);
            return _CountErrors(stats) > 0 ? CliExitCode.UserError : CliExitCode.Success;
        }

        private static void _PrintResult(
            string presetName,
            int totalFiles,
            IReadOnlyList<RenameResultItem> results,
            OutputFormat format,
            bool silent)
        {
            if (silent)
            {
                return;
            }

            switch (format)
            {
                case OutputFormat.Table:
                    _PrintTable(presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
                case OutputFormat.Json:
                    _PrintJson(presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
                case OutputFormat.Csv:
                    _PrintCsv(results);
                    break;
                default:
                    _PrintTable(presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
            }
        }

        private static void _PrintTable(string presetName, int totalFiles, IReadOnlyList<RenameResultItem> results)
        {
            var errors = _CountErrors(results);
            var renamed = _CountStatus(results, RenameStatus.CommitOk);
            var skipped = _CountSkipped(results);
            var conflicts = _CountStatus(results, RenameStatus.CommitConflictSkipped);
            _PrintLine($"Preset: {presetName}");
            _PrintLine($"Total: {totalFiles}  Renamed: {renamed}  Skipped: {skipped}  Conflicts: {conflicts}  Errors: {errors}");
            _PrintLine(string.Empty);
            _PrintLine(string.Format("{0,-60} {1,-60} {2,-16} {3}", "Original", "Result", "Status", "Error"));

            foreach (var item in results)
            {
                _PrintLine($"{_Trunc(item.OriginalPath, 60),-60} {_Trunc(item.ResultPath, 60),-60} {item.Status,-16} {item.Error ?? ""}");
            }
        }

        private static void _PrintJson(string presetName, int totalFiles, IReadOnlyList<RenameResultItem> results)
        {
            var errors = _CountErrors(results);
            var renamed = _CountStatus(results, RenameStatus.CommitOk);
            var skipped = _CountSkipped(results);
            var conflicts = _CountStatus(results, RenameStatus.CommitConflictSkipped);
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();
            writer.WriteString("preset", presetName);
            writer.WriteNumber("totalFiles", totalFiles);
            writer.WriteNumber("renamed", renamed);
            writer.WriteNumber("skipped", skipped);
            writer.WriteNumber("errors", errors);
            writer.WriteNumber("conflicts", conflicts);

            writer.WritePropertyName("results");
            writer.WriteStartArray();
            foreach (var r in results)
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
            _PrintLine(Encoding.UTF8.GetString(ms.ToArray()));
        }

        private static void _PrintCsv(IReadOnlyList<RenameResultItem> results)
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine("original,result,status,error");
            foreach (var item in results)
            {
                _ = sb.AppendLine($"{_CsvEscape(item.OriginalPath)},{_CsvEscape(item.ResultPath)},{_CsvEscape(item.Status.ToString())},{_CsvEscape(item.Error ?? "")}");
            }
            _PrintLine(sb.ToString());
        }

        private static void _PrintLine(string text)
        {
            Console.WriteLine(text);
        }

        private static void _PrintError(string text)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(text);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
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

        private static int _CountErrors(IReadOnlyList<RenameResultItem> results)
        {
            return results.Count(item => item.Status is RenameStatus.PreviewError or RenameStatus.CommitError);
        }

        private static int _CountStatus(IReadOnlyList<RenameResultItem> results, RenameStatus status)
        {
            return results.Count(item => item.Status == status);
        }

        private static int _CountSkipped(IReadOnlyList<RenameResultItem> results)
        {
            return results.Count(item => item.Status is RenameStatus.PreviewNoChange or RenameStatus.CommitSkipped);
        }

        private static List<RenameResultItem> _BuildPreviewResults(IReadOnlyList<RenameItem> renameItems)
        {
            var results = new List<RenameResultItem>();
            foreach (var item in renameItems)
            {
                if (item.Preview is null && item.PreviewError is null)
                {
                    continue;
                }

                var resultPath = item.Preview?.FullPath ?? item.Original.FullPath;
                results.Add(new RenameResultItem(
                    OriginalPath: item.Original.FullPath,
                    ResultPath: resultPath,
                    Status: item.Status,
                    Error: item.PreviewError?.Message));
            }

            return results;
        }
    }

}

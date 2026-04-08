using System.Text;
using System.Text.Json;

using Mfr.Core;
using Mfr.Models;
using Mfr.Utils;
using Serilog;

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
            CliOptions? options;
            try
            {
                options = CliArgParser.ParseArgs(args);
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

            // help or version requested
            if (options is null)
            {
                return CliExitCode.Success;
            }

            using var loggerSession = CliLogging.Start(options.LogLevel, options.LogDirectoryPath);
            var logger = loggerSession.Logger;
            try
            {
                return _Execute(options, logger);
            }
            catch (UserException ex)
            {
                logger.Error("{Text}", ex.Message);
                return CliExitCode.UserError;
            }
            catch (Exception ex)
            {
                logger.Error("{Text}", ex.ToString());
                return CliExitCode.AppError;
            }
        }

        private static CliExitCode _Execute(CliOptions options, ILogger logger)
        {
            if (options.PresetsFilePath.IsBlank())
            {
                throw new UserException("Presets file path is required.");
            }

            var presetManager = new PresetManager(options.PresetsFilePath);
            if (options.PresetName.IsBlank())
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

            renameList.Preview(preset: preset);
            var previewResults = _BuildPreviewResults(renameItems);
            var previewErrors = _CountErrors(previewResults);
            if (previewErrors > 0)
            {
                _PrintResult(
                    logger: logger,
                    presetName: preset.Name,
                    totalFiles: renameItems.Count,
                    results: previewResults,
                    format: options.OutputFormat);
                return CliExitCode.UserError;
            }

            var commitFailFast = !options.ContinueOnRenameError;
            var renameResults = renameList.Commit(failFast: commitFailFast);

            _PrintResult(
                logger: logger,
                presetName: preset.Name,
                totalFiles: renameItems.Count,
                results: renameResults,
                format: options.OutputFormat);
            return _CountErrors(renameResults) > 0 ? CliExitCode.UserError : CliExitCode.Success;
        }

        private static void _PrintResult(
            ILogger logger,
            string presetName,
            int totalFiles,
            IReadOnlyList<RenameResultItem> results,
            OutputFormat format)
        {
            switch (format)
            {
                case OutputFormat.Table:
                    _PrintTable(logger: logger, presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
                case OutputFormat.Json:
                    _PrintJson(logger: logger, presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
                case OutputFormat.Csv:
                    _PrintCsv(logger: logger, results: results);
                    break;
                default:
                    _PrintTable(logger: logger, presetName: presetName, totalFiles: totalFiles, results: results);
                    break;
            }
        }

        private static void _PrintTable(ILogger logger, string presetName, int totalFiles, IReadOnlyList<RenameResultItem> results)
        {
            var errors = _CountErrors(results);
            var renamed = _CountStatus(results, RenameStatus.CommitOk);
            var skipped = _CountSkipped(results);
            _PrintLine(logger, $"Preset: {presetName}");
            _PrintLine(logger, $"Total: {totalFiles}  Renamed: {renamed}  Skipped: {skipped}  Errors: {errors}");
            _PrintLine(logger, string.Empty);
            _PrintLine(logger, string.Format("{0,-60} {1,-60} {2,-16} {3}", "Original", "Result", "Status", "Error"));

            foreach (var item in results)
            {
                _PrintLine(logger, $"{_Trunc(item.OriginalPath, 60),-60} {_Trunc(item.ResultPath, 60),-60} {item.Status,-16} {item.Error ?? ""}");
            }
        }

        private static void _PrintJson(ILogger logger, string presetName, int totalFiles, IReadOnlyList<RenameResultItem> results)
        {
            var errors = _CountErrors(results);
            var renamed = _CountStatus(results, RenameStatus.CommitOk);
            var skipped = _CountSkipped(results);
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();
            writer.WriteString("preset", presetName);
            writer.WriteNumber("totalFiles", totalFiles);
            writer.WriteNumber("renamed", renamed);
            writer.WriteNumber("skipped", skipped);
            writer.WriteNumber("errors", errors);

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
            _PrintLine(logger, Encoding.UTF8.GetString(ms.ToArray()));
        }

        private static void _PrintCsv(ILogger logger, IReadOnlyList<RenameResultItem> results)
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine("original,result,status,error");
            foreach (var item in results)
            {
                _ = sb.AppendLine($"{_CsvEscape(item.OriginalPath)},{_CsvEscape(item.ResultPath)},{_CsvEscape(item.Status.ToString())},{_CsvEscape(item.Error ?? "")}");
            }
            _PrintLine(logger, sb.ToString());
        }

        private static void _PrintLine(ILogger logger, string text)
        {
            logger.Information("{Text}", text);
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
            return results.Count(item => item.Status == RenameStatus.CommitSkipped);
        }

        private static List<RenameResultItem> _BuildPreviewResults(IReadOnlyList<RenameItem> renameItems)
        {
            var results = new List<RenameResultItem>();
            foreach (var item in renameItems)
            {
                // In preview fail-fast mode, untouched trailing items have no preview or error and should be omitted.
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

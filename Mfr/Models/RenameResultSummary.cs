using System.Text.Json;
using System.Text.Encodings.Web;

namespace Mfr.Models
{
    /// <summary>
    /// Represents the top-level rename execution result for JSON serialization.
    /// </summary>
    /// <param name="Preset">Preset name used for this execution.</param>
    /// <param name="TotalCount">Total number of resolved files.</param>
    /// <param name="RenamedCount">Count of successfully renamed files.</param>
    /// <param name="SkippedCount">Count of skipped files.</param>
    /// <param name="ErrorsCount">Count of files that ended with errors.</param>
    /// <param name="Results">Per-file rename results.</param>
    public sealed record RenameResultSummary(
        string Preset,
        int TotalCount,
        int RenamedCount,
        int SkippedCount,
        int ErrorsCount,
        IReadOnlyList<RenameResultItem> Results)
    {
        // Reused to avoid per-call allocations and CA1869 warnings when serializing output.
        private static readonly JsonSerializerOptions _jsonOutputSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// Creates a summary from per-item rename results and writes it to a JSON file.
        /// </summary>
        /// <param name="outputFilePath">Destination JSON file path.</param>
        /// <param name="presetName">Preset name used for the rename run.</param>
        /// <param name="results">Per-item rename results.</param>
        public static void WriteJsonFile(string outputFilePath, string presetName, IReadOnlyList<RenameResultItem> results)
        {
            var summary = new RenameResultSummary(
                Preset: presetName,
                TotalCount: results.Count,
                RenamedCount: _CountStatus(results, RenameStatus.CommitOk),
                SkippedCount: _CountStatus(results, RenameStatus.CommitSkipped),
                ErrorsCount: _CountErrors(results),
                Results: results);

            var json = JsonSerializer.Serialize(summary, _jsonOutputSerializerOptions);
            var outputFilePathTrimmed = outputFilePath.Trim();
            var outputDirectoryPath = Path.GetDirectoryName(outputFilePathTrimmed);
            if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                _ = Directory.CreateDirectory(outputDirectoryPath);
            }

            File.WriteAllText(outputFilePathTrimmed, json);
        }

        private static int _CountErrors(IReadOnlyList<RenameResultItem> results)
        {
            return results.Count(item => item.Status is RenameStatus.PreviewError or RenameStatus.CommitError);
        }

        private static int _CountStatus(IReadOnlyList<RenameResultItem> results, RenameStatus status)
        {
            return results.Count(item => item.Status == status);
        }
    }
}

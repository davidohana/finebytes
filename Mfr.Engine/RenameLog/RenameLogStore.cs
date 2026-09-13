using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Models.Config;
using Mfr.Utils;
using Serilog;
using RenameLogModel = Mfr.Models.Rename.RenameLog;

namespace Mfr.Engine.RenameLog
{
    /// <summary>
    /// Captures rename-commit outcomes into an in-memory last operation and optional on-disk <c>.mfrlog</c> files.
    /// <para>
    /// Disk files live under <see cref="DefaultDirectoryPath"/> (not diagnostic Serilog <c>logs/</c>).
    /// Retention uses <see cref="ConfigStore.RenameLog"/>.<see cref="RenameLogConfig.Limit"/>:
    /// <c>0</c> = memory only, <see cref="int.MaxValue"/> = unlimited, otherwise keep newest N.
    /// </para>
    /// </summary>
    public static class RenameLogStore
    {
        /// <summary>
        /// Filename extension for rename-commit log files, including the leading dot.
        /// </summary>
        public const string FileExtension = ".mfrlog";

        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly bool s_IsTestHostProcess = _DetectTestHostProcess();

        /// <summary>
        /// Default rename-log directory: <see cref="AppDataPaths.LocalRoot"/> + <c>rename-logs</c>.
        /// </summary>
        public static string DefaultDirectoryPath => AppDataPaths.LocalRoot().CombinePath("rename-logs");

        /// <summary>
        /// Gets the last captured rename operation for Undo Last, or <see langword="null"/> when none.
        /// </summary>
        public static RenameLogModel? LastOperation { get; private set; }

        /// <summary>
        /// Clears <see cref="LastOperation"/> (tests / process reset).
        /// </summary>
        public static void ClearLastOperation()
        {
            LastOperation = null;
        }

        /// <summary>
        /// Builds a log from successful commit rows, stores it as <see cref="LastOperation"/>, and optionally writes disk.
        /// <para>
        /// No-op when <paramref name="dryRun"/> is <see langword="true"/>, or when there are no
        /// <see cref="RenameStatus.CommitOk"/> rows (previous last op is left unchanged).
        /// </para>
        /// </summary>
        /// <param name="results">Per-item commit outcomes from <c>RenameList.Commit</c>.</param>
        /// <param name="dryRun">When <see langword="true"/>, skips capture entirely.</param>
        /// <param name="directoryPath">
        /// Override directory for disk files. When blank, <see cref="DefaultDirectoryPath"/> is used
        /// (skipped under the xUnit testhost so unit tests do not pollute AppData).
        /// </param>
        /// <param name="limit">
        /// Override retention. When <see langword="null"/>, uses <see cref="ConfigStore.RenameLog"/>.Limit.
        /// </param>
        /// <returns>The absolute path of the written <c>.mfrlog</c>, or <see langword="null"/> when not written.</returns>
        public static string? CaptureFromCommit(
            IReadOnlyList<RenameResultItem> results,
            bool dryRun = false,
            string? directoryPath = null,
            int? limit = null
        )
        {
            ArgumentNullException.ThrowIfNull(results);

            if (dryRun)
            {
                return null;
            }

            var log = TryBuildFromCommitResults(results);
            if (log is null)
            {
                return null;
            }

            LastOperation = log;

            var retention = limit ?? ConfigStore.RenameLog.Limit;
            if (retention <= 0)
            {
                return null;
            }

            // Default AppData writes are skipped under testhost; tests pass an explicit directoryPath.
            if (directoryPath.IsBlank() && s_IsTestHostProcess)
            {
                return null;
            }

            var resolvedDirectory = directoryPath.IsBlank() ? DefaultDirectoryPath : directoryPath.Trim();
            return _SaveAndTrim(log, resolvedDirectory, retention);
        }

        /// <summary>
        /// Builds a <see cref="RenameLogModel"/> from <see cref="RenameStatus.CommitOk"/> rows only.
        /// </summary>
        /// <param name="results">Per-item commit outcomes.</param>
        /// <returns>A log when at least one CommitOk row has a destination path; otherwise <see langword="null"/>.</returns>
        public static RenameLogModel? TryBuildFromCommitResults(IReadOnlyList<RenameResultItem> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            var entries = new List<RenameLogEntry>();
            foreach (var result in results)
            {
                if (result.Status != RenameStatus.CommitOk)
                {
                    continue;
                }

                if (result.DestinationPath.IsBlank())
                {
                    continue;
                }

                entries.Add(
                    new RenameLogEntry(
                        DestinationPath: result.DestinationPath,
                        OriginalPath: result.OriginalPath,
                        IsFolder: result.IsFolder,
                        Changes: result.Changes
                    )
                );
            }

            if (entries.Count == 0)
            {
                return null;
            }

            return new RenameLogModel(CommittedAt: DateTimeOffset.UtcNow, Entries: entries);
        }

        /// <summary>
        /// Deletes older <c>.mfrlog</c> files so at most <paramref name="maxFiles"/> remain.
        /// <para>
        /// Does nothing when the directory is missing, <paramref name="maxFiles"/> is less than 1,
        /// or <paramref name="maxFiles"/> is <see cref="int.MaxValue"/> (unlimited).
        /// </para>
        /// </summary>
        /// <param name="logDirectoryPath">Directory to prune.</param>
        /// <param name="maxFiles">Maximum files to keep (newest by creation time, then name).</param>
        public static void PruneFiles(string logDirectoryPath, int maxFiles)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectoryPath);

            if (!Directory.Exists(logDirectoryPath) || maxFiles < 1 || maxFiles == int.MaxValue)
            {
                return;
            }

            var logFilePaths = Directory
                .EnumerateFiles(logDirectoryPath, $"*{FileExtension}", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(fileInfo => fileInfo.CreationTimeUtc)
                .ThenByDescending(fileInfo => fileInfo.Name, StringComparer.Ordinal)
                .ToList();

            if (logFilePaths.Count <= maxFiles)
            {
                return;
            }

            foreach (var fileInfo in logFilePaths.Skip(maxFiles))
            {
                try
                {
                    fileInfo.Delete();
                    Log.Information("Deleted old rename log '{RenameLogPath}' during pruning.", fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        ex,
                        "Failed to delete old rename log '{RenameLogPath}' during pruning.",
                        fileInfo.FullName
                    );
                }
            }
        }

        /// <summary>
        /// Writes <paramref name="log"/> under <paramref name="logDirectoryPath"/> and prunes to
        /// <paramref name="retention"/>.
        /// </summary>
        /// <returns>The written file path, or <see langword="null"/> when the write failed.</returns>
        private static string? _SaveAndTrim(RenameLogModel log, string logDirectoryPath, int retention)
        {
            try
            {
                Directory.CreateDirectory(logDirectoryPath);
                var filePath = _CreateUniqueFilePath(logDirectoryPath);
                var json = JsonSerializer.Serialize(log, s_JsonOptions);
                File.WriteAllText(filePath, json);
                PruneFiles(logDirectoryPath, retention);
                Log.Debug("Wrote rename log '{RenameLogPath}'.", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to write rename log under '{RenameLogDirectory}'.", logDirectoryPath);
                return null;
            }
        }

        /// <summary>
        /// Builds <c>yyyyMMddHHmmss.mfrlog</c>, appending <c>-N</c> on same-second collisions.
        /// </summary>
        private static string _CreateUniqueFilePath(string logDirectoryPath)
        {
            var stamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
            var candidate = logDirectoryPath.CombinePath(stamp + FileExtension);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            for (var suffix = 1; ; suffix++)
            {
                candidate = logDirectoryPath.CombinePath($"{stamp}-{suffix}{FileExtension}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        /// <summary>
        /// True when the entry assembly name contains <c>testhost</c> (xUnit), so default AppData
        /// disk writes are skipped unless an explicit directory is passed.
        /// </summary>
        private static bool _DetectTestHostProcess()
        {
            var entryName = Assembly.GetEntryAssembly()?.GetName().Name;
            return entryName is not null && entryName.Contains("testhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}

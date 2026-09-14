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
            // Compact JSON: large GO/Undo logs (thousands of entries) stay cheap to write and reload.
            WriteIndented = false,
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
        /// Absolute path of the <c>.mfrlog</c> written for the current <see cref="LastOperation"/>, or
        /// <see langword="null"/> when the last undoable capture skipped disk (limit 0) or none exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used by the Rename Log dialog to dedupe the in-memory row without deserializing every retained
        /// disk log, and to seed the matching disk row with the in-memory instance.
        /// </para>
        /// </remarks>
        public static string? LastWrittenFilePath { get; private set; }

        /// <summary>
        /// Clears <see cref="LastOperation"/> and <see cref="LastWrittenFilePath"/> (tests / process reset).
        /// </summary>
        public static void ClearLastOperation()
        {
            LastOperation = null;
            LastWrittenFilePath = null;
        }

        /// <summary>
        /// Lists on-disk <c>.mfrlog</c> paths newest-first (by filename stamp, then name).
        /// </summary>
        /// <param name="directoryPath">
        /// Override directory. When blank, <see cref="DefaultDirectoryPath"/> is used.
        /// </param>
        /// <returns>Absolute file paths; empty when the directory is missing or has no logs.</returns>
        public static IReadOnlyList<string> ListDiskFilePaths(string? directoryPath = null)
        {
            var resolvedDirectory = directoryPath.IsBlank() ? DefaultDirectoryPath : directoryPath.Trim();
            if (!Directory.Exists(resolvedDirectory))
            {
                return [];
            }

            return
            [
                .. Directory
                    .EnumerateFiles(resolvedDirectory, $"*{FileExtension}", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal),
            ];
        }

        /// <summary>
        /// Loads a rename log from a JSON <c>.mfrlog</c> file.
        /// </summary>
        /// <param name="filePath">Absolute path to the log file.</param>
        /// <returns>The deserialized log, or <see langword="null"/> when the file is missing or invalid.</returns>
        public static RenameLogModel? TryLoadFile(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                using var stream = File.OpenRead(filePath);
                var log = JsonSerializer.Deserialize<RenameLogModel>(stream, s_JsonOptions);
                if (log?.Entries is null)
                {
                    Log.Warning("Rename log '{RenameLogPath}' is missing entries.", filePath);
                    return null;
                }

                return log;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to read rename log '{RenameLogPath}'.", filePath);
                return null;
            }
        }

        /// <summary>
        /// Deletes an on-disk rename log file.
        /// </summary>
        /// <param name="filePath">Absolute path to the <c>.mfrlog</c> file.</param>
        /// <returns>
        /// <see langword="true"/> when the file was deleted or already absent;
        /// <see langword="false"/> when delete failed.
        /// </returns>
        public static bool TryDeleteFile(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to erase rename log '{RenameLogPath}'.", filePath);
                return false;
            }
        }

        /// <summary>
        /// Builds a log from commit outcomes, stores undoable logs as <see cref="LastOperation"/>, and optionally writes disk.
        /// <para>
        /// No-op when <paramref name="dryRun"/> is <see langword="true"/>, or when there are no
        /// <see cref="RenameStatus.CommitOk"/> / <see cref="RenameStatus.CommitError"/> rows to log.
        /// Assigns <see cref="LastOperation"/> only when the built log has at least one undoable entry
        /// (errors-only captures leave a prior last op unchanged). When retention &gt; 0, still writes
        /// <c>.mfrlog</c> for any non-null log, including errors-only.
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
        /// <param name="isUndo">
        /// When <see langword="true"/>, marks the log as an Undo operation (shown in details as Undo).
        /// </param>
        /// <returns>The absolute path of the written <c>.mfrlog</c>, or <see langword="null"/> when not written.</returns>
        public static string? CaptureFromCommit(
            IReadOnlyList<RenameResultItem> results,
            bool dryRun = false,
            string? directoryPath = null,
            int? limit = null,
            bool isUndo = false
        )
        {
            ArgumentNullException.ThrowIfNull(results);

            if (dryRun)
            {
                return null;
            }

            var log = TryBuildFromCommitResults(results, isUndo: isUndo);
            if (log is null)
            {
                return null;
            }

            if (log.HasUndoableEntries)
            {
                LastOperation = log;
                // Cleared until a successful write below so limit-0 / failed writes do not keep a stale path.
                LastWrittenFilePath = null;
            }

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
            var writtenPath = _SaveAndTrim(log, resolvedDirectory, retention);
            if (writtenPath is not null && ReferenceEquals(LastOperation, log))
            {
                LastWrittenFilePath = writtenPath;
            }

            return writtenPath;
        }

        /// <summary>
        /// Builds a <see cref="RenameLogModel"/> from <see cref="RenameStatus.CommitOk"/> and
        /// <see cref="RenameStatus.CommitError"/> rows (skips skipped / preview-error).
        /// </summary>
        /// <param name="results">Per-item commit outcomes.</param>
        /// <param name="isUndo">When <see langword="true"/>, sets <see cref="RenameLogModel.IsUndo"/>.</param>
        /// <returns>
        /// A log when at least one CommitOk (non-blank destination) or CommitError row is present;
        /// otherwise <see langword="null"/>.
        /// </returns>
        public static RenameLogModel? TryBuildFromCommitResults(
            IReadOnlyList<RenameResultItem> results,
            bool isUndo = false
        )
        {
            ArgumentNullException.ThrowIfNull(results);

            var entries = new List<RenameLogEntry>();
            foreach (var result in results)
            {
                if (result.Status == RenameStatus.CommitOk)
                {
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
                    continue;
                }

                if (result.Status != RenameStatus.CommitError)
                {
                    continue;
                }

                var destinationPath = result.DestinationPath.IsBlank() ? result.OriginalPath : result.DestinationPath;
                entries.Add(
                    new RenameLogEntry(
                        DestinationPath: destinationPath,
                        OriginalPath: result.OriginalPath,
                        IsFolder: result.IsFolder,
                        Changes: result.Changes,
                        Error: result.Error
                    )
                );
            }

            if (entries.Count == 0)
            {
                return null;
            }

            return new RenameLogModel(CommittedAt: DateTimeOffset.UtcNow, Entries: entries, IsUndo: isUndo);
        }

        /// <summary>
        /// Deletes older <c>.mfrlog</c> files so at most <paramref name="maxFiles"/> remain.
        /// <para>
        /// Does nothing when the directory is missing or <paramref name="maxFiles"/> is
        /// <see cref="int.MaxValue"/> (unlimited). When <paramref name="maxFiles"/> is <c>0</c> or
        /// less, deletes all <c>.mfrlog</c> files (Options Disabled / MFR7 LogLimit 0 trim).
        /// </para>
        /// </summary>
        /// <param name="logDirectoryPath">Directory to prune.</param>
        /// <param name="maxFiles">
        /// Maximum files to keep (newest by creation time, then name). <c>0</c> or less deletes all;
        /// <see cref="int.MaxValue"/> leaves all files.
        /// </param>
        public static void PruneFiles(string logDirectoryPath, int maxFiles)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectoryPath);

            if (maxFiles == int.MaxValue)
            {
                return;
            }

            NewestFilesPruner.PruneByCreationTimeUtc(
                directoryPath: logDirectoryPath,
                keepCount: Math.Max(0, maxFiles),
                searchPattern: $"*{FileExtension}",
                onDeleted: path => Log.Information("Deleted old rename log '{RenameLogPath}' during pruning.", path),
                onFailed: (path, ex) =>
                    Log.Warning(ex, "Failed to delete old rename log '{RenameLogPath}' during pruning.", path)
            );
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
                using (var stream = File.Create(filePath))
                {
                    JsonSerializer.Serialize(stream, log, s_JsonOptions);
                }

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

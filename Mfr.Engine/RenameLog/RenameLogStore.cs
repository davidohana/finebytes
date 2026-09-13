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

                var json = File.ReadAllText(filePath);
                var log = JsonSerializer.Deserialize<RenameLogModel>(json, s_JsonOptions);
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
        /// Builds the Rename Log list title for a commit time (MFR7 <c>dd/MM/yyyy HH:mm:ss</c> local).
        /// </summary>
        /// <param name="committedAt">Commit timestamp (typically UTC from <see cref="RenameLogModel.CommittedAt"/>).</param>
        /// <returns>Local-time list title matching disk rows and the details pane date line.</returns>
        public static string FormatListTitle(DateTimeOffset committedAt)
        {
            return committedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }

        /// <summary>
        /// Builds the Rename Log list title for a disk file (MFR7 <c>dd/MM/yyyy HH:mm:ss</c> from stamp).
        /// </summary>
        /// <param name="filePath">Absolute or relative <c>.mfrlog</c> path.</param>
        /// <returns>Formatted stamp when the stem is <c>yyyyMMddHHmmss</c> (optional <c>-N</c>); otherwise the stem.</returns>
        public static string FormatDiskListTitle(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var stem = Path.GetFileNameWithoutExtension(filePath);
            var stamp = stem;
            var dashIndex = stem.IndexOf('-');
            if (dashIndex > 0)
            {
                stamp = stem[..dashIndex];
            }

            if (stamp.Length != 14 || !stamp.All(char.IsDigit))
            {
                return stem;
            }

            return string.Create(
                19,
                stamp,
                static (span, value) =>
                {
                    span[0] = value[6];
                    span[1] = value[7];
                    span[2] = '/';
                    span[3] = value[4];
                    span[4] = value[5];
                    span[5] = '/';
                    span[6] = value[0];
                    span[7] = value[1];
                    span[8] = value[2];
                    span[9] = value[3];
                    span[10] = ' ';
                    span[11] = value[8];
                    span[12] = value[9];
                    span[13] = ':';
                    span[14] = value[10];
                    span[15] = value[11];
                    span[16] = ':';
                    span[17] = value[12];
                    span[18] = value[13];
                }
            );
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

            if (!Directory.Exists(logDirectoryPath) || maxFiles == int.MaxValue)
            {
                return;
            }

            var keepCount = Math.Max(0, maxFiles);
            var logFilePaths = Directory
                .EnumerateFiles(logDirectoryPath, $"*{FileExtension}", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(fileInfo => fileInfo.CreationTimeUtc)
                .ThenByDescending(fileInfo => fileInfo.Name, StringComparer.Ordinal)
                .ToList();

            if (logFilePaths.Count <= keepCount)
            {
                return;
            }

            foreach (var fileInfo in logFilePaths.Skip(keepCount))
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

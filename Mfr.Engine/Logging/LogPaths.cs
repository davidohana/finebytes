using Mfr.Models.Config;
using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.Logging
{
    /// <summary>
    /// Path and on-disk helpers for diagnostic session and crash log files.
    /// <para>
    /// CLI and UI session logs write under <see cref="DefaultDirectoryPath"/> unless
    /// <see cref="LogConfig.DirectoryPath"/> is set. Crash files always use the default directory.
    /// These helpers do not configure Serilog; hosts assign <c>Log.Logger</c> via <see cref="LogSession.Start"/>.
    /// </para>
    /// </summary>
    public static class LogPaths
    {
        /// <summary>
        /// Filename prefix for best-effort crash files written when Serilog is not running.
        /// </summary>
        public const string CrashFilePrefix = "crash-";

        /// <summary>
        /// Default diagnostic log directory:
        /// <see cref="AppDataPaths.LocalRoot"/> + <c>logs</c>.
        /// </summary>
        public static string DefaultDirectoryPath => AppDataPaths.LocalRoot().CombinePath("logs");

        /// <summary>
        /// Resolves the directory used for session log files.
        /// </summary>
        /// <param name="configuredLogDirectoryPath">
        /// Override directory. When blank, <see cref="DefaultDirectoryPath"/> is used.
        /// </param>
        /// <returns>The trimmed override path, or the default directory.</returns>
        public static string ResolveDirectoryPath(string? configuredLogDirectoryPath)
        {
            if (!configuredLogDirectoryPath.IsBlank())
                return configuredLogDirectoryPath.Trim();

            return DefaultDirectoryPath;
        }

        /// <summary>
        /// Builds a new per-session log file path using <see cref="LogConfig"/> naming.
        /// </summary>
        /// <param name="logDirectoryPath">Directory that will hold the file (not created here).</param>
        /// <param name="logConfig">Prefix and extension for the session file name.</param>
        /// <returns>Full path for a new session log file.</returns>
        public static string CreateSessionFilePath(string logDirectoryPath, LogConfig logConfig)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectoryPath);
            ArgumentNullException.ThrowIfNull(logConfig);

            return _CreateTimestampedFilePath(
                logDirectoryPath: logDirectoryPath,
                prefix: logConfig.FilePrefix,
                extension: logConfig.FileExtension
            );
        }

        /// <summary>
        /// Deletes older session log files so at most <paramref name="maxSessionFiles"/> remain.
        /// <para>
        /// Logs each deleted file and each delete failure. Does nothing when the directory is
        /// missing or <paramref name="maxSessionFiles"/> is less than 1.
        /// </para>
        /// </summary>
        /// <param name="logDirectoryPath">Directory to prune.</param>
        /// <param name="maxSessionFiles">Maximum files to keep (newest by creation time, then name).</param>
        /// <param name="sessionLogPrefix">Filename prefix to match (e.g. <c>session-</c>).</param>
        /// <param name="sessionLogExtension">Filename extension to match (e.g. <c>.log</c>).</param>
        public static void PruneSessionFiles(
            string logDirectoryPath,
            int maxSessionFiles,
            string sessionLogPrefix,
            string sessionLogExtension
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectoryPath);

            if (!Directory.Exists(logDirectoryPath) || maxSessionFiles < 1)
                return;

            var prefix = sessionLogPrefix ?? string.Empty;
            var extension = sessionLogExtension ?? string.Empty;
            var sessionLogFilePaths = Directory
                .EnumerateFiles(logDirectoryPath, $"{prefix}*{extension}", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(fileInfo => fileInfo.CreationTimeUtc)
                .ThenByDescending(fileInfo => fileInfo.Name, StringComparer.Ordinal)
                .ToList();

            if (sessionLogFilePaths.Count <= maxSessionFiles)
                return;

            foreach (var fileInfo in sessionLogFilePaths.Skip(maxSessionFiles))
            {
                try
                {
                    fileInfo.Delete();
                    Log.Information("Deleted old log file '{LogFilePath}' during pruning.", fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete old log file '{LogFilePath}' during pruning.", fileInfo.FullName);
                }
            }
        }

        /// <summary>
        /// Formats user-facing crash text from an exception, including inner exceptions.
        /// </summary>
        /// <param name="exception">The fault to describe.</param>
        /// <returns>A terminating header plus <see cref="Exception.ToString"/>.</returns>
        public static string FormatCrashText(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var header = "An unexpected error occurred." + Environment.NewLine + "Application will be terminated.";
            return header + Environment.NewLine + Environment.NewLine + exception.ToString();
        }

        /// <summary>
        /// Best-effort write of a <c>crash-*.log</c> file under <see cref="DefaultDirectoryPath"/>.
        /// </summary>
        /// <param name="exception">The fault to persist.</param>
        /// <returns>The written file path, or <c>null</c> when the write failed.</returns>
        public static string? TryWriteCrashFile(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            try
            {
                var directoryPath = DefaultDirectoryPath;
                Directory.CreateDirectory(directoryPath);
                var crashFilePath = _CreateTimestampedFilePath(
                    logDirectoryPath: directoryPath,
                    prefix: CrashFilePrefix,
                    extension: ".log"
                );
                File.WriteAllText(crashFilePath, FormatCrashText(exception));
                return crashFilePath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string _CreateTimestampedFilePath(string logDirectoryPath, string prefix, string extension)
        {
            var fileName = $"{prefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}{extension}";
            return logDirectoryPath.CombinePath(fileName);
        }
    }
}

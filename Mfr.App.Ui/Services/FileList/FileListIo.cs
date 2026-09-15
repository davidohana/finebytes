namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Shared File List filesystem probes with network timeouts.
    /// </summary>
    internal static class FileListIo
    {
        // Caps how long a disconnected UNC or mapped drive may block Exists/enumerate.
        // The OS SMB timeout cannot be cancelled; this bound keeps File List from waiting forever
        // after async listing already keeps the UI responsive.
        public static readonly TimeSpan NetworkProbeTimeout = TimeSpan.FromSeconds(15);

        // First contact with a UNC server (\\ohanas) is often slower than a share Exists check.
        public static readonly TimeSpan UncServerProbeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether <paramref name="path"/> exists as a directory, with a timeout on UNC/network drives.
        /// </summary>
        /// <param name="path">Filesystem folder path.</param>
        /// <returns><see langword="false"/> when missing or the probe times out.</returns>
        public static bool DirectoryExists(string path)
        {
            if (!NeedsNetworkTimeout(path))
            {
                return Directory.Exists(path);
            }

            return TryRunWithTimeout(() => Directory.Exists(path), NetworkProbeTimeout, out var exists) && exists;
        }

        /// <summary>
        /// Whether Exists/enumerate of <paramref name="path"/> should use a network timeout.
        /// </summary>
        /// <param name="path">Filesystem path.</param>
        /// <returns><see langword="true"/> for UNC paths and mapped network drives.</returns>
        public static bool NeedsNetworkTimeout(string path)
        {
            if (FileListPath.IsUncPath(path))
            {
                return true;
            }

            try
            {
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                {
                    return false;
                }

                return new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Runs <paramref name="action"/> on a thread-pool thread and abandons it after <paramref name="timeout"/>.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="action">Work that may block on SMB/IO.</param>
        /// <param name="timeout">Maximum wait before treating the probe as failed.</param>
        /// <param name="result">Action result when completed in time.</param>
        /// <returns><see langword="false"/> on timeout or fault.</returns>
        public static bool TryRunWithTimeout<T>(Func<T> action, TimeSpan timeout, out T result)
        {
            var task = Task.Run(action);
            try
            {
                if (task.Wait(timeout))
                {
                    result = task.Result;
                    return true;
                }
            }
            catch (AggregateException)
            {
                result = default!;
                return false;
            }

            // Exists/enumerate cannot be cancelled; observe later faults so they are not unhandled.
            _ = task.ContinueWith(
                static completed => completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            result = default!;
            return false;
        }
    }
}

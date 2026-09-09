namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Optional overrides for Tools → Reset Configuration (headless tests; leave null in production).
    /// </summary>
    internal sealed class ResetConfigurationHooks
    {
        /// <summary>
        /// When set, replaces the confirm dialog; return <see langword="true"/> to proceed.
        /// </summary>
        public Func<Task<bool>>? Confirm { get; init; }

        /// <summary>
        /// When set, replaces AppData file deletion (and in-memory defaults clear).
        /// </summary>
        public Action? DeletePersistedConfiguration { get; init; }

        /// <summary>
        /// When set, replaces executable path resolution for the restart process.
        /// </summary>
        public Func<string?>? ResolveExecutablePath { get; init; }

        /// <summary>
        /// When set, replaces starting the replacement process.
        /// </summary>
        public Action<string>? StartProcess { get; init; }

        /// <summary>
        /// When set, replaces app shutdown after a successful restart spawn.
        /// </summary>
        public Action? Shutdown { get; init; }
    }
}

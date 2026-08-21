using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// View model for the unexpected-error dialog.
    /// </summary>
    /// <remarks>
    /// Initializes dialog text from a persisted crash report.
    /// </remarks>
    /// <param name="details">Full exception text including inner exceptions.</param>
    /// <param name="logFilePath">Session or crash log path, when written.</param>
    /// <param name="logDirectoryPath">Directory to open from the dialog.</param>
    public sealed class CrashDialogViewModel(
        string details,
        string? logFilePath,
        string logDirectoryPath)
    {
        /// <summary>
        /// Gets the short user-facing summary.
        /// </summary>
        public string Summary { get; } = "An unexpected error occurred. Application will be terminated.";

        /// <summary>
        /// Gets the full exception details for copy/display.
        /// </summary>
        public string Details { get; } = details;

        /// <summary>
        /// Gets the log file path, or <c>null</c> when none was written.
        /// </summary>
        public string? LogFilePath { get; } = logFilePath;

        /// <summary>
        /// Gets text to show for the log file (or a fallback when missing).
        /// </summary>
        public string LogFileDisplay { get; } = logFilePath.IsBlank()
                ? "Diagnostic log was not written."
                : logFilePath;

        /// <summary>
        /// Gets the diagnostic log directory.
        /// </summary>
        public string LogDirectoryPath { get; } = logDirectoryPath;

        /// <summary>
        /// Gets whether <see cref="LogDirectoryPath"/> can be opened.
        /// </summary>
        public bool HasLogDirectory { get; } = !logDirectoryPath.IsBlank();
    }
}

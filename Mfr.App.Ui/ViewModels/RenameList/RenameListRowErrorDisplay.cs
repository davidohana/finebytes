using System.Text;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Shared copy helpers for the Rename List row-error dialog.
    /// </summary>
    internal static class RenameListRowErrorDisplay
    {
        /// <summary>
        /// Builds the primary details box text (file path and user message).
        /// </summary>
        /// <param name="filePath">Absolute path of the errored row.</param>
        /// <param name="userMessage">Plain-language explanation.</param>
        /// <returns>Path, blank line, then message.</returns>
        internal static string FormatPrimaryDetails(string filePath, string userMessage)
        {
            return string.Join(Environment.NewLine, filePath, string.Empty, userMessage);
        }

        /// <summary>
        /// Builds clipboard text (summary, path, user message, and technical details when present).
        /// </summary>
        /// <param name="content">Dialog content.</param>
        /// <returns>Multi-line text suitable for copy/paste.</returns>
        internal static string FormatCopyText(RenameListRowErrorDialogContent content)
        {
            var lines = new List<string>
            {
                content.Summary,
                string.Empty,
                FormatPrimaryDetails(content.FilePath, content.UserMessage),
            };
            if (!string.IsNullOrWhiteSpace(content.TechnicalDetails))
            {
                lines.Add(string.Empty);
                lines.Add(content.TechnicalDetails);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Formats an exception chain for the Technical details expander (type, message, stack).
        /// </summary>
        /// <param name="exception">Root exception, or <see langword="null"/>.</param>
        /// <returns>Multi-line diagnostic text, or <see langword="null"/> when <paramref name="exception"/> is null.</returns>
        internal static string? FormatExceptionDetails(Exception? exception)
        {
            if (exception is null)
            {
                return null;
            }

            var builder = new StringBuilder();
            _AppendException(builder, exception);
            return builder.ToString().TrimEnd();
        }

        private static void _AppendException(StringBuilder builder, Exception exception)
        {
            builder.Append("Type: ");
            builder.AppendLine(exception.GetType().FullName);
            builder.Append("Message: ");
            builder.AppendLine(exception.Message);
            builder.AppendLine("Stack Trace:");
            builder.AppendLine(exception.StackTrace ?? string.Empty);
            builder.AppendLine("-----");

            if (exception.InnerException is not null)
            {
                _AppendException(builder, exception.InnerException);
            }
        }
    }
}

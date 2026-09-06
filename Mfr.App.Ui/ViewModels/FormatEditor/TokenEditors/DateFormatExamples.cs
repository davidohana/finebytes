namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Shared .NET date/time format suggestions for format-token editors.
    /// </summary>
    internal static class DateFormatExamples
    {
        /// <summary>
        /// Public Microsoft Learn reference for custom date/time format strings.
        /// </summary>
        public static Uri DocsUri { get; } =
            new("https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings");

        /// <summary>
        /// Link label for <see cref="DocsUri"/>.
        /// </summary>
        public const string DocsLinkText = "Date and time format strings guide";

        /// <summary>
        /// Gets common custom and filename-friendly date/time format strings.
        /// </summary>
        public static IReadOnlyList<string> All { get; } =
        [
            "yyyy-MM-dd",
            "yyyy-MM-dd_HH-mm-ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyyMMdd",
            "yyyyMMdd_HHmmss",
            "dd-MM-yyyy",
            "dd-MM-yyyy hh_mm_ss",
            "MM/dd/yyyy",
            "yyyy",
            "hh_mm_ss",
            "HH:mm:ss",
            "s",
            "g",
            "G",
        ];
    }
}

namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// How <see cref="FormatStringSyntax.TryValidate(string, FormatStringValidationMode)"/> treats text that may contain formatter tokens.
    /// </summary>
    public enum FormatStringValidationMode
    {
        /// <summary>
        /// Always walk and compile balanced <c>&lt;…&gt;</c> spans (Formatter / PathMover / Name List templates).
        /// </summary>
        Always = 0,

        /// <summary>
        /// Validate only when the text contains at least one likely token name; otherwise treat the whole
        /// string as a literal (Inserter / Audio Tag Setter / ID3v2 Field Setter).
        /// </summary>
        WhenLikelyTokens = 1,
    }
}

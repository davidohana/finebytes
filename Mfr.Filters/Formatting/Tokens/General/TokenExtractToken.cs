using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Resolves the <c>&lt;token:token-number,separator,include-next,include-prev,source-format-string&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Splits the resolved <c>source-format-string</c> by <c>separator</c> and extracts the
    /// 1-based <c>token-number</c> part. When <c>include-next</c> is <c>1</c>, all parts from
    /// <c>token-number</c> to the end are rejoined. When <c>include-prev</c> is <c>1</c>, all
    /// parts from the start through <c>token-number</c> are rejoined. When both flags are <c>1</c>
    /// the full source string is returned.
    /// </para>
    /// <para>
    /// <c>source-format-string</c> may itself be a nested format token such as <c>&lt;full-name&gt;</c>;
    /// it is resolved before the split is applied.
    /// </para>
    /// </remarks>
    internal sealed class TokenExtractToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;token:...&gt;</c>.
        /// </summary>
        /// <param name="TokenNumber">1-based index of the token to extract.</param>
        /// <param name="Separator">String used to split the source into parts.</param>
        /// <param name="IncludeNext">When <see langword="true"/>, returns from <paramref name="TokenNumber"/> to the end of the source.</param>
        /// <param name="IncludePrev">When <see langword="true"/>, returns from the start of the source up to and including <paramref name="TokenNumber"/>.</param>
        /// <param name="SourceFormatString">Raw format string (may contain nested tokens) to extract from.</param>
        private sealed record Options(
            int TokenNumber,
            string Separator,
            bool IncludeNext,
            bool IncludePrev,
            string SourceFormatString);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["token"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when arguments are invalid or <c>token-number</c> exceeds the number of parts in the source.
        /// </exception>
        public string Resolve(string arg, RenameItem item)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var options = _ParseOptions(arg);
            var source = FormatStringResolver.ResolveTemplate(options.SourceFormatString, item);
            var parts = source.Split(options.Separator, StringSplitOptions.None);

            if (options.TokenNumber > parts.Length)
                throw new InvalidOperationException(
                    $"{tokenDisplayName} token-number {options.TokenNumber} exceeds the number of parts ({parts.Length}) " +
                    $"in '{source}' when split by '{options.Separator}'.");

            if (options.IncludeNext && options.IncludePrev)
                return source;

            if (options.IncludeNext)
                return string.Join(options.Separator, parts[(options.TokenNumber - 1)..]);

            if (options.IncludePrev)
                return string.Join(options.Separator, parts[..options.TokenNumber]);

            return parts[options.TokenNumber - 1];
        }

        private Options _ParseOptions(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            if (string.IsNullOrEmpty(arg))
                throw new InvalidOperationException(
                    $"{tokenDisplayName} requires 5 arguments: token-number,separator,include-next,include-prev,source-format-string.");

            var parts = arg.Split(',', 5);
            if (parts.Length != 5)
                throw new InvalidOperationException(
                    $"{tokenDisplayName} requires exactly 5 comma-separated arguments (got {parts.Length}): '{arg}'.");

            var tokenNumber = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            var separator = parts[1];
            var includeNext = parts[2].Trim() == "1";
            var includePrev = parts[3].Trim() == "1";
            var sourceFormatString = parts[4];

            if (tokenNumber < 1)
                throw new InvalidOperationException(
                    $"{tokenDisplayName} token-number must be 1 or greater (got {tokenNumber}).");

            if (string.IsNullOrEmpty(separator))
                throw new InvalidOperationException(
                    $"{tokenDisplayName} separator must not be empty.");

            return new Options(
                TokenNumber: tokenNumber,
                Separator: separator,
                IncludeNext: includeNext,
                IncludePrev: includePrev,
                SourceFormatString: sourceFormatString);
        }
    }
}

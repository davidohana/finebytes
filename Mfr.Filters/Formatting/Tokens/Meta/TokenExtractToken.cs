using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Resolves the <c>&lt;token:token-number,separator,include-next,include-prev,source-format-string&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Splits the resolved <c>source-format-string</c> by <c>separator</c> and extracts the
    /// 1-based <c>token-number</c> part. <c>include-next</c> and <c>include-prev</c> are <c>true</c> or <c>false</c> (case-insensitive).
    /// When include-next is true, all parts from <c>token-number</c> to the end are rejoined.
    /// When include-prev is true, all parts from the start through <c>token-number</c> are rejoined.
    /// When both are true the full source string is returned.
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

        private static readonly string[] _includeFlagKeywords = ["true", "false"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is invalid or <c>token-number</c> is inconsistent with resolved source text.</exception>
        public Func<RenameItem, string> Compile(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var options = _ParseOptions(tokenDisplayName, arg);
            var compiledSource = FormatStringCompiler.Compile(options.SourceFormatString);
            return item =>
            {
                var source = compiledSource(item);
                var parts = source.Split(options.Separator, StringSplitOptions.None);

                Require.That(
                    options.TokenNumber <= parts.Length,
                    $"{tokenDisplayName} token-number {options.TokenNumber} exceeds the number of parts ({parts.Length}) " +
                        $"in '{source}' when split by '{options.Separator}'.",
                    nameof(arg));

                if (options.IncludeNext && options.IncludePrev)
                    return source;

                if (options.IncludeNext)
                    return string.Join(options.Separator, parts[(options.TokenNumber - 1)..]);

                if (options.IncludePrev)
                    return string.Join(options.Separator, parts[..options.TokenNumber]);

                return parts[options.TokenNumber - 1];
            };
        }

        private Options _ParseOptions(string tokenDisplayName, string arg)
        {
            Require.That(!string.IsNullOrEmpty(arg), $"{tokenDisplayName} requires 5 arguments: token-number,separator,include-next,include-prev,source-format-string.", nameof(arg));

            var parts = arg.Split(',', 5);
            Require.That(
                parts.Length == 5,
                $"{tokenDisplayName} requires exactly 5 comma-separated arguments (got {parts.Length}): '{arg}'.",
                nameof(arg));

            var tokenNumber = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
            var separator = parts[1];
            var includeNext = _ParseIncludeFlag(tokenDisplayName, fieldLabel: "include-next", parts[2]);
            var includePrev = _ParseIncludeFlag(tokenDisplayName, fieldLabel: "include-prev", parts[3]);
            var sourceFormatString = parts[4];

            Require.That(tokenNumber >= 1, $"{tokenDisplayName} token-number must be 1 or greater (got {tokenNumber}).", nameof(arg));

            Require.That(!string.IsNullOrEmpty(separator), $"{tokenDisplayName} separator must not be empty.", nameof(arg));

            return new Options(
                TokenNumber: tokenNumber,
                Separator: separator,
                IncludeNext: includeNext,
                IncludePrev: includePrev,
                SourceFormatString: sourceFormatString);
        }

        /// <summary>
        /// Parses an include-next or include-prev flag field (<c>true</c>/<c>false</c>, case-insensitive).
        /// </summary>
        private static bool _ParseIncludeFlag(string tokenDisplayName, string fieldLabel, string raw)
        {
            if (!bool.TryParse(raw.Trim(), out var value))
                throw new ArgumentException(
                    $"{tokenDisplayName} {fieldLabel} '{raw}' is not supported (expected {FormatOptionsParsing.FormatExpectedKeywords(_includeFlagKeywords)}).",
                    paramName: nameof(raw));

            return value;
        }
    }
}

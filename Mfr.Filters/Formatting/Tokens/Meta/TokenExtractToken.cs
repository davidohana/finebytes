using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Resolves the <c>&lt;token:…&gt;</c> meta-token (split resolved source by separator and pick a part).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Named arguments (order-independent), for example
    /// <c>&lt;token:tokenNumber=1, separator=-, includeNext=false, includePrev=false, source=&lt;full-name&gt;&gt;</c>.
    /// Whitespace around <c>=</c> and commas is ignored.
    /// </para>
    /// <para>
    /// Splits the resolved <c>source</c> format string by <c>separator</c> and extracts the
    /// 1-based <c>tokenNumber</c> part. <c>includeNext</c> and <c>includePrev</c> are <c>true</c> or <c>false</c> (case-insensitive).
    /// When include-next is true, all parts from <c>token-number</c> to the end are rejoined.
    /// When include-prev is true, all parts from the start through <c>token-number</c> are rejoined.
    /// When both are true the full source string is returned.
    /// </para>
    /// <para>
    /// <c>source</c> may contain nested format tokens and commas inside <c>&lt;…&gt;</c>; commas at bracket depth 0 separate options.
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

        private static readonly string[] _tokenExtractOptionKeys =
            ["tokenNumber", "separator", "includeNext", "includePrev", "source"];

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
            Require.That(
                !string.IsNullOrEmpty(arg),
                $"{tokenDisplayName} requires named options ({FormatOptionsParsing.FormatExpectedKeywords(_tokenExtractOptionKeys)}).",
                nameof(arg));

            var map = FormatOptionsParsing.ParseNamedKeyValuePairs(arg.Trim(), tokenDisplayName);
            FormatOptionsParsing.RequireKnownOptionKeysOnly(map, tokenDisplayName, _tokenExtractOptionKeys, nameof(arg));

            foreach (var req in _tokenExtractOptionKeys)
            {
                if (!map.ContainsKey(req))
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} missing required option '{req}' (expected all of {FormatOptionsParsing.FormatExpectedKeywords(_tokenExtractOptionKeys)}).",
                        nameof(arg));
                }
            }

            var tokenNumber = int.Parse(map["tokenNumber"].Trim(), CultureInfo.InvariantCulture);
            var separator = map["separator"];
            var includeNext = _ParseIncludeFlag(tokenDisplayName, fieldLabel: "includeNext", map["includeNext"]);
            var includePrev = _ParseIncludeFlag(tokenDisplayName, fieldLabel: "includePrev", map["includePrev"]);
            var sourceFormatString = map["source"];

            Require.That(tokenNumber >= 1, $"{tokenDisplayName} tokenNumber must be 1 or greater (got {tokenNumber}).", nameof(arg));

            Require.That(!string.IsNullOrEmpty(separator), $"{tokenDisplayName} separator must not be empty.", nameof(arg));

            return new Options(
                TokenNumber: tokenNumber,
                Separator: separator,
                IncludeNext: includeNext,
                IncludePrev: includePrev,
                SourceFormatString: sourceFormatString);
        }

        /// <summary>
        /// Parses <c>includeNext</c> / <c>includePrev</c> (<c>true</c>/<c>false</c>, case-insensitive).
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

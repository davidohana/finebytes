using System.Text;
using Mfr.Filters.Formatting.Tokens;

namespace Mfr.Filters.Formatting.FormatString
{
    /// <summary>
    /// Compiles formatter template text into a per-item delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tokens use angle-bracket syntax <c>&lt;name&gt;</c> or <c>&lt;name:arg&gt;</c>. The set of
    /// recognized names is discovered automatically: every concrete <see cref="IFormatToken"/>
    /// implementation in this assembly with a parameterless constructor is instantiated once at
    /// startup and registered under each of its <see cref="IFormatToken.Names"/>. Add a new token by
    /// dropping a new class implementing <see cref="IFormatToken"/> under
    /// <c>Mfr.Filters.Formatting.Tokens.*</c> with <see cref="FormatTokenInfoAttribute"/>.
    /// </para>
    /// <para>
    /// Nesting is handled at compile time: tokens whose argument contains a nested format string
    /// (e.g. <c>&lt;substr:start=1,end=3,source=&lt;full-name&gt;&gt;</c>) call <see cref="Compile"/> on that argument
    /// inside their own <see cref="IFormatToken.Compile"/> implementation, so the inner template is
    /// compiled once alongside the outer one.
    /// </para>
    /// </remarks>
    internal static class FormatStringCompiler
    {
        /// <summary>
        /// Formatter that always yields <see cref="string.Empty"/> (e.g. when a preset omits a field or uses a literal with no template).
        /// </summary>
        internal static readonly Formatter EmptyFormatter = static _ => "";

        /// <summary>
        /// Compiles <paramref name="template"/> into a delegate that is evaluated per item at rename time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Token boundaries are located using a balanced bracket scan so arbitrary nesting depth is
        /// handled correctly. All argument parsing runs exactly once; the returned delegate performs
        /// only the item-dependent work on each call.
        /// </para>
        /// </remarks>
        /// <param name="template">Template text that may contain tokens.</param>
        /// <returns>A <see cref="Formatter"/> that produces the fully expanded string for a <see cref="RenameItem"/>.</returns>
        internal static Formatter Compile(string template)
        {
            var pieces = new List<FormatStringPiece>();
            // Compile treats unclosed likely tokens as literals (same as non-token angles).
            _ = FormatStringScan.TryWalk(template, errorOnUnclosedLikelyToken: false, pieces, out _);

            var segments = new List<Formatter>(pieces.Count);
            foreach (var piece in pieces)
            {
                if (!piece.IsToken)
                {
                    var literal = template.Substring(piece.Start, piece.Length);
                    segments.Add(_ => literal);
                    continue;
                }

                segments.Add(_CompileToken(piece.Name, piece.Args));
            }

            if (segments.Count == 0)
            {
                return _ => template;
            }

            if (segments.Count == 1)
            {
                return segments[0];
            }

            var frozen = segments.ToArray();
            return item =>
            {
                var sb = new StringBuilder();
                foreach (var seg in frozen)
                {
                    sb.Append(seg(item));
                }

                return sb.ToString();
            };
        }

        /// <summary>
        /// Returns whether <paramref name="text"/> likely contains formatter tokens: at least one balanced
        /// <c>&lt;...&gt;</c> span whose leading name matches the formatter token-name heuristic (ASCII letter, then
        /// letters/digits/<c>-</c>/<c>_</c>, at least two characters before an optional <c>:</c>).
        /// Names are not trimmed (same exact-name policy as <see cref="Compile"/>).
        /// </summary>
        /// <param name="text">Candidate template text.</param>
        /// <returns><see langword="true"/> when a qualifying span exists.</returns>
        internal static bool ContainsLikelyFormatTokens(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '<')
                {
                    continue;
                }

                var close = FormatStringScan.FindMatchingClose(text, i);
                if (close < 0 || close <= i + 1)
                {
                    continue;
                }

                var inner = text[(i + 1)..close];
                if (inner.Length == 0)
                {
                    continue;
                }

                FormatStringScan.SplitNameAndArgs(inner, out var namePart, out _);
                // Exact name (no trim) — same policy as Compile / TryValidate.
                if (FormatStringScan.LooksLikeFormatterTokenName(namePart))
                {
                    return true;
                }
            }

            return false;
        }

        private static Formatter _CompileToken(string name, string tokenArgs)
        {
            if (!FormatTokenRegistry.NameToToken.TryGetValue(name, out var token))
            {
                throw new NotSupportedException(FormatStringScan.UnknownTokenMessage(name));
            }

            return token.Compile(tokenArgs);
        }
    }
}

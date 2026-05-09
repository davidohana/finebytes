using System.Text;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Models;

namespace Mfr.Filters.Formatting
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
    /// <c>Mfr.Filters.Formatting.Tokens.*</c>.
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
        private static readonly Dictionary<string, IFormatToken> _nameToToken = _DiscoverTokens();

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
        /// <returns>A function that produces the fully expanded string for a <see cref="RenameItem"/>.</returns>
        internal static Func<RenameItem, string> Compile(string template)
        {
            var segments = new List<Func<RenameItem, string>>();
            var i = 0;
            var literalStart = 0;

            while (i < template.Length)
            {
                if (template[i] != '<')
                {
                    i++;
                    continue;
                }

                var tokenStart = i;
                var tokenEnd = _FindMatchingClose(template, tokenStart);
                if (tokenEnd < 0)
                {
                    i++;
                    continue;
                }

                if (tokenStart > literalStart)
                {
                    var literal = template[literalStart..tokenStart];
                    segments.Add(_ => literal);
                }

                var tokenInner = template[(tokenStart + 1)..tokenEnd];
                segments.Add(_CompileToken(tokenInner));
                i = tokenEnd + 1;
                literalStart = i;
            }

            if (literalStart < template.Length)
            {
                var tail = template[literalStart..];
                segments.Add(_ => tail);
            }

            if (segments.Count == 0)
                return _ => template;

            if (segments.Count == 1)
                return segments[0];

            var frozen = segments.ToArray();
            return item =>
            {
                var sb = new StringBuilder();
                foreach (var seg in frozen)
                    sb.Append(seg(item));
                return sb.ToString();
            };
        }

        /// <summary>
        /// Scans forward from the opening <c>&lt;</c> at <paramref name="openIndex"/> and returns the
        /// index of the matching closing <c>&gt;</c>, or <c>-1</c> when no balanced close is found.
        /// </summary>
        private static int _FindMatchingClose(string template, int openIndex)
        {
            var depth = 0;
            for (var j = openIndex; j < template.Length; j++)
            {
                if (template[j] == '<') depth++;
                else if (template[j] == '>')
                {
                    depth--;
                    if (depth == 0)
                        return j;
                }
            }
            return -1;
        }

        private static Func<RenameItem, string> _CompileToken(string tokenInner)
        {
            var colonIndex = tokenInner.IndexOf(':');
            var name = colonIndex < 0 ? tokenInner : tokenInner[..colonIndex];
            var arg = colonIndex < 0 ? "" : tokenInner[(colonIndex + 1)..];
            if (!_nameToToken.TryGetValue(name, out var token))
                throw new NotSupportedException($"Unknown formatter token '<{name}>'. See the Formatter docs for supported tokens.");
            return token.Compile(arg);
        }

        private static Dictionary<string, IFormatToken> _DiscoverTokens()
        {
            var map = new Dictionary<string, IFormatToken>(StringComparer.Ordinal);
            var tokenTypes = typeof(FormatStringCompiler).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFormatToken).IsAssignableFrom(t));

            foreach (var tokenType in tokenTypes)
            {
                var token = (IFormatToken)Activator.CreateInstance(tokenType)!;
                foreach (var name in token.Names)
                {
                    if (map.TryGetValue(name, out var registeredToken))
                    {
                        throw new InvalidOperationException(
                            $"Format token name '{name}' is registered by multiple types "
                            + $"('{registeredToken.GetType().FullName}' and '{tokenType.FullName}').");
                    }

                    map[name] = token;
                }
            }
            return map;
        }
    }
}

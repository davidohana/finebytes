using System.Text.RegularExpressions;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Resolves formatter tokens in template text.
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
    /// Arbitrary nesting depth is supported. The regex matches tokens with at most one level of
    /// inner nesting per pass; the resolver iterates until the template stabilizes, peeling one
    /// layer of nesting per pass. Each token's <see cref="IFormatToken.Resolve"/> implementation
    /// is responsible for calling <see cref="ResolveTemplate"/> on its own source argument when
    /// that argument may itself contain tokens.
    /// </para>
    /// </remarks>
    internal static partial class FormatStringResolver
    {
        private static readonly Dictionary<string, IFormatToken> _nameToToken = _DiscoverTokens();

        private const int MaxResolutionPasses = 20;

        /// <summary>
        /// Resolves all formatter tokens inside <paramref name="template"/>, supporting arbitrary nesting depth.
        /// </summary>
        /// <param name="template">Template text that may contain tokens.</param>
        /// <param name="item">Rename item used to resolve item-aware tokens.</param>
        /// <returns>Template text with all tokens fully resolved.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the template does not stabilize within <see cref="MaxResolutionPasses"/> passes,
        /// which indicates a token whose resolved value introduces new tokens.
        /// </exception>
        internal static string ResolveTemplate(string template, RenameItem item)
        {
            for (var pass = 0; pass < MaxResolutionPasses; pass++)
            {
                var resolved = _TokenRegex().Replace(template, m => _ResolveToken(m.Groups[1].Value, item));
                if (string.Equals(resolved, template, StringComparison.Ordinal))
                    return resolved;
                template = resolved;
            }
            throw new InvalidOperationException(
                $"Format template did not stabilize after {MaxResolutionPasses} resolution passes. " +
                "A token may be producing output that contains new format tokens.");
        }

        private static string _ResolveToken(string tokenInner, RenameItem item)
        {
            var parts = tokenInner.Split(':', 2);
            var name = parts[0];
            var arg = parts.Length == 2 ? parts[1] : "";
            if (!_nameToToken.TryGetValue(name, out var token))
                throw new NotSupportedException($"Unknown formatter token '<{name}>'. See the Formatter docs for supported tokens.");
            return token.Resolve(arg, item);
        }

        private static Dictionary<string, IFormatToken> _DiscoverTokens()
        {
            var map = new Dictionary<string, IFormatToken>(StringComparer.Ordinal);
            var tokenTypes = typeof(FormatStringResolver).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFormatToken).IsAssignableFrom(t));

            foreach (var tokenType in tokenTypes)
            {
                var token = (IFormatToken)Activator.CreateInstance(tokenType)!;
                foreach (var name in token.Names)
                {
                    if (map.TryGetValue(name, out var value))
                    {
                        throw new InvalidOperationException(
                            $"Format token name '{name}' is registered by multiple types " +
                            $"('{value.GetType().FullName}' and '{tokenType.FullName}').");
                    }
                    map[name] = token;
                }
            }
            return map;
        }

        [GeneratedRegex(@"<([^<>]+(?:<[^<>]+>[^<>]*)*)>")]
        private static partial Regex _TokenRegex();
    }
}

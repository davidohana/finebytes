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
    /// </remarks>
    internal static partial class FormatStringResolver
    {
        private static readonly Dictionary<string, IFormatToken> _nameToToken = _DiscoverTokens();

        /// <summary>
        /// Resolves all formatter tokens inside <paramref name="template"/>.
        /// </summary>
        /// <param name="template">Template text that may contain tokens.</param>
        /// <param name="item">Rename item used to resolve item-aware tokens.</param>
        /// <returns>Template text with tokens resolved.</returns>
        internal static string ResolveTemplate(string template, RenameItem item)
        {
            return _TokenRegex().Replace(template, m => _ResolveToken(m.Groups[1].Value, item));
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

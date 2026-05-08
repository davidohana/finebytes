using System.Text.RegularExpressions;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Filters.Formatting.Tokens.FileNameGroup;
using Mfr.Filters.Formatting.Tokens.FilePropertiesGroup;
using Mfr.Filters.Formatting.Tokens.GeneralGroup;
using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Resolves formatter tokens in template text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tokens use angle-bracket syntax <c>&lt;name&gt;</c> or <c>&lt;name:arg&gt;</c>. The set of
    /// recognized names comes from group modules under <see cref="Tokens"/>; add a new group by
    /// implementing a <c>Register</c> method and wiring it in <see cref="_BuildRegistry"/>.
    /// </para>
    /// </remarks>
    internal static partial class FormatStringResolver
    {
        private static readonly FormatTokenRegistry _registry = _BuildRegistry();

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
            return _registry.Resolve(name, arg, item);
        }

        private static FormatTokenRegistry _BuildRegistry()
        {
            var registry = new FormatTokenRegistry();
            FileNameGroupTokens.Register(registry);
            FilePropertiesGroupTokens.Register(registry);
            GeneralGroupTokens.Register(registry);
            return registry;
        }

        [GeneratedRegex(@"<([^<>]+)>", RegexOptions.Compiled)]
        private static partial Regex _TokenRegex();
    }
}

using System.Text.RegularExpressions;

namespace Mfr.Filters.Space
{
    internal static partial class TextFilterRegexCache
    {
        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex _whitespaceRegex();

        internal static Regex WhitespaceRegex => _whitespaceRegex();
    }
}

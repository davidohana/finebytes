using System.Text.RegularExpressions;

namespace Mfr.Models.Filters.Text
{
    internal static partial class TextFilterRegexCache
    {
        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex _WhitespaceRegex();

        internal static Regex WhitespaceRegex => _WhitespaceRegex();
    }
}

using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Misc
{
    /// <summary>
    /// Parenthesis/bracket types.
    /// </summary>
    public enum ParenthesisType
    {
        Round,
        Square,
        Curly,
        Angle
    }

    /// <summary>
    /// Options for stripping bracket/parenthesis pairs.
    /// </summary>
    /// <param name="Type">Pair type to target.</param>
    /// <param name="RemoveContents">Whether to remove bracketed contents or only delimiters.</param>
    public sealed record StripParenthesesOptions(
        ParenthesisType Type,
        bool RemoveContents);

    /// <summary>
    /// Removes selected parenthesis/bracket delimiters and optionally their contents.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Parenthesis-strip options.</param>
    public sealed partial record StripParenthesesFilter(
        bool Enabled,
        FilterTarget Target,
        StripParenthesesOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripParentheses";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            return Options.Type switch
            {
                ParenthesisType.Round => Strip(segment, _RoundParenRegex(), "(", ")"),
                ParenthesisType.Square => Strip(segment, _SquareParenRegex(), "[", "]"),
                ParenthesisType.Curly => Strip(segment, _CurlyParenRegex(), "{", "}"),
                ParenthesisType.Angle => Strip(segment, _AngleParenRegex(), "<", ">"),
                _ => segment
            };

            string Strip(string s, Regex regex, string open, string close)
            {
                return Options.RemoveContents
                    ? regex.Replace(s, "")
                    : s.Replace(open, "").Replace(close, "");
            }
        }

        [GeneratedRegex(@"\([^)]*\)", RegexOptions.Compiled)]
        private static partial Regex _RoundParenRegex();

        [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
        private static partial Regex _SquareParenRegex();

        [GeneratedRegex(@"\{[^}]*\}", RegexOptions.Compiled)]
        private static partial Regex _CurlyParenRegex();

        [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
        private static partial Regex _AngleParenRegex();
    }
}

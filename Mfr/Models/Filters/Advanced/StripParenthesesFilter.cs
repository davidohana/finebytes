using System.Text.RegularExpressions;
namespace Mfr.Models.Filters.Advanced
{
    /// <summary>
    /// Options for stripping bracket/parenthesis pairs.
    /// </summary>
    /// <param name="Types">Pipe-separated pair types to target.</param>
    /// <param name="RemoveContents">Whether to remove bracketed contents or only delimiters.</param>
    public sealed record StripParenthesesOptions(
        string Types,
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
        StripParenthesesOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripParentheses";

        internal override string ApplySegment(string segment, RenameItem item)
        {
            var pairs = new List<(char open, char close)>();
            foreach (var token in Options.Types.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                switch (token.Trim())
                {
                    case "Round":
                        pairs.Add(('(', ')'));
                        break;
                    case "Square":
                        pairs.Add(('[', ']'));
                        break;
                    case "Curly":
                        pairs.Add(('{', '}'));
                        break;
                    case "Angle":
                        pairs.Add(('<', '>'));
                        break;
                    default:
                        break;
                }
            }

            var res = segment;
            foreach ((var open, var close) in pairs)
            {
                if (open == '(' && close == ')')
                {
                    res = Options.RemoveContents
                        ? _RoundParenRegex().Replace(res, "")
                        : res.Replace("(", "").Replace(")", "");
                }
                else if (open == '[' && close == ']')
                {
                    res = Options.RemoveContents
                        ? _SquareParenRegex().Replace(res, "")
                        : res.Replace("[", "").Replace("]", "");
                }
                else if (open == '{' && close == '}')
                {
                    res = Options.RemoveContents
                        ? _CurlyParenRegex().Replace(res, "")
                        : res.Replace("{", "").Replace("}", "");
                }
                else if (open == '<' && close == '>')
                {
                    res = Options.RemoveContents
                        ? _AngleParenRegex().Replace(res, "")
                        : res.Replace("<", "").Replace(">", "");
                }
            }

            return res;
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

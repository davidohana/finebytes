using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Uppercases dot-separated initials patterns inside text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An initials pattern is one or more single letters separated by dots, such as
    /// <c>u.s.a</c>, <c>d.j</c>, or <c>e.x.a.m.p.l.e</c>.
    /// </para>
    /// <para>
    /// Only letters inside detected initials patterns are uppercased; all other characters stay unchanged.
    /// </para>
    /// </remarks>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed partial record UppercaseInitialsFilter(
        FilterTarget Target) : BaseFilter(Target)
    {
        // Keep this runtime-compiled regex for readability and to avoid GeneratedRegex partial-method
        // analyzer noise in this project setup.
#pragma warning disable SYSLIB1045
        private static readonly Regex _regex = new(
            @"(?<!\p{L})\p{L}(?:\.\p{L})+(?!\p{L})",
            RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "UppercaseInitials";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return segment;
            }

            return _regex.Replace(segment, m => m.Value.ToUpperInvariant());
        }

    }
}

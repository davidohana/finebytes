using Mfr.Filters.Case;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="StringApplyScope"/> slice/splice behavior via <see cref="LettersCaseFilter"/>.
    /// </summary>
    public sealed class StringApplyScopeTransformTests
    {
        private static readonly FilePrefixTarget s_target = new();

        /// <summary>
        /// Verifies 1-based inclusive left-anchored substring is uppercased only inside the range.
        /// </summary>
        [Fact]
        public void Substring_LeftAnchors_UppercasesInsideRangeOnly()
        {
            var scope = new SubstringApplyScope(
                StartPosition: 1,
                StartAnchor: StringScopeAnchor.Left,
                EndPosition: 3,
                EndAnchor: StringScopeAnchor.Left);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("ABCdef", FilterTestHelpers.ApplyToPrefix(f, "abcdef"));
        }

        /// <summary>
        /// Verifies inverted end/start indices are normalized (still inclusive).
        /// </summary>
        [Fact]
        public void Substring_InvertedEndpoints_NormalizesRange()
        {
            var scope = new SubstringApplyScope(
                StartPosition: 4,
                StartAnchor: StringScopeAnchor.Left,
                EndPosition: 2,
                EndAnchor: StringScopeAnchor.Left);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("aBCDef", FilterTestHelpers.ApplyToPrefix(f, "abcdef"));
        }

        /// <summary>
        /// Verifies right anchor maps position 1 to the last character.
        /// </summary>
        [Fact]
        public void Substring_RightAnchor_LastCharacterOnly()
        {
            var scope = new SubstringApplyScope(
                StartPosition: 1,
                StartAnchor: StringScopeAnchor.Right,
                EndPosition: 1,
                EndAnchor: StringScopeAnchor.Right);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("abcdeF", FilterTestHelpers.ApplyToPrefix(f, "abcdef"));
        }

        /// <summary>
        /// Verifies positions past the string length clamp without throwing.
        /// </summary>
        [Fact]
        public void Substring_ClampShortString_DoesNotThrow()
        {
            var scope = new SubstringApplyScope(
                StartPosition: 1,
                StartAnchor: StringScopeAnchor.Left,
                EndPosition: 99,
                EndAnchor: StringScopeAnchor.Left);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("AB", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies token scope transforms one part and preserves separators.
        /// </summary>
        [Fact]
        public void Token_SecondPart_UppercasesOnlyThatToken()
        {
            var scope = new TokenApplyScope(Separator: "-", TokenNumber: 2);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("aa-BB-cc", FilterTestHelpers.ApplyToPrefix(f, "aa-bb-cc"));
        }

        /// <summary>
        /// Verifies multi-character separator splits like formatter <c>&lt;token:&gt;</c>.
        /// </summary>
        [Fact]
        public void Token_MultiCharSeparator()
        {
            var scope = new TokenApplyScope(Separator: "--", TokenNumber: 2);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("x--YY--z", FilterTestHelpers.ApplyToPrefix(f, "x--yy--z"));
        }

        /// <summary>
        /// Verifies token index beyond part count leaves the value unchanged (no-op).
        /// </summary>
        [Fact]
        public void Token_OutOfRange_NoOp()
        {
            var scope = new TokenApplyScope(Separator: "-", TokenNumber: 5);
            var f = new LettersCaseFilter(
                s_target,
                new LettersCaseOptions(LettersCaseMode.UpperCase, []),
                scope);
            Assert.Equal("a-b", FilterTestHelpers.ApplyToPrefix(f, "a-b"));
        }
    }
}

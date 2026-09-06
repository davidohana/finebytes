using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Models.Filters.Formatting.FormatString
{
    /// <summary>
    /// Tests for <see cref="FormatStringSyntax.TryValidate"/>.
    /// </summary>
    public sealed class FormatStringSyntaxTests
    {
        /// <summary>
        /// Verifies an empty template is valid with no tokens.
        /// </summary>
        [Fact]
        public void TryValidate_EmptyTemplate_Succeeds()
        {
            var result = FormatStringSyntax.TryValidate("");

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(-1, result.ErrorPosition);
            Assert.Empty(result.Tokens);
        }

        /// <summary>
        /// Verifies a mixed literal/token template reports spans.
        /// </summary>
        [Fact]
        public void TryValidate_GoodMixedTemplate_ReturnsSpans()
        {
            var template = "Track: <file-name> [<ext>]";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.True(result.Success);
            Assert.Equal(2, result.Tokens.Count);
            Assert.Equal("file-name", result.Tokens[0].CanonicalName);
            Assert.Equal("file-extension", result.Tokens[1].CanonicalName);
            Assert.Equal(template.IndexOf("<file-name>", StringComparison.Ordinal), result.Tokens[0].Start);
            Assert.Equal("<file-name>".Length, result.Tokens[0].Length);
            Assert.Equal(template.IndexOf("<ext>", StringComparison.Ordinal), result.Tokens[1].Start);
        }

        /// <summary>
        /// Verifies an unknown likely token fails with that span selected.
        /// </summary>
        [Fact]
        public void TryValidate_UnknownToken_FailsWithSpan()
        {
            var template = "x <does-not-exist> y";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.False(result.Success);
            Assert.Contains("Unknown formatter token", result.ErrorMessage);
            Assert.Equal(template.IndexOf('<'), result.ErrorPosition);
            Assert.Equal("<does-not-exist>".Length, result.ErrorLength);
            Assert.Empty(result.Tokens);
        }

        /// <summary>
        /// Verifies bad counter arguments fail with the counter span.
        /// </summary>
        [Fact]
        public void TryValidate_BadCounterArgs_FailsWithSpan()
        {
            var template = "<counter:padding=nope>";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(0, result.ErrorPosition);
            Assert.Equal(template.Length, result.ErrorLength);
        }

        /// <summary>
        /// Verifies bad substr arguments fail with the substr span.
        /// </summary>
        [Fact]
        public void TryValidate_BadSubstrArgs_FailsWithSpan()
        {
            var template = "pre <substr:start=1> post";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.False(result.Success);
            Assert.Equal(template.IndexOf('<'), result.ErrorPosition);
            Assert.Equal("<substr:start=1>".Length, result.ErrorLength);
        }

        /// <summary>
        /// Verifies an unclosed likely token name is reported as an error.
        /// </summary>
        [Fact]
        public void TryValidate_UnclosedLikelyToken_Fails()
        {
            var template = "start <file-name";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.False(result.Success);
            Assert.Contains("Unclosed", result.ErrorMessage);
            Assert.Equal(template.IndexOf('<'), result.ErrorPosition);
            Assert.Equal(template.Length - result.ErrorPosition, result.ErrorLength);
        }

        /// <summary>
        /// Verifies balanced spans that <see cref="FormatStringCompiler.Compile"/> rejects also fail validation
        /// (even when they do not match the token-name heuristic used for unclosed angles / ContainsLikely).
        /// </summary>
        [Theory]
        [InlineData("a < b > c")]
        [InlineData("<3>")]
        [InlineData("<>")]
        [InlineData("< file-name>")]
        public void TryValidate_BalancedUnknownSpans_FailLikeCompile(string template)
        {
            Assert.ThrowsAny<Exception>(() => FormatStringCompiler.Compile(template));

            var result = FormatStringSyntax.TryValidate(template);

            Assert.False(result.Success);
            Assert.Contains("Unknown formatter token", result.ErrorMessage);
            Assert.True(result.ErrorPosition >= 0);
            Assert.True(result.ErrorLength > 0);
            Assert.Empty(result.Tokens);
        }

        /// <summary>
        /// Verifies comparison-like text without a balanced close still succeeds (compile treats as literal).
        /// </summary>
        [Fact]
        public void TryValidate_UnclosedNonTokenLooksLike_Succeeds()
        {
            var result = FormatStringSyntax.TryValidate("a < b");

            Assert.True(result.Success);
            Assert.Empty(result.Tokens);
        }

        /// <summary>
        /// Verifies balanced non-token-looking spans are accepted under WhenLikelyTokens (literal path).
        /// </summary>
        [Theory]
        [InlineData("a < b > c")]
        [InlineData("<3>")]
        [InlineData("<>")]
        [InlineData("< file-name>")]
        public void TryValidate_WhenLikelyTokens_NonLikelyBalanced_Succeeds(string template)
        {
            var result = FormatStringSyntax.TryValidate(template, FormatStringValidationMode.WhenLikelyTokens);

            Assert.True(result.Success);
            Assert.Empty(result.Tokens);
        }

        /// <summary>
        /// Verifies WhenLikelyTokens still rejects bad real tokens.
        /// </summary>
        [Fact]
        public void TryValidate_WhenLikelyTokens_UnknownLikelyToken_Fails()
        {
            var template = "x <does-not-exist> y";
            var result = FormatStringSyntax.TryValidate(template, FormatStringValidationMode.WhenLikelyTokens);

            Assert.False(result.Success);
            Assert.Contains("Unknown formatter token", result.ErrorMessage);
        }

        /// <summary>
        /// Verifies nested tokens inside args are accepted when the outer token compiles.
        /// </summary>
        [Fact]
        public void TryValidate_NestedSubstr_Succeeds()
        {
            var template = "<substr:start=1,end=-1,source=<file-name>>";
            var result = FormatStringSyntax.TryValidate(template);

            Assert.True(result.Success);
            Assert.Single(result.Tokens);
            Assert.Equal("substr", result.Tokens[0].CanonicalName);
        }
    }
}

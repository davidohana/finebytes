using System.Text.Json;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="ShrinkDuplicateCharactersFilter"/>.
    /// </summary>
    public class ShrinkDuplicateCharactersFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies adjacent duplicate occurrences of the configured character collapse to one.
        /// </summary>
        [Fact]
        public void Apply_CollapsesAdjacentDuplicatesOfConfiguredCharacter()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '-')
            );

            Assert.Equal("I am Kloot - To You", FilterTestHelpers.ApplyToPrefix(filter, "I am Kloot --- To You"));
            Assert.Equal("a-b-c", FilterTestHelpers.ApplyToPrefix(filter, "a--b---c"));
        }

        /// <summary>
        /// Verifies only adjacent duplicates are affected and non-adjacent occurrences are retained.
        /// </summary>
        [Fact]
        public void Apply_LeavesNonAdjacentOccurrencesUntouched()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '>')
            );

            Assert.Equal("a>b>c", FilterTestHelpers.ApplyToPrefix(filter, "a>>b>>>c"));
            Assert.Equal(">a>b>", FilterTestHelpers.ApplyToPrefix(filter, ">>>a>>>b>>>"));
        }

        /// <summary>
        /// Verifies regex metacharacters are treated literally (via escape), not as patterns.
        /// </summary>
        [Fact]
        public void Apply_RegexMetacharacter_CollapsesLiteralRuns()
        {
            var dot = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '.')
            );
            Assert.Equal("a.b.c", FilterTestHelpers.ApplyToPrefix(dot, "a...b..c"));

            var star = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '*')
            );
            Assert.Equal("a*b*", FilterTestHelpers.ApplyToPrefix(star, "a***b**"));
        }

        /// <summary>
        /// Verifies empty input stays empty.
        /// </summary>
        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '-')
            );

            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(filter, ""));
        }

        /// <summary>
        /// Verifies unchanged output when the configured character is absent.
        /// </summary>
        [Fact]
        public void Apply_NoTargetCharacter_ReturnsInputAsIs()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '-')
            );

            Assert.Equal("abc def", FilterTestHelpers.ApplyToPrefix(filter, "abc def"));
        }

        /// <summary>
        /// Verifies MFR7 empty-editor / null character is a no-op.
        /// </summary>
        [Fact]
        public void Apply_NullCharacter_ReturnsInputAsIs()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '\0')
            );

            Assert.Equal("a---b", FilterTestHelpers.ApplyToPrefix(filter, "a---b"));
        }

        /// <summary>
        /// Verifies preset JSON round-trips a one-character option (including a regex metacharacter).
        /// </summary>
        [Fact]
        public void Json_RoundTripsSingleCharacterOption()
        {
            var original = new ShrinkDuplicateCharactersFilter(
                _target,
                new ShrinkDuplicateCharactersOptions(Character: '.')
            );

            var json = JsonSerializer.Serialize<BaseFilter>(original, PresetJsonOptions.Default);
            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<ShrinkDuplicateCharactersFilter>(filter);

            Assert.Equal('.', typed.Options.Character);
            Assert.Equal("a.b", FilterTestHelpers.ApplyToPrefix(typed, "a...b"));
        }

        /// <summary>
        /// Verifies empty or multi-character JSON strings fail to deserialize as <c>char</c>.
        /// </summary>
        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"ab\"")]
        public void Json_EmptyOrMultiCharacter_Throws(string characterJson)
        {
            var json = /*lang=json,strict*/
                $$"""
                {
                  "type": "ShrinkDuplicateCharacters",
                  "target": {
                    "targetType": "FilePrefix"
                  },
                  "options": {
                    "character": {{characterJson}}
                  }
                }
                """;

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default));
        }
    }
}

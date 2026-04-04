using System.Text.Json;

using Mfr8.Core;

namespace mfr8.Tests.Core
{
    public class FilterParserTests
    {
        [Fact]
        /// <summary>
        /// Verifies that the JSON parser correctly builds a <see cref="LettersCaseFilter"/> from valid input.
        /// </summary>
        public void Parses_LettersCase()
        {
            var json = """
        {
          "type": "LettersCase",
          "enabled": true,
          "target": { "family": "FileName", "fileNameMode": "Full" },
          "options": { "mode": "UpperCase", "skipWords": ["a", "the"] }
        }
        """;

            var el = JsonDocument.Parse(json).RootElement;
            var filter = FilterParser.ParseFilter(el);

            _ = Assert.IsType<LettersCaseFilter>(filter);
            var typed = (LettersCaseFilter)filter;
            Assert.Equal(LettersCaseMode.UpperCase, typed.Options.Mode);
            Assert.Contains("the", typed.Options.SkipWords);
        }

        [Fact]
        /// <summary>
        /// Verifies that parsing rejects non-<c>FileName</c> targets in phase 1.
        /// </summary>
        public void Rejects_NonFileNameFamily()
        {
            var json = """
        {
          "type": "LettersCase",
          "enabled": true,
          "target": { "family": "AudioTag", "field": "Title" },
          "options": { "mode": "UpperCase" }
        }
        """;

            var el = JsonDocument.Parse(json).RootElement;
            var ex = Assert.Throws<NotSupportedException>(() => FilterParser.ParseFilter(el));
            Assert.Contains("Phase 1 only supports target.family='FileName'", ex.Message);
        }
    }
}

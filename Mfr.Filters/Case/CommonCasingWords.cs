namespace Mfr.Filters.Case
{
    /// <summary>
    /// Shared casing word lists for Casing List factory defaults and Letters Case capitalize skip-words.
    /// </summary>
    internal static class CommonCasingWords
    {
        /// <summary>
        /// English title-case exceptions (articles, short conjunctions, common prepositions, short <c>be</c> forms).
        /// </summary>
        /// <remarks>
        /// Single source for <see cref="LettersCaseOptions.DefaultCapitalizeSkipWords"/> and the leading
        /// segment of <see cref="CasingListOptions.DefaultWords"/>.
        /// </remarks>
        internal static readonly string[] TitleCaseExceptions =
        [
            "a",
            "about",
            "above",
            "across",
            "after",
            "against",
            "along",
            "am",
            "among",
            "an",
            "and",
            "are",
            "around",
            "as",
            "at",
            "be",
            "been",
            "before",
            "behind",
            "being",
            "below",
            "beneath",
            "beside",
            "between",
            "beyond",
            "but",
            "by",
            "despite",
            "down",
            "during",
            "for",
            "from",
            "in",
            "inside",
            "into",
            "is",
            "like",
            "near",
            "nor",
            "of",
            "off",
            "on",
            "onto",
            "or",
            "out",
            "outside",
            "over",
            "per",
            "since",
            "so",
            "than",
            "the",
            "through",
            "to",
            "toward",
            "towards",
            "under",
            "until",
            "up",
            "upon",
            "versus",
            "via",
            "vs",
            "was",
            "were",
            "with",
            "within",
            "without",
            "yet",
        ];

        /// <summary>
        /// Common multilingual particles useful in world-music / file titles (not already in title-case exceptions).
        /// </summary>
        private static readonly string[] MultilingualParticles =
        [
            "da",
            "das",
            "de",
            "del",
            "der",
            "des",
            "di",
            "die",
            "du",
            "el",
            "en",
            "et",
            "la",
            "le",
            "les",
            "und",
            "van",
            "von",
            "y",
        ];

        /// <summary>
        /// Common rename / media acronyms and roman numerals (exact casing; omits bare <c>I</c> / <c>V</c>).
        /// </summary>
        private static readonly string[] MediaAcronyms =
        [
            "DJ",
            "MC",
            "EP",
            "LP",
            "CD",
            "DVD",
            "OST",
            "VIP",
            "RMX",
            "MIX",
            "HD",
            "UHD",
            "4K",
            "8K",
            "HDR",
            "MP3",
            "FLAC",
            "AAC",
            "WAV",
            "PDF",
            "USA",
            "UK",
            "EU",
            "NYC",
            "TV",
            "ID",
            "II",
            "III",
            "IV",
            "VI",
            "VII",
            "VIII",
            "IX",
            "X",
            "XI",
            "XII",
        ];

        /// <summary>
        /// Full curated factory list: title-case exceptions, then particles, then media acronyms.
        /// </summary>
        internal static readonly IReadOnlyList<string> DefaultWords =
        [
            .. TitleCaseExceptions,
            .. MultilingualParticles,
            .. MediaAcronyms,
        ];
    }
}

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Shared sentence-initial uppercasing for Letters Case (sentence mode) and Casing List.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uppercases the first letter of the text (scanning past leading non-letters) and the first letter
    /// after each character in <see cref="RenameItem.SentenceEndChars"/> when that character is not the
    /// word separator and is followed by one or more word-separator characters.
    /// </para>
    /// </remarks>
    internal static class SentenceInitialCasing
    {
        /// <summary>
        /// Uppercases sentence-start letters in <paramref name="input"/> using the shared boundary rules.
        /// </summary>
        /// <param name="input">Text to process (not lowercased by this helper).</param>
        /// <param name="wordSeparator">Configured word separator character.</param>
        /// <param name="sentenceEndChars">Characters treated as sentence boundaries.</param>
        /// <returns>Text with sentence starts uppercased.</returns>
        public static string UppercaseInitials(string input, char wordSeparator, string sentenceEndChars)
        {
            if (input.Length == 0)
            {
                return input;
            }

            var chars = input.ToCharArray();
            _TryUppercaseFirstLetterFrom(chars, startIndex: 0);

            for (var i = 0; i < chars.Length - 1; i++)
            {
                var isSentenceEnd = chars[i] != wordSeparator && sentenceEndChars.Contains(chars[i]);
                if (!isSentenceEnd || chars[i + 1] != wordSeparator)
                {
                    continue;
                }

                var j = i + 2;
                while (j < chars.Length && chars[j] == wordSeparator)
                {
                    j++;
                }

                if (j >= chars.Length)
                {
                    continue;
                }

                _TryUppercaseFirstLetterFrom(chars, j);
            }

            return new string(chars);
        }

        /// <summary>
        /// Uppercases the first letter at or after <paramref name="startIndex"/> (any script).
        /// </summary>
        private static void _TryUppercaseFirstLetterFrom(char[] chars, int startIndex)
        {
            for (var i = startIndex; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!char.IsLetter(c))
                {
                    continue;
                }

                chars[i] = char.ToUpperInvariant(c);
                return;
            }
        }
    }
}

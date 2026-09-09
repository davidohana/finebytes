using System.Text;

namespace Mfr.Filters
{
    /// <summary>
    /// Shared shrink patterns for adjacent duplicate characters (MFR7 <c>{2,}</c>).
    /// </summary>
    internal static class CharacterRunHelpers
    {
        /// <summary>
        /// Collapses each run of two or more identical <paramref name="character"/> values to one.
        /// </summary>
        /// <param name="value">Input text.</param>
        /// <param name="character">Character whose adjacent duplicates are collapsed.</param>
        /// <returns>Text with runs of length ≥ 2 reduced to a single <paramref name="character"/>.</returns>
        internal static string CollapseAdjacentDuplicates(string value, char character)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length < 2)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);
            var previous = value[0];
            builder.Append(previous);
            for (var i = 1; i < value.Length; i++)
            {
                var c = value[i];
                if (c == character && previous == character)
                {
                    continue;
                }

                builder.Append(c);
                previous = c;
            }

            return builder.ToString();
        }
    }
}

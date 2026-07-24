namespace Mfr.Utils
{
    /// <summary>
    /// Provides helpers for selecting between nullable candidate values.
    /// </summary>
    public static class Nullables
    {
        /// <summary>
        /// Returns the first candidate that is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">Reference type of the candidates.</typeparam>
        /// <param name="candidates">Candidates in precedence order.</param>
        /// <returns>The first non-<see langword="null"/> candidate, or <see langword="null"/> when all are absent.</returns>
        public static T? FirstNonNull<T>(params T?[] candidates)
            where T : class
        {
            foreach (var candidate in candidates)
            {
                if (candidate is not null)
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Returns the first candidate that has a value.
        /// </summary>
        /// <typeparam name="T">Value type of the candidates.</typeparam>
        /// <param name="candidates">Candidates in precedence order.</param>
        /// <returns>The first candidate with a value, or <see langword="null"/> when all are absent.</returns>
        public static T? FirstNonNull<T>(params T?[] candidates)
            where T : struct
        {
            foreach (var candidate in candidates)
            {
                if (candidate is not null)
                    return candidate;
            }

            return null;
        }
    }
}

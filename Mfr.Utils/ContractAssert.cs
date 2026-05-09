using System.Diagnostics.CodeAnalysis;

namespace Mfr.Utils
{
    /// <summary>
    /// Kotlin-like state checks (<c>check</c>); failures throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// Ensures <paramref name="condition"/> is true.
        /// </summary>
        /// <param name="condition">Expected to be true for valid runtime state.</param>
        /// <param name="message">Describes the failure when <paramref name="condition"/> is false.</param>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="condition"/> is false.</exception>
        public static void That(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Returns <paramref name="value"/> when it is non-null.
        /// </summary>
        /// <typeparam name="T">Reference type held by <paramref name="value"/>.</typeparam>
        /// <param name="value">Candidate value that must not be null.</param>
        /// <param name="message">Describes the failure when <paramref name="value"/> is null.</param>
        /// <returns><paramref name="value"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="value"/> is null.</exception>
        [return: NotNull]
        public static T NotNull<T>(T? value, string message) where T : class
        {
            if (value is null)
                throw new InvalidOperationException(message);

            return value;
        }
    }

    /// <summary>
    /// Kotlin-like preconditions (<c>require</c>); failures throw <see cref="ArgumentException"/>.
    /// </summary>
    public static class Require
    {
        /// <summary>
        /// Ensures <paramref name="condition"/> is true for valid arguments or configuration.
        /// </summary>
        /// <param name="condition">Expected to hold for valid inputs.</param>
        /// <param name="message">Describes the contract violation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="condition"/> is false.</exception>
        public static void That(bool condition, string message)
        {
            if (!condition)
                throw new ArgumentException(message);
        }

        /// <summary>
        /// Ensures <paramref name="condition"/> is true and associates the violation with <paramref name="paramName"/>.
        /// </summary>
        /// <param name="condition">Expected to hold for valid inputs.</param>
        /// <param name="message">Describes the contract violation.</param>
        /// <param name="paramName">Affected parameter name (for example from <see langword="nameof"/>).</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="condition"/> is false.</exception>
        public static void That(bool condition, string message, string paramName)
        {
            if (!condition)
                throw new ArgumentException(message, paramName);
        }
    }
}

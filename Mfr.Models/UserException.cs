namespace Mfr.Models
{
    /// <summary>
    /// Exception for invalid user input or configuration.
    /// </summary>
    public sealed class UserException : Exception
    {
        /// <summary>
        /// Initializes a <see cref="UserException"/> with default state.
        /// </summary>
        public UserException()
        {
        }

        /// <summary>
        /// Initializes with an error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UserException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes with a message and an inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UserException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}

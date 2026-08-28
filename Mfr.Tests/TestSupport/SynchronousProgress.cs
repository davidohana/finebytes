namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Invokes progress callbacks on the reporting thread (unlike <see cref="Progress{T}"/>).
    /// </summary>
    /// <typeparam name="T">Progress snapshot type.</typeparam>
    internal sealed class SynchronousProgress<T>(Action<T> onReport) : IProgress<T>
    {
        /// <inheritdoc />
        public void Report(T value)
        {
            onReport(value);
        }
    }
}

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Marks a test that requires Windows path, attribute, or shell semantics.
    /// </summary>
    /// <remarks>
    /// Skipped automatically on non-Windows hosts so Linux CI stays green for Windows-only behavior.
    /// </remarks>
    public sealed class WindowsFactAttribute : FactAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsFactAttribute"/> class.
        /// </summary>
        public WindowsFactAttribute()
        {
            if (!OperatingSystem.IsWindows())
            {
                Skip = "Windows-only behavior";
            }
        }
    }
}

using System.Runtime.CompilerServices;

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
        /// <param name="sourceFilePath">Caller source file (forwarded to xUnit v3).</param>
        /// <param name="sourceLineNumber">Caller source line (forwarded to xUnit v3).</param>
        public WindowsFactAttribute(
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1
        )
            : base(sourceFilePath, sourceLineNumber)
        {
            Skip = "Windows-only behavior";
            SkipUnless = nameof(IsWindows);
            SkipType = typeof(WindowsFactAttribute);
        }

        /// <summary>
        /// Gets a value indicating whether the current OS is Windows.
        /// </summary>
        public static bool IsWindows => OperatingSystem.IsWindows();
    }
}

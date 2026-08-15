namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Creates the default <see cref="ISystemIconProvider"/> for the current OS.
    /// </summary>
    public static class SystemIconProvider
    {
        /// <summary>
        /// Returns a Windows shell-icon provider on Windows, otherwise a no-op provider.
        /// </summary>
        /// <returns>An icon provider suitable for the host OS.</returns>
        public static ISystemIconProvider CreateDefault()
        {
            if (OperatingSystem.IsWindows())
                return new WindowsSystemIconProvider();

            return NullSystemIconProvider.Instance;
        }
    }
}

using Avalonia.Media;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Icon provider that never returns an image. Used in tests and on platforms without shell icons.
    /// </summary>
    public sealed class NullSystemIconProvider : ISystemIconProvider
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullSystemIconProvider Instance { get; } = new();

        /// <inheritdoc />
        public IImage? GetSmallIcon(string path, bool isDirectory)
        {
            return null;
        }
    }
}

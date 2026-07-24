using Mfr.Filters;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Temporarily replaces <see cref="RenameItemEmbeddedTags.TagReader"/> for one test scope.
    /// </summary>
    internal sealed class EmbeddedTagReaderScope : IDisposable
    {
        private readonly Func<string, AudioTagOverlay> _previousReader;

        private EmbeddedTagReaderScope(Func<string, AudioTagOverlay> reader)
        {
            _previousReader = RenameItemEmbeddedTags.TagReader;
            RenameItemEmbeddedTags.TagReader = reader;
        }

        /// <summary>
        /// Uses a reader that returns a detached copy of <paramref name="meta"/>'s current overlay.
        /// </summary>
        /// <param name="meta">Source row metadata.</param>
        /// <returns>A disposable that restores the prior reader on dispose.</returns>
        public static EmbeddedTagReaderScope ForMetaSnapshot(FileMeta meta)
        {
            ArgumentNullException.ThrowIfNull(meta);
            return new EmbeddedTagReaderScope(_ => meta.AudioTagOverlay.Clone());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameItemEmbeddedTags.TagReader = _previousReader;
        }
    }
}

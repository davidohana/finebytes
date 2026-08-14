using Mfr.Models.Tags;
using Mfr.Utils;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Overlay and stream-property snapshots from one TagLib open.
    /// </summary>
    /// <param name="Overlay">Embedded tags read from the file.</param>
    /// <param name="Media">Stream properties, including nested MPEG header data when present.</param>
    public readonly record struct TagLibFileSnapshot(AudioTagOverlay Overlay, MediaProperties Media);

    /// <summary>
    /// Opens a file once with TagLib and maps both embedded tags and stream properties.
    /// </summary>
    public static class TagLibFileReader
    {
        /// <summary>
        /// Reads tags and media properties from an existing regular file in a single TagLib open.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>Overlay and media snapshots mapped from the same open file.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static TagLibFileSnapshot Read(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            return new TagLibFileSnapshot(
                Overlay: AudioTagPersistence.ReadFrom(file),
                Media: MediaPropertiesReader.ReadFrom(file));
        }
    }
}

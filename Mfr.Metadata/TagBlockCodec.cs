using System.Collections.Immutable;
using Mfr.Models.Tags;
using TagLib;
using TagLib.Ogg;
using TagLib.Riff;

namespace Mfr.Metadata
{
    /// <summary>
    /// Rehydrates TagLib tag objects from durable overlay block snapshots (and the reverse build for ASF rows).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by <see cref="CommonAudioTag.FromOverlay"/> projection and <see cref="AudioTagPersistence"/> merge/apply so
    /// bytes ↔ TagLib parsing lives in one place.
    /// </para>
    /// </remarks>
    internal static class TagBlockCodec
    {
        /// <summary>
        /// Parses ID3v2 canonical bytes into a TagLib tag, or <see langword="null"/> when missing or corrupt.
        /// </summary>
        /// <param name="data">ID3v2 overlay snapshot.</param>
        /// <returns>Parsed tag, or <see langword="null"/>.</returns>
        public static TagLib.Id3v2.Tag? TryParseId3v2(Id3v2TagData? data)
        {
            if (data is null || data.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new TagLib.Id3v2.Tag(_ToByteVector(data.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a Xiph comment blob into a TagLib tag, or <see langword="null"/> when missing or corrupt.
        /// </summary>
        /// <param name="blob">Canonical Xiph render bytes.</param>
        /// <returns>Parsed comment, or <see langword="null"/>.</returns>
        public static XiphComment? TryParseXiph(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new XiphComment(_ToByteVector(blob.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                return null;
            }
            catch (ArgumentOutOfRangeException)
            {
                // TagLib can throw when comment packets are truncated or opaque (test doubles, partial reads).
                return null;
            }
        }

        /// <summary>
        /// Parses an APEv2 blob into a TagLib tag, or <see langword="null"/> when missing or corrupt.
        /// </summary>
        /// <param name="blob">Canonical APE render bytes.</param>
        /// <returns>Parsed tag, or <see langword="null"/>.</returns>
        public static TagLib.Ape.Tag? TryParseApe(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new TagLib.Ape.Tag(_ToByteVector(blob.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                return null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a RIFF INFO blob into a TagLib tag, or <see langword="null"/> when missing or corrupt.
        /// </summary>
        /// <param name="blob">Canonical INFO list render bytes.</param>
        /// <returns>Parsed tag, or <see langword="null"/>.</returns>
        public static InfoTag? TryParseRiffInfo(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new InfoTag(_ToByteVector(blob.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                return null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        /// <summary>
        /// Builds an ASF tag from descriptor rows when any exist; otherwise <see langword="null"/>.
        /// </summary>
        /// <param name="data">ASF overlay snapshot.</param>
        /// <returns>TagLib ASF tag, or <see langword="null"/> when absent or empty.</returns>
        public static TagLib.Asf.Tag? TryBuildAsfTag(AsfTagData? data)
        {
            if (data is null || data.Descriptors.IsDefaultOrEmpty)
                return null;

            return BuildAsfTag(data);
        }

        /// <summary>
        /// Builds an ASF tag from all descriptor rows on <paramref name="data"/> (including an empty set).
        /// </summary>
        /// <param name="data">ASF overlay snapshot; must not be <see langword="null"/>.</param>
        /// <returns>TagLib ASF tag populated from <see cref="AsfTagData.Descriptors"/>.</returns>
        public static TagLib.Asf.Tag BuildAsfTag(AsfTagData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var asf = new TagLib.Asf.Tag();
            foreach (var row in data.Descriptors)
                asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));

            return asf;
        }

        private static ByteVector _ToByteVector(ImmutableArray<byte> bytes)
        {
            return new ByteVector([.. bytes]);
        }
    }
}

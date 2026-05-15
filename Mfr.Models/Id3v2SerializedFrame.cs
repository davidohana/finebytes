using System.Collections.Immutable;

namespace Mfr.Models
{
    /// <summary>
    /// One ID3v2 frame serialized for overlay round-trip (full TagLib frame render, including header).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="ImmutableArray{T}"/> for <see cref="Data"/> so record equality compares payload bytes,
    /// not array references.
    /// </para>
    /// </remarks>
    public sealed record Id3v2SerializedFrame
    {
        /// <summary>
        /// Four-character frame id (for example <c>TIT2</c>, <c>TXXX</c>).
        /// </summary>
        public string FrameId { get; init; } = "";

        /// <summary>
        /// Raw frame bytes as returned by TagLib <c>Frame.Render(version)</c>.
        /// </summary>
        public ImmutableArray<byte> Data { get; init; } = [];
    }
}

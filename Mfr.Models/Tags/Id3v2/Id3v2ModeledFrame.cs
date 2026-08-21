using System.Collections.Immutable;
using Mfr.Utils;

namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// One modeled ID3v2 text-bearing frame in an <see cref="Id3v2TagData"/> snapshot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Singleton frames (for example <c>TIT2</c>) use <see cref="FrameId"/> only. Multi-instance frames
    /// (<c>COMM</c>, <c>USLT</c>, <c>TXXX</c>) also carry <see cref="Language"/> and/or <see cref="Description"/>
    /// as identity. Unmodeled frames (for example <c>APIC</c>) are omitted and left on disk by field-patch Apply.
    /// </para>
    /// </remarks>
    public sealed class Id3v2ModeledFrame : IEquatable<Id3v2ModeledFrame?>
    {
        /// <summary>
        /// Frame ids whose identity includes language and/or description (not <see cref="FrameId"/> alone).
        /// </summary>
        public static IReadOnlySet<string> MultiInstanceFrameIds { get; } =
            new HashSet<string>(StringComparer.Ordinal) { "COMM", "USLT", "TXXX" };

        /// <summary>
        /// Four-character frame id (for example <c>TIT2</c>, <c>COMM</c>).
        /// </summary>
        public string FrameId { get; init; } = "";

        /// <summary>
        /// ISO-639-2 language for <c>COMM</c>/<c>USLT</c>, or <see langword="null"/> when not applicable.
        /// </summary>
        public string? Language { get; init; }

        /// <summary>
        /// Description / content descriptor for <c>COMM</c>/<c>USLT</c>/<c>TXXX</c>, or <see langword="null"/> when not applicable.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Trimmed text payload values (singletons usually have one entry).
        /// </summary>
        public ImmutableArray<string> TextValues { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(Id3v2ModeledFrame? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!string.Equals(FrameId, other.FrameId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Language, other.Language, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Description, other.Description, StringComparison.Ordinal))
            {
                return false;
            }

            return OrdinalSequence.AreEqual(TextValues, other.TextValues);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as Id3v2ModeledFrame);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(FrameId, StringComparer.Ordinal);
            hash.Add(Language, StringComparer.Ordinal);
            hash.Add(Description, StringComparer.Ordinal);
            foreach (var value in TextValues)
            {
                hash.Add(value, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }
    }
}

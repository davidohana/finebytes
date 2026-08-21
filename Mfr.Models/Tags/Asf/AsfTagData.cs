using System.Collections.Immutable;

namespace Mfr.Models.Tags.Asf
{
    /// <summary>
    /// Detached snapshot of an ASF/WMA tag: Content Description fields plus extended descriptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are sorted by <see cref="AsfDescriptorRow.Name"/> for stable equality. Content Description
    /// fields use <see cref="AsfDescriptorNames.Title"/>, <see cref="AsfDescriptorNames.Author"/>, and
    /// <see cref="AsfDescriptorNames.Copyright"/>; remaining rows are extended content descriptors
    /// (TagLib string conversion).
    /// </para>
    /// </remarks>
    public sealed class AsfTagData : IEquatable<AsfTagData?>
    {
        /// <summary>
        /// ASF field rows (Content Description + extended descriptors) in canonical name order.
        /// </summary>
        public ImmutableArray<AsfDescriptorRow> Descriptors { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(AsfTagData? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Descriptors.Length != other.Descriptors.Length)
            {
                return false;
            }

            var comparer = EqualityComparer<AsfDescriptorRow>.Default;
            for (var i = 0; i < Descriptors.Length; i++)
            {
                if (!comparer.Equals(Descriptors[i], other.Descriptors[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AsfTagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var row in Descriptors)
            {
                hash.Add(row);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// One ASF name/value pair in the overlay snapshot.
    /// </summary>
    /// <param name="Name">Descriptor name.</param>
    /// <param name="Value">String form suitable for round-trip (see persistence layer).</param>
    public readonly record struct AsfDescriptorRow(string Name, string Value);
}

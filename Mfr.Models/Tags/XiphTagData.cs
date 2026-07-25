using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Detached Xiph / Vorbis comment snapshot as a known-key multimap (no binary blob).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only keys needed for <c>CommonAudioTag</c> projection and filters are stored. Unknown on-disk keys are left
    /// untouched by field-patch Apply. Rows are sorted by key then values for stable equality.
    /// </para>
    /// </remarks>
    public sealed class XiphTagData : IEquatable<XiphTagData?>
    {
        /// <summary>
        /// Known comment fields present in this block.
        /// </summary>
        public ImmutableArray<TextFieldRow> Fields { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(XiphTagData? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return TextFieldRowEquality.Equals(Fields, other.Fields);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as XiphTagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return TextFieldRowEquality.GetHashCode(Fields);
        }
    }
}

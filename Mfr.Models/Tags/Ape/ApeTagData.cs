using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Detached APEv2 snapshot as a known text-key map (no binary blob).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only modeled keys are stored; unknown on-disk items are left for field-patch Apply. Rows are sorted for
    /// stable equality.
    /// </para>
    /// </remarks>
    public sealed class ApeTagData : IEquatable<ApeTagData?>
    {
        /// <summary>
        /// Known text items present in this block.
        /// </summary>
        public ImmutableArray<TextFieldRow> Fields { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(ApeTagData? other)
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
            return Equals(obj as ApeTagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return TextFieldRowEquality.GetHashCode(Fields);
        }
    }
}

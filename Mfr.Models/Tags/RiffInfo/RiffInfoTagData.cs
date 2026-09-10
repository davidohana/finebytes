using System.Collections.Immutable;

namespace Mfr.Models.Tags.RiffInfo
{
    /// <summary>
    /// Detached RIFF LIST INFO snapshot as INFO fourCC → string (no binary blob).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are sorted by key for stable equality. Unknown INFO keys stay on disk under field-patch Apply.
    /// Modeled keys are listed in <see cref="RiffInfoKnownKeys.All"/>.
    /// </para>
    /// </remarks>
    public sealed class RiffInfoTagData : IEquatable<RiffInfoTagData?>
    {
        /// <summary>
        /// Known INFO key/value pairs.
        /// </summary>
        public ImmutableArray<RiffInfoFieldRow> Fields { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(RiffInfoTagData? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Fields.Length != other.Fields.Length)
            {
                return false;
            }

            for (var i = 0; i < Fields.Length; i++)
            {
                if (!Equals(Fields[i], other.Fields[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as RiffInfoTagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var row in Fields)
            {
                hash.Add(row);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// One RIFF INFO chunk key and its string value.
    /// </summary>
    /// <param name="Key">Four-character INFO id (for example <c>INAM</c>).</param>
    /// <param name="Value">Trimmed text value.</param>
    public readonly record struct RiffInfoFieldRow(string Key, string Value)
    {
        /// <summary>
        /// Orders rows by key, then value, for stable equality and Apply ordering.
        /// </summary>
        /// <param name="left">First row.</param>
        /// <param name="right">Second row.</param>
        /// <returns>Negative, zero, or positive per ordinal key then value.</returns>
        public static int Compare(RiffInfoFieldRow left, RiffInfoFieldRow right)
        {
            var byKey = string.CompareOrdinal(left.Key, right.Key);
            return byKey != 0 ? byKey : string.CompareOrdinal(left.Value, right.Value);
        }
    }
}

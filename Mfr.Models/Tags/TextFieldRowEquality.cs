using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Structural equality helpers for sorted <see cref="TextFieldRow"/> collections.
    /// </summary>
    internal static class TextFieldRowEquality
    {
        /// <summary>
        /// Returns whether two field-row arrays are ordinal-equal in order and content.
        /// </summary>
        public static bool Equals(ImmutableArray<TextFieldRow> left, ImmutableArray<TextFieldRow> right)
        {
            if (left.Length != right.Length)
                return false;

            for (var i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i].Key, right[i].Key, StringComparison.Ordinal))
                    return false;

                if (left[i].Values.Length != right[i].Values.Length)
                    return false;

                for (var j = 0; j < left[i].Values.Length; j++)
                {
                    if (!string.Equals(left[i].Values[j], right[i].Values[j], StringComparison.Ordinal))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes a content hash for sorted field rows.
        /// </summary>
        public static int GetHashCode(ImmutableArray<TextFieldRow> fields)
        {
            var hash = new HashCode();
            foreach (var row in fields)
            {
                hash.Add(row.Key, StringComparer.Ordinal);
                foreach (var value in row.Values)
                    hash.Add(value, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }
    }
}

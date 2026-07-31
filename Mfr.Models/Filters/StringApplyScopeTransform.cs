using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Applies optional <see cref="StringApplyScope"/> around a string filter callback.
    /// </summary>
    internal static class StringApplyScopeTransform
    {
        /// <summary>
        /// Runs <paramref name="transform"/> on the full string or a derived slice, returning the value to write back.
        /// </summary>
        internal static string Apply(
            StringApplyScope? scope,
            string value,
            RenameItem item,
            Func<string, RenameItem, string> transform)
        {
            if (scope is null)
                return transform(value, item);

            if (scope is SubstringApplyScope sub)
                return _ApplySubstring(sub, value, item, transform);

            if (scope is TokenApplyScope token)
                return _ApplyToken(token, value, item, transform);

            throw new NotSupportedException($"Unsupported apply scope '{scope.GetType().Name}'.");
        }

        private static string _ApplySubstring(
            SubstringApplyScope scope,
            string value,
            RenameItem item,
            Func<string, RenameItem, string> transform)
        {
            var length = value.Length;
            if (length == 0)
                return transform(value, item);

            var iStart = _ResolveIndex(scope.StartPosition, scope.StartAnchor, length);
            var iEnd = _ResolveIndex(scope.EndPosition, scope.EndAnchor, length);
            if (iStart > iEnd)
                (iStart, iEnd) = (iEnd, iStart);

            var slice = value.Substring(iStart, iEnd - iStart + 1);
            var transformedSlice = transform(slice, item);
            return string.Concat(value.AsSpan(0, iStart), transformedSlice, value.AsSpan(iEnd + 1));
        }

        private static int _ResolveIndex(int oneBasedPosition, StringScopeAnchor anchor, int length)
        {
            var p = oneBasedPosition < 1 ? 1 : oneBasedPosition;
            if (anchor == StringScopeAnchor.Left)
            {
                if (p > length)
                    p = length;

                return p - 1;
            }

            if (p > length)
                p = length;

            return length - p;
        }

        private static string _ApplyToken(
            TokenApplyScope scope,
            string value,
            RenameItem item,
            Func<string, RenameItem, string> transform)
        {
            Require.That(
                scope.TokenNumber >= 1,
                $"Token apply-scope tokenNumber must be at least 1 (got {scope.TokenNumber}).",
                nameof(scope));

            Require.That(
                !string.IsNullOrEmpty(scope.Separator),
                "Token apply-scope separator must be non-empty.",
                nameof(scope));

            var parts = value.Split(scope.Separator, StringSplitOptions.None);
            if (scope.TokenNumber > parts.Length)
                return value;

            var index = scope.TokenNumber - 1;
            parts[index] = transform(parts[index], item);
            return string.Join(scope.Separator, parts);
        }
    }
}

using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Resolves the <c>&lt;file-date&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Argument shape: <c>format</c> or <c>format,date-type</c>. <c>date-type</c> is
    /// <c>0</c> for creation (default), <c>1</c> for last write, <c>2</c> for last access.
    /// Default format when none supplied is <c>dd-MM-yyyy</c>.
    /// </para>
    /// </remarks>
    internal sealed class FileDateToken : IFormatToken
    {
        private const string DefaultFormat = "dd-MM-yyyy";

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-date"];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Thrown when an unsupported date type is supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var (format, dateType) = _ParseArg(arg);
            var date = dateType switch
            {
                0 => item.Original.CreationTime,
                1 => item.Original.LastWriteTime,
                2 => item.Original.LastAccessTime,
                _ => throw new NotSupportedException($"File date type '{dateType}' is not supported.")
            };

            return date.ToString(format, CultureInfo.InvariantCulture);
        }

        private static (string Format, int DateType) _ParseArg(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return (DefaultFormat, 0);

            var lastComma = arg.LastIndexOf(',');
            if (lastComma < 0)
                return (arg, 0);

            var formatPart = arg[..lastComma];
            var dateTypePart = arg[(lastComma + 1)..].Trim();
            var format = string.IsNullOrWhiteSpace(formatPart) ? DefaultFormat : formatPart;
            var dateType = string.IsNullOrEmpty(dateTypePart)
                ? 0
                : int.Parse(dateTypePart, CultureInfo.InvariantCulture);
            return (format, dateType);
        }
    }
}

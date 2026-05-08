using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Parsed arguments for <c>&lt;file-date&gt;</c>.
    /// </summary>
    /// <param name="Format">.NET date format string.</param>
    /// <param name="DateType"><c>0</c> creation, <c>1</c> last write, <c>2</c> last access.</param>
    internal sealed record FileDateFormatOptions(string Format, int DateType)
    {
        private const string DefaultFormat = "dd-MM-yyyy";

        /// <summary>
        /// Parses <c>format</c> or <c>format,date-type</c>, using the last comma as separator for <c>date-type</c>.
        /// </summary>
        /// <param name="arg">Raw argument text.</param>
        internal static FileDateFormatOptions Parse(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return new FileDateFormatOptions(Format: DefaultFormat, DateType: 0);

            var lastComma = arg.LastIndexOf(',');
            if (lastComma < 0)
                return new FileDateFormatOptions(Format: arg, DateType: 0);

            var formatPart = arg[..lastComma];
            var dateTypePart = arg[(lastComma + 1)..].Trim();
            var format = string.IsNullOrWhiteSpace(formatPart) ? DefaultFormat : formatPart;
            var dateType = string.IsNullOrEmpty(dateTypePart)
                ? 0
                : int.Parse(dateTypePart, CultureInfo.InvariantCulture);
            return new FileDateFormatOptions(Format: format, DateType: dateType);
        }
    }

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
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["file-date"];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Thrown when an unsupported date type is supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var options = FileDateFormatOptions.Parse(arg);
            var date = options.DateType switch
            {
                0 => item.Original.CreationTime,
                1 => item.Original.LastWriteTime,
                2 => item.Original.LastAccessTime,
                _ => throw new NotSupportedException($"File date type '{options.DateType}' is not supported.")
            };

            return date.ToString(options.Format, CultureInfo.InvariantCulture);
        }
    }
}

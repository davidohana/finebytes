using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Exif
{
    /// <summary>
    /// Resolves the <c>&lt;exif:source,name&gt;</c> escape-hatch token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Splits on the first comma. <c>source</c> must be a known directory alias
    /// (<see cref="ExifData.SourceAliases"/>). <c>name</c> is a MetadataExtractor tag name
    /// (for example <c>Make</c> or <c>Date/Time Original</c>) or a decimal tag id (for example <c>271</c>).
    /// Missing tags expand empty.
    /// </para>
    /// </remarks>
    internal sealed class ExifToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;exif&gt;</c>.
        /// </summary>
        /// <param name="Source">Canonical directory alias.</param>
        /// <param name="Name">Tag name or decimal id.</param>
        private sealed record Options(string Source, string Name);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["exif"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the fragment after ':' is malformed or <c>source</c> is not a known alias.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var options = _ParseOptions(FormatOptionsParsing.TokenDisplayName(this), tokenArgs);
            return item =>
            {
                item.EnsureImagePropertiesLoaded();
                return ExifDataFormatting.FormatExtendedTag(item.Original.Exif, options.Source, options.Name);
            };
        }

        private static Options _ParseOptions(string tokenDisplayName, string tokenArgs)
        {
            Require.That(
                !string.IsNullOrWhiteSpace(tokenArgs),
                $"{tokenDisplayName} requires arguments: source and name separated by a comma "
                    + "(for example 'Exif,Make' or 'ExifSub,36867').",
                nameof(tokenArgs)
            );

            var trimmed = tokenArgs.Trim();
            var firstComma = trimmed.IndexOf(',');
            Require.That(
                firstComma >= 0,
                $"{tokenDisplayName} requires source and name separated by a comma "
                    + "(for example 'Exif,Make' or 'ExifSub,36867').",
                nameof(tokenArgs)
            );

            var source = trimmed[..firstComma].Trim();
            var name = trimmed[(firstComma + 1)..].Trim();

            Require.That(
                source.Length > 0,
                $"{tokenDisplayName} source must not be empty (expected {FormatOptionsParsing.FormatExpectedKeywords(ExifData.SourceAliases)}).",
                nameof(tokenArgs)
            );

            Require.That(
                name.Length > 0,
                $"{tokenDisplayName} name must not be empty after the comma (tag name or decimal id).",
                nameof(tokenArgs)
            );

            if (!ExifData.IsKnownSourceAlias(source))
            {
                throw new ArgumentException(
                    $"{tokenDisplayName} invalid source '{source}' "
                        + $"(expected {FormatOptionsParsing.FormatExpectedKeywords(ExifData.SourceAliases)}).",
                    nameof(tokenArgs)
                );
            }

            return new Options(Source: source, Name: name);
        }
    }
}

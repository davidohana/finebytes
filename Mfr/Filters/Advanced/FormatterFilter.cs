using System.Globalization;
using System.Text.RegularExpressions;
namespace Mfr.Models.Filters.Advanced
{
    /// <summary>
    /// Options for formatter templates.
    /// </summary>
    /// <param name="Template">Template expression with formatter tokens.</param>
    public sealed record FormatterOptions(string Template);

    /// <summary>
    /// Applies formatter template tokens.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Formatter options.</param>
    public sealed partial record FormatterFilter(
        bool Enabled,
        FilterTarget Target,
        FormatterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Formatter";

        internal override string TransformSegment(string segment, RenameItem item)
        {
            return _TokenRegex().Replace(Options.Template, m => _ResolveToken(m.Groups[1].Value, item));
        }

        private static string _ResolveToken(string tokenInner, RenameItem item)
        {
            var parts = tokenInner.Split(':', 2);
            var name = parts[0];
            var arg = parts.Length == 2 ? parts[1] : "";

            return name switch
            {
                "file-name" => item.Original.Prefix,
                "file-ext" => item.Original.Extension,
                "ext" => item.Original.Extension,
                "full-name" => item.Original.Prefix + item.Original.Extension,
                "parent-folder" => Path.GetFileName(item.Original.DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                "full-path" => item.Original.FullPath,
                "now" => string.IsNullOrWhiteSpace(arg) ? DateTimeOffset.UtcNow.ToString("o") : DateTimeOffset.UtcNow.ToString(arg),
                "counter" => _ResolveCounterToken(arg, item),
                _ => throw new NotSupportedException($"Phase 1 formatter token '{name}' is not supported.")
            };
        }

        private static string _ResolveCounterToken(string arg, RenameItem item)
        {
            var parts = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                throw new InvalidOperationException($"Invalid counter token arg '{arg}'. Expected 5 comma-separated params.");
            }

            var start = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var step = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var reset = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var width = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var pad = int.Parse(parts[4], CultureInfo.InvariantCulture);

            var n = reset == 1 ? item.Original.InFolderIndex : item.Original.GlobalIndex;
            var value = start + ((long)step * n);
            var raw = value.ToString(CultureInfo.InvariantCulture);
            if (width <= 0)
            {
                return raw;
            }

            var padChar = pad == 0 ? '0' : ' ';
            return raw.PadLeft(width, padChar);
        }

        [GeneratedRegex(@"<([^<>]+)>", RegexOptions.Compiled)]
        private static partial Regex _TokenRegex();
    }
}

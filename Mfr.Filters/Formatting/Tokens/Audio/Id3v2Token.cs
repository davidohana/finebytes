using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Resolves the MFR7-compatible <c>&lt;id3v2:field-code&gt;</c> token (ID3v2 Custom Field).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads one modeled ID3v2 frame from <see cref="FileMeta.AudioTagOverlay"/> on the
    /// preview snapshot. <c>field-code</c> is the four-character frame id (for example <c>TALB</c>,
    /// <c>TIT2</c>, <c>TXXX</c>).
    /// </para>
    /// <para>
    /// Bare <c>&lt;id3v2:TXXX&gt;</c> returns the first <c>TXXX</c> frame in overlay order (MFR7 had no
    /// content-descriptor picker). <c>&lt;id3v2:TXXX:catalog&gt;</c> targets that content descriptor.
    /// <c>COMM</c>/<c>USLT</c> bare ids resolve the primary instance; an optional content-descriptor
    /// suffix selects a non-primary instance.
    /// </para>
    /// </remarks>
    internal sealed class Id3v2Token : IFormatToken
    {
        /// <summary>
        /// Parsed frame identity for one compiled token.
        /// </summary>
        /// <param name="FrameId">Normalized four-character frame id.</param>
        /// <param name="ContentDescriptor">
        /// Optional multi-instance content descriptor (<c>TXXX</c>/<c>COMM</c>/<c>USLT</c>).
        /// For <c>TXXX</c>, <see langword="null"/> means first frame; for <c>COMM</c>/<c>USLT</c>,
        /// <see langword="null"/> means primary. Unused for singletons.
        /// </param>
        private sealed record Options(string FrameId, string? ContentDescriptor);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["id3v2"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when <paramref name="tokenArgs"/> is missing or malformed.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var options = _ParseOptions(FormatOptionsParsing.TokenDisplayName(this), tokenArgs);
            return item =>
            {
                item.EnsureEmbeddedTagsLoaded();
                var overlay = item.Preview.AudioTagOverlay;
                var isBareTxxx =
                    string.Equals(options.FrameId, "TXXX", StringComparison.Ordinal)
                    && options.ContentDescriptor is null;
                if (isBareTxxx)
                    return _FirstTxxxText(overlay);

                return AudioOverlayBlockFieldIo.GetId3v2FrameString(
                    overlay,
                    options.FrameId,
                    language: null,
                    description: options.ContentDescriptor
                );
            };
        }

        private static Options _ParseOptions(string tokenDisplayName, string tokenArgs)
        {
            if (string.IsNullOrWhiteSpace(tokenArgs))
            {
                throw new ArgumentException(
                    $"{tokenDisplayName} requires a field-code argument (for example TXXX or TALB).",
                    nameof(tokenArgs)
                );
            }

            var trimmed = tokenArgs.Trim();
            var colon = trimmed.IndexOf(':');
            var frameIdPart = colon < 0 ? trimmed : trimmed[..colon];
            var remainder = colon < 0 ? null : trimmed[(colon + 1)..];

            var frameId = frameIdPart.Trim().ToUpperInvariant();
            if (frameId.Length == 0)
            {
                throw new ArgumentException($"{tokenDisplayName} field-code is missing a frame id.", nameof(tokenArgs));
            }

            var contentDescriptor = string.IsNullOrWhiteSpace(remainder) ? null : remainder;
            var allowsContentDescriptor =
                string.Equals(frameId, "TXXX", StringComparison.Ordinal)
                || string.Equals(frameId, "COMM", StringComparison.Ordinal)
                || string.Equals(frameId, "USLT", StringComparison.Ordinal);

            if (contentDescriptor is not null && !allowsContentDescriptor)
            {
                throw new ArgumentException(
                    $"{tokenDisplayName} frame '{frameId}' does not accept a content-descriptor suffix.",
                    nameof(tokenArgs)
                );
            }

            return new Options(frameId, contentDescriptor);
        }

        private static string _FirstTxxxText(AudioTagOverlay overlay)
        {
            var block = overlay.Id3v2;
            if (block is null)
                return string.Empty;

            foreach (var frame in block.Frames)
            {
                if (!string.Equals(frame.FrameId, "TXXX", StringComparison.Ordinal))
                    continue;

                return DelimitedText.Join(frame.TextValues);
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Resolves <c>&lt;id3v2-version&gt;</c> — the ID3v2 tag minor version from the preview overlay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns MFR7-style text such as <c>2.3</c> or <c>2.4</c> from
    /// <see cref="Models.Tags.Id3v2.Id3v2TagData.Version"/>. Empty when no ID3v2 block is present.
    /// </para>
    /// </remarks>
    internal sealed class Id3v2VersionToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["id3v2-version"];

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureEmbeddedTagsLoaded();
                var block = item.Preview.AudioTagOverlay.Id3v2;
                if (block is null)
                    return string.Empty;

                return $"2.{block.Version}";
            };
        }
    }
}

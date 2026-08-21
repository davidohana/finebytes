using System.Text.Json.Serialization;
using Mfr.Filters.Formatting;
using Mfr.Models.Tags;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Options for <see cref="Id3v2FieldSetterFilter"/> (legacy ID3v2 Field Setter style).
    /// </summary>
    /// <param name="FrameId">
    /// Four-character ID3v2 frame id (case-insensitive; stored uppercase). Required.
    /// </param>
    /// <param name="Text">
    /// Plain text, or a formatter template when it contains at least one balanced <c>&lt;...&gt;</c> span
    /// that looks like a formatter token (same rules as the <see cref="FormatterFilter"/> template language).
    /// </param>
    /// <param name="OnlyIfEmpty">
    /// When <c>true</c>, set the frame only when the current preview value is empty; when <c>false</c>, always set.
    /// </param>
    /// <param name="Language">
    /// ISO-639-2 language for <c>COMM</c>/<c>USLT</c>, or <see langword="null"/> when not applicable.
    /// </param>
    /// <param name="Description">
    /// Content descriptor for <c>COMM</c>/<c>USLT</c>/<c>TXXX</c>, or <see langword="null"/> for the primary instance.
    /// </param>
    public sealed record Id3v2FieldSetterOptions(
        [property: JsonPropertyName("frameId")] string FrameId,
        [property: JsonPropertyName("text")] string Text = "",
        [property: JsonPropertyName("onlyIfEmpty")] bool OnlyIfEmpty = false,
        [property: JsonPropertyName("language")] string? Language = null,
        [property: JsonPropertyName("description")] string? Description = null
    );

    /// <summary>
    /// Sets one modeled ID3v2 text frame on each MPEG file row (legacy ID3v2 Field Setter).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates an ID3v2.3 block when absent. Existing tag versions are preserved on patch; writing a v2.4-only
    /// frame into a lower-version tag is a preview error. Non-MPEG containers and directory rows fail preview.
    /// Empty resolved <c>text</c> clears that frame instance.
    /// </para>
    /// <para>
    /// Individual frames can also be set with a string filter (for example <see cref="FormatterFilter"/>) whose
    /// target is <see cref="Id3v2FrameTarget"/>; this filter adds <c>onlyIfEmpty</c> and a dedicated preset shape.
    /// </para>
    /// </remarks>
    /// <param name="Options">Frame identity, value, and fill-if-empty behavior.</param>
    public sealed record Id3v2FieldSetterFilter(Id3v2FieldSetterOptions Options) : BaseFilter
    {
        private Formatter _textFormatter = FormatStringCompiler.EmptyFormatter;
        private string _normalizedFrameId = string.Empty;

        /// <inheritdoc />
        public override string Type => "Id3v2FieldSetter";

        /// <inheritdoc />
        /// <exception cref="ArgumentException"><see cref="Id3v2FieldSetterOptions.FrameId"/> is missing or whitespace.</exception>
        protected override void _Setup()
        {
            if (string.IsNullOrWhiteSpace(Options.FrameId))
            {
                throw new ArgumentException("Id3v2FieldSetter requires a non-empty 'frameId'.", nameof(Options));
            }

            _normalizedFrameId = Options.FrameId.Trim().ToUpperInvariant();
            _textFormatter = FormatStringCompiler.ContainsLikelyFormatTokens(Options.Text)
                ? FormatStringCompiler.Compile(Options.Text)
                : _ => Options.Text;
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v2);

            var overlay = item.Preview.AudioTagOverlay;
            var current = AudioOverlayBlockFieldIo.GetId3v2FrameString(
                overlay,
                _normalizedFrameId,
                Options.Language,
                Options.Description
            );

            if (Options.OnlyIfEmpty && !string.IsNullOrWhiteSpace(current))
                return;

            var resolved = _textFormatter(item);
            AudioOverlayBlockFieldIo.SetId3v2FrameString(
                overlay,
                _normalizedFrameId,
                resolved,
                Options.Language,
                Options.Description
            );
        }
    }
}

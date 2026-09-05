using Mfr.Models.Tags;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Checkbox row metadata for one <see cref="AudioTagBlockKind"/> in Tag Remover.
    /// </summary>
    /// <param name="Kind">Modeled tag block kind.</param>
    /// <param name="DisplayName">User-visible checkbox label.</param>
    /// <param name="Tip">Short per-kind tooltip.</param>
    internal sealed record AudioTagBlockKindChoice(AudioTagBlockKind Kind, string DisplayName, string Tip)
    {
        /// <summary>
        /// All modeled block kinds in declaration order, with UI labels and tips.
        /// </summary>
        public static IReadOnlyList<AudioTagBlockKindChoice> All { get; } =
        [
            new(AudioTagBlockKind.Id3v1, "ID3v1", "ID3v1 trailer (MP3)."),
            new(AudioTagBlockKind.Id3v2, "ID3v2", "ID3v2 frames (MP3)."),
            new(AudioTagBlockKind.Xiph, "Xiph comment", "Xiph/Vorbis comment (FLAC, Ogg, Opus)."),
            new(AudioTagBlockKind.Ape, "APEv2", "APEv2 tag (FLAC, Monkey's Audio)."),
            new(AudioTagBlockKind.Apple, "Apple/iTunes", "Apple/iTunes ilst metadata (MP4, M4A)."),
            new(AudioTagBlockKind.Asf, "ASF", "ASF extended content descriptors (WMA)."),
            new(AudioTagBlockKind.RiffInfo, "RIFF INFO", "RIFF LIST/INFO chunk (WAV)."),
        ];
    }
}

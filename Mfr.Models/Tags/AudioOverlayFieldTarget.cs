namespace Mfr.Models.Tags
{
    /// <summary>
    /// Targets one field on <see cref="FileMeta.AudioTagOverlay"/>; string filters read/write text or decimal-digit numeric strings.
    /// </summary>
    /// <param name="Field">Which overlay property is addressed.</param>
    public sealed record AudioOverlayFieldTarget(AudioOverlayField Field) : FilterTarget;
}

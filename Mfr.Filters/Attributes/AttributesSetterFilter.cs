using System.Text.Json.Serialization;
using Mfr.Models;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Whether an attribute flag is turned on, off, or left unchanged.
    /// </summary>
    public enum AttributeTriState
    {
        /// <summary>
        /// Set the attribute.
        /// </summary>
        Set,

        /// <summary>
        /// Clear the attribute.
        /// </summary>
        Clear,

        /// <summary>
        /// Leave the attribute as on the current preview.
        /// </summary>
        Keep
    }

    /// <summary>
    /// Options for batch attribute changes (read-only, hidden, archive, system).
    /// </summary>
    /// <param name="ReadOnly">Read-only flag behavior.</param>
    /// <param name="Hidden">Hidden flag behavior.</param>
    /// <param name="Archive">Archive flag behavior.</param>
    /// <param name="System">System flag behavior.</param>
    public sealed record AttributesSetterOptions(
        [property: JsonPropertyName("readOnly")] AttributeTriState ReadOnly,
        [property: JsonPropertyName("hidden")] AttributeTriState Hidden,
        [property: JsonPropertyName("archive")] AttributeTriState Archive,
        [property: JsonPropertyName("system")] AttributeTriState System);

    /// <summary>
    /// Sets or clears filesystem attributes on each rename item (preview and commit).
    /// </summary>
    /// <param name="Target">Must serialize as <see cref="AttributesTarget"/>.</param>
    /// <param name="Options">Per-flag tri-state options.</param>
    public sealed record AttributesSetterFilter(
        FilterTarget Target,
        AttributesSetterOptions Options) : BaseFilter(Target)
    {
        /// <inheritdoc />
        public override string Type => "AttributesSetter";

        /// <inheritdoc />
        protected override void _Setup()
        {
            if (Target is not AttributesTarget)
            {
                throw new InvalidOperationException(
                    "AttributesSetter requires target with family 'Attributes'.");
            }
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            var attrs = item.Preview.Attributes;
            attrs = _ApplyOne(attrs, Options.ReadOnly, FileAttributes.ReadOnly);
            attrs = _ApplyOne(attrs, Options.Hidden, FileAttributes.Hidden);
            attrs = _ApplyOne(attrs, Options.Archive, FileAttributes.Archive);
            attrs = _ApplyOne(attrs, Options.System, FileAttributes.System);
            item.Preview.Attributes = attrs;
        }

        private static FileAttributes _ApplyOne(
            FileAttributes current,
            AttributeTriState mode,
            FileAttributes flag)
        {
            return mode switch
            {
                AttributeTriState.Keep => current,
                AttributeTriState.Set => current | flag,
                AttributeTriState.Clear => current & ~flag,
                _ => current
            };
        }
    }
}

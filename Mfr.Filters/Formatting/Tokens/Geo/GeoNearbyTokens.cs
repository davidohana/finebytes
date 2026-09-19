using Mfr.Models.RenameList.Fields.Jpeg;

namespace Mfr.Filters.Formatting.Tokens.Geo
{
    /// <summary>
    /// Shared implementation for no-arg <c>geo-*</c> formatter tokens.
    /// </summary>
    internal abstract class GeoNearbyTokenBase(IReadOnlyList<string> names, GeoNamesField propertyField)
        : IFormatToken,
            IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = JpegRenameListFields.Group;
            propertyKey = JpegNearbyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureGeoNamesLoaded();
                return GeoNamesFormatting.Format(item.Original.GeoNames, propertyField);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Nearby Place", "Image\\Nearby", "Nearby place name from GeoNames", "geo-place")]
    internal sealed class GeoPlaceToken : GeoNearbyTokenBase
    {
        /// <summary>Registers <c>&lt;geo-place&gt;</c>.</summary>
        public GeoPlaceToken()
            : base(["geo-place"], GeoNamesField.Place) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Nearby Region", "Image\\Nearby", "Nearby admin region from GeoNames", "geo-region")]
    internal sealed class GeoRegionToken : GeoNearbyTokenBase
    {
        /// <summary>Registers <c>&lt;geo-region&gt;</c>.</summary>
        public GeoRegionToken()
            : base(["geo-region"], GeoNamesField.Region) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Nearby Country", "Image\\Nearby", "Nearby country name from GeoNames", "geo-country")]
    internal sealed class GeoCountryToken : GeoNearbyTokenBase
    {
        /// <summary>Registers <c>&lt;geo-country&gt;</c>.</summary>
        public GeoCountryToken()
            : base(["geo-country"], GeoNamesField.Country) { }
    }
}

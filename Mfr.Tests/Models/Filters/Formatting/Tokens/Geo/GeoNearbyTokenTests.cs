using Mfr.Filters.Formatting.Tokens.Geo;
using Mfr.Models.RenameList.Fields.Jpeg;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Geo
{
    /// <summary>
    /// Unit tests for <c>geo-*</c> formatter tokens.
    /// </summary>
    public sealed class GeoNearbyTokenTests
    {
        [Fact]
        public void Geo_tokens_format_seeded_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.GeoNames = new GeoNamesInfo
                {
                    Place = "Haifa",
                    Region = "Haifa District",
                    Country = "Israel",
                }
            );

            Assert.Equal("Haifa", new GeoPlaceToken().Compile(string.Empty)(item));
            Assert.Equal("Haifa District", new GeoRegionToken().Compile(string.Empty)(item));
            Assert.Equal("Israel", new GeoCountryToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Geo_tokens_empty_when_no_gps_and_empty_snapshot()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.Exif = new ExifData();
                m.GeoNames = new GeoNamesInfo();
            });

            Assert.Equal(string.Empty, new GeoPlaceToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new GeoRegionToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new GeoCountryToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Geo_tokens_map_to_nearby_rename_list_fields()
        {
            Assert.True(new GeoPlaceToken().TryGetFixedField(out var group, out var placeKey));
            Assert.Equal(JpegRenameListFields.Group, group);
            Assert.Equal(JpegRenameListFields.Key.NearbyPlace, placeKey);

            Assert.True(new GeoRegionToken().TryGetFixedField(out _, out var regionKey));
            Assert.Equal(JpegRenameListFields.Key.NearbyRegion, regionKey);

            Assert.True(new GeoCountryToken().TryGetFixedField(out _, out var countryKey));
            Assert.Equal(JpegRenameListFields.Key.NearbyCountry, countryKey);
        }

        [Fact]
        public void Geo_tokens_rethrow_stored_geonames_load_error()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            item.ClearGeoNamesCache();
            item.MarkGeoNamesLoadAttempted();
            item.SetGeoNamesLoadError(new InvalidOperationException("GeoNames rate limit exceeded."));

            var ex = Assert.Throws<InvalidOperationException>(() => new GeoPlaceToken().Compile(string.Empty)(item));
            Assert.Equal("GeoNames rate limit exceeded.", ex.Message);
        }
    }
}

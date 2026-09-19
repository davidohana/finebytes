using Mfr.Metadata.GeoNames;

namespace Mfr.Tests.Metadata.GeoNames
{
    /// <summary>
    /// Unit tests for L3 disk write debouncing on <see cref="GeoNamesResponseCache"/>.
    /// </summary>
    public sealed class GeoNamesResponseCacheTests : IDisposable
    {
        private readonly string _cacheDir;
        private readonly string _cachePath;

        public GeoNamesResponseCacheTests()
        {
            _cacheDir = Path.Combine(Path.GetTempPath(), "mfr-geonames-cache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_cacheDir);
            _cachePath = Path.Combine(_cacheDir, "geonames-cache.json");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    Directory.Delete(_cacheDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // best-effort temp cleanup
            }
        }

        [Fact]
        public void Put_writes_disk_immediately()
        {
            var cache = new GeoNamesResponseCache(_cachePath);
            cache.Put("32.8231|35.0444|fbmfr", new GeoNamesInfo { Place = "Haifa" });

            Assert.True(File.Exists(_cachePath));
            Assert.Contains("Haifa", File.ReadAllText(_cachePath), StringComparison.Ordinal);
        }

        [Fact]
        public void L3_hit_does_not_rewrite_disk_until_Flush()
        {
            var seed = new GeoNamesResponseCache(_cachePath);
            seed.Put("32.8231|35.0444|fbmfr", new GeoNamesInfo { Place = "Haifa", Country = "Israel" });

            var before = File.ReadAllBytes(_cachePath);
            Thread.Sleep(50);

            var cache = new GeoNamesResponseCache(_cachePath);
            Assert.True(cache.TryGet("32.8231|35.0444|fbmfr", out var info));
            Assert.Equal("Haifa", info.Place);
            Assert.Equal(before, File.ReadAllBytes(_cachePath));

            cache.Flush();
            var after = File.ReadAllBytes(_cachePath);
            Assert.NotEqual(before, after);
        }

        [Fact]
        public void Flush_noop_when_not_dirty()
        {
            var cache = new GeoNamesResponseCache(_cachePath);
            cache.Put("1|2|fbmfr", new GeoNamesInfo { Place = "A" });
            var afterPut = File.GetLastWriteTimeUtc(_cachePath);

            Thread.Sleep(20);
            cache.Flush();
            Assert.Equal(afterPut, File.GetLastWriteTimeUtc(_cachePath));
        }
    }
}

using Mfr.Models;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="ConfigLoader"/>.
    /// </summary>
    public sealed class ConfigLoaderTests : IDisposable
    {
        private static readonly Lock Gate = new();

        /// <summary>
        /// Verifies a valid config file updates <see cref="MfrSettings.MaxListFileLineLength"/>.
        /// </summary>
        [Fact]
        public void Load_sets_max_line_length_when_property_present()
        {
            lock (Gate)
            {
                try
                {
                    var path = Path.GetTempFileName();
                    try
                    {
                        File.WriteAllText(path, /*lang=json,strict*/ """{"maxListFileLineLength":"2048"}""");
                        ConfigLoader.Load(path);
                        Assert.Equal(2048, ConfigLoader.Settings.MaxListFileLineLength);
                    }
                    finally
                    {
                        File.Delete(path);
                    }
                }
                finally
                {
                    ConfigLoader.ResetToDefaultsForTests();
                }
            }
        }

        /// <summary>
        /// Verifies out-of-range values are rejected.
        /// </summary>
        [Fact]
        public void Load_throws_when_value_below_minimum()
        {
            lock (Gate)
            {
                try
                {
                    var path = Path.GetTempFileName();
                    try
                    {
                        File.WriteAllText(path, /*lang=json,strict*/ """{"maxListFileLineLength":"0"}""");
                        var ex = Assert.Throws<InvalidDataException>(() => ConfigLoader.Load(path));
                        Assert.Contains("must be between", ex.Message);
                    }
                    finally
                    {
                        File.Delete(path);
                    }
                }
                finally
                {
                    ConfigLoader.ResetToDefaultsForTests();
                }
            }
        }

        public void Dispose()
        {
            lock (Gate)
            {
                ConfigLoader.ResetToDefaultsForTests();
            }
        }
    }
}

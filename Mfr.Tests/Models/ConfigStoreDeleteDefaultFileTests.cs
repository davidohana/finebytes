namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.DeleteDefaultFile"/>.
    /// </summary>
    public sealed class ConfigStoreDeleteDefaultFileTests
    {
        /// <summary>
        /// Verifies a missing file is a no-op.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_missing_is_noop()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-delete-missing-config-" + Guid.NewGuid() + ".json");
            ConfigStore.DeleteDefaultFile(path);
            Assert.False(File.Exists(path));
        }

        /// <summary>
        /// Verifies an existing config file is removed.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_removes_existing_file()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-delete-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, "{}");
            try
            {
                Assert.True(File.Exists(path));
                ConfigStore.DeleteDefaultFile(path);
                Assert.False(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}

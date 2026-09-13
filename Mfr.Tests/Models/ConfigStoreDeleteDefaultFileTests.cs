namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.DeleteDefaultFile"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfigStoreDeleteDefaultFileTests
    {
        /// <summary>
        /// Verifies a missing file is a no-op.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_missing_is_noop()
        {
            using var temp = ConfigStoreTempFile.Create();
            ConfigStore.DeleteDefaultFile(temp.Path);
            Assert.False(File.Exists(temp.Path));
        }

        /// <summary>
        /// Verifies an existing config file is removed.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_removes_existing_file()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent("{}");
            Assert.True(File.Exists(temp.Path));
            ConfigStore.DeleteDefaultFile(temp.Path);
            Assert.False(File.Exists(temp.Path));
        }
    }
}

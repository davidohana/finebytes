namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Resets <see cref="ConfigStore.Config"/> for isolated tests via an empty JSON document.
    /// </summary>
    public static class ConfigStoreTestReset
    {
        /// <summary>
        /// Loads <c>{}</c> into <see cref="ConfigStore"/> so section field initializers apply (defaults).
        /// </summary>
        public static void LoadEmpty()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-empty-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, """{}""");
            try
            {
                ConfigStore.Load(path);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

using Mfr.Filters;
using Mfr.Models.Config;

namespace Mfr.Engine.Config
{
    /// <summary>
    /// Pushes process config into Filters runtime settings.
    /// </summary>
    public static class FilterRuntimeConfig
    {
        /// <summary>
        /// Syncs <see cref="ListEntryLength.MaxLength"/> from <see cref="ConfigStore.Config"/>.
        /// </summary>
        /// <remarks>
        /// Call after <see cref="ConfigStore.Load"/> and after <see cref="ConfigStore.ApplyCliOverrides"/>.
        /// </remarks>
        public static void SyncFromConfigStore()
        {
            ListEntryLength.Configure(ConfigStore.Config.Filters.MaxListFileLineLength);
        }
    }
}

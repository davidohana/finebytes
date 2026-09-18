using Mfr.Engine.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Invokes a test Save override or <see cref="ConfigStore.Save"/>.
    /// </summary>
    internal static class ConfigStoreSave
    {
        /// <summary>
        /// Calls <paramref name="overrideSave"/> when set; otherwise <see cref="ConfigStore.Save"/>.
        /// </summary>
        /// <param name="overrideSave">Optional test hook that replaces disk save.</param>
        public static void Invoke(Action? overrideSave)
        {
            if (overrideSave is { } save)
            {
                save();
                return;
            }

            ConfigStore.Save();
        }
    }
}

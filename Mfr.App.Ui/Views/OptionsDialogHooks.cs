using Mfr.App.Ui.ViewModels.Options;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Optional overrides for MFR → Options (headless tests; leave null in production).
    /// </summary>
    internal sealed class OptionsDialogHooks
    {
        /// <summary>
        /// When set, replaces showing the Options dialog; return <see langword="true"/> to apply.
        /// </summary>
        public Func<OptionsDialogViewModel, Task<bool?>>? Show { get; init; }

        /// <summary>
        /// When set, replaces <see cref="ConfigStore.Save"/> after OK.
        /// </summary>
        public Action? SaveConfig { get; init; }
    }
}

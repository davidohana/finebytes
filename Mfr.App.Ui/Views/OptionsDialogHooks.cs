using Mfr.App.Ui.ViewModels.Options;
using Mfr.Engine.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Optional overrides for Tools → Options (headless tests; leave null in production).
    /// </summary>
    internal sealed class OptionsDialogHooks
    {
        /// <summary>
        /// When set, replaces showing the Options dialog; return <see langword="true"/> to apply.
        /// </summary>
        public Func<OptionsDialogViewModel, Task<bool?>>? Show { get; init; }

        /// <summary>
        /// When set, replaces <see cref="ConfigStore.Save"/> after OK (via <see cref="ConfigStoreSave"/>).
        /// </summary>
        public Action? SaveConfig { get; init; }

        /// <summary>
        /// When hooks are used, optional directory for rename-log trim on OK.
        /// <para>
        /// Production (no hooks) always trims <c>RenameLogStore.DefaultDirectoryPath</c>.
        /// Tests leave this <see langword="null"/> to skip AppData prune, or set a temp dir.
        /// </para>
        /// </summary>
        public string? PruneRenameLogDirectoryPath { get; init; }
    }
}

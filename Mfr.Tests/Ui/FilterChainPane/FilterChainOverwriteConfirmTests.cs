using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.Views.FilterChainPane;

namespace Mfr.Tests.Ui.FilterChainPane
{
    /// <summary>
    /// Overwrite-preset confirmation gates for Filter Chain save.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FilterChainOverwriteConfirmTests
    {
        public FilterChainOverwriteConfirmTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies overwrite confirms by default and proceeds when accepted.
        /// </summary>
        [AvaloniaFact]
        public async Task Overwrite_confirm_accept_proceeds()
        {
            var (view, owner) = _ShowView();
            var asked = 0;
            view.ConfirmOverwriteAsync = name =>
            {
                asked++;
                Assert.Equal("Mine", name);
                return Task.FromResult(true);
            };

            Assert.True(await view.ConfirmOverwritePresetAsync(owner, "Mine"));
            Assert.Equal(1, asked);
            owner.Close();
        }

        /// <summary>
        /// Verifies decline aborts overwrite.
        /// </summary>
        [AvaloniaFact]
        public async Task Overwrite_confirm_decline_aborts()
        {
            var (view, owner) = _ShowView();
            view.ConfirmOverwriteAsync = _ => Task.FromResult(false);

            Assert.False(await view.ConfirmOverwritePresetAsync(owner, "Mine"));
            owner.Close();
        }

        /// <summary>
        /// Verifies a suppressed OverwritePreset skips the confirm hook.
        /// </summary>
        [AvaloniaFact]
        public async Task Overwrite_suppressed_skips_confirm()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.OverwritePreset);
            var (view, owner) = _ShowView();
            var asked = 0;
            view.ConfirmOverwriteAsync = _ =>
            {
                asked++;
                return Task.FromResult(false);
            };

            Assert.True(await view.ConfirmOverwritePresetAsync(owner, "Mine"));
            Assert.Equal(0, asked);
            owner.Close();
        }

        private static (FilterChainView View, Window Owner) _ShowView()
        {
            var view = new FilterChainView();
            var owner = new Window { Content = view };
            owner.Show();
            owner.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return (view, owner);
        }
    }
}

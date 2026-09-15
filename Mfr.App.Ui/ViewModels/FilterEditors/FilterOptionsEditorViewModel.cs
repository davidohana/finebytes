using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.FilterEditors
{
    /// <summary>
    /// Base type for type-specific Filter Configuration option editors.
    /// </summary>
    public abstract partial class FilterOptionsEditorViewModel : ViewModelBase
    {
        private CancellationTokenSource? _liveListTextApplyCts;
        private Action? _pendingLiveListTextApply;

        /// <summary>
        /// Delay before applying multiline list-editor text to the step.
        /// <para>
        /// Zero applies immediately (tests). Production default is 150 ms so large pastes do not
        /// re-preview on every intermediate binding update. Pastes above
        /// <see cref="LargeListTextDebounceThreshold"/> use at least
        /// <see cref="LargeListTextApplyDebounceMilliseconds"/>.
        /// </para>
        /// </summary>
        public static int LiveListTextApplyDebounceMilliseconds { get; set; } = 150;

        /// <summary>
        /// Text length at which list editors use the longer apply debounce.
        /// </summary>
        public const int LargeListTextDebounceThreshold = 32_000;

        /// <summary>
        /// Minimum debounce for large list-editor pastes (ms).
        /// </summary>
        public const int LargeListTextApplyDebounceMilliseconds = 500;

        /// <summary>
        /// Text length at which list parse runs on a worker before applying on the UI context.
        /// </summary>
        public const int LargeListTextParseOffUiThreshold = 32_000;

        /// <summary>
        /// Initializes an options editor for one Filter Chain step.
        /// </summary>
        /// <param name="step">Filter Chain step being edited.</param>
        protected FilterOptionsEditorViewModel(FilterChainStepViewModel step)
        {
            ArgumentNullException.ThrowIfNull(step);
            Step = step;
        }

        /// <summary>
        /// Gets the Filter Chain step being edited.
        /// </summary>
        protected FilterChainStepViewModel Step { get; }

        /// <summary>
        /// Gets or sets whether the format-token picker catalog is expanded.
        /// <para>
        /// Shared Filter Configuration chrome; bound by <see cref="Views.FormatEditor.FormatTokenPickerPane"/>
        /// and kept in sync with <see cref="FilterEditorViewModel.FormatTokenPickerExpanded"/>.
        /// </para>
        /// </summary>
        [ObservableProperty]
        private bool _formatTokenPickerExpanded = true;

        /// <summary>
        /// Gets whether property setters should skip live option replace (sync-from-filter in progress).
        /// </summary>
        protected bool IsLoading { get; private set; }

        /// <summary>
        /// Runs <paramref name="load"/> without treating property changes as option applies.
        /// </summary>
        /// <param name="load">Copies current filter options into editor properties.</param>
        protected void LoadWithoutApplying(Action load)
        {
            ArgumentNullException.ThrowIfNull(load);
            IsLoading = true;
            try
            {
                load();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Truncates <paramref name="value"/> to an integer in <paramref name="minInclusive"/>..<paramref name="maxInclusive"/>.
        /// </summary>
        /// <param name="value">NumericUpDown binding value.</param>
        /// <param name="minInclusive">Lowest allowed integer.</param>
        /// <param name="maxInclusive">Highest allowed integer.</param>
        /// <returns>Clamped integer.</returns>
        protected static int ClampToInt(decimal value, int minInclusive, int maxInclusive)
        {
            return Math.Clamp((int)value, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Replaces the step filter when <paramref name="updated"/> differs from <paramref name="current"/>.
        /// </summary>
        /// <param name="current">Filter currently stored on the step.</param>
        /// <param name="updated">Candidate replacement.</param>
        protected void ApplyIfChanged(BaseFilter current, BaseFilter updated)
        {
            if (Equals(current, updated))
            {
                return;
            }

            Step.SetFilter(updated);
        }

        /// <summary>
        /// Schedules <paramref name="apply"/> after <see cref="LiveListTextApplyDebounceMilliseconds"/>.
        /// <para>Supersedes any prior pending list-text apply for this editor.</para>
        /// </summary>
        /// <param name="apply">Writes the current list text onto the applied step.</param>
        /// <param name="textLength">
        /// Current editor text length; large values stretch the debounce so Auto-Preview waits for the paste to settle.
        /// </param>
        protected void ScheduleLiveListTextApply(Action apply, int textLength = 0)
        {
            ArgumentNullException.ThrowIfNull(apply);

            _CancelLiveListTextApplyTimer();
            _pendingLiveListTextApply = apply;

            var delay = LiveListTextApplyDebounceMilliseconds;
            if (delay <= 0)
            {
                _pendingLiveListTextApply = null;
                apply();
                return;
            }

            if (textLength >= LargeListTextDebounceThreshold)
            {
                delay = Math.Max(delay, LargeListTextApplyDebounceMilliseconds);
            }

            var cts = new CancellationTokenSource();
            _liveListTextApplyCts = cts;
            var context = SynchronizationContext.Current;
            _ = _RunDebouncedLiveListTextApplyAsync(apply, delay, cts.Token, context);
        }

        /// <summary>
        /// Runs <paramref name="parse"/> on a worker when <paramref name="text"/> is large, then
        /// <paramref name="applyParsed"/> on the captured synchronization context when the text is still current.
        /// </summary>
        /// <typeparam name="TParsed">Parsed list payload type.</typeparam>
        /// <param name="text">Editor text snapshot used for parse and staleness checks.</param>
        /// <param name="getCurrentText">Returns the live editor text (staleness guard).</param>
        /// <param name="parse">Parses <paramref name="text"/> (may run off the UI thread).</param>
        /// <param name="applyParsed">Applies the parse result on the UI / sync context.</param>
        protected void ParseListTextThenApply<TParsed>(
            string text,
            Func<string> getCurrentText,
            Func<string, TParsed> parse,
            Action<TParsed> applyParsed
        )
        {
            ArgumentNullException.ThrowIfNull(getCurrentText);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(applyParsed);

            if (text.Length < LargeListTextParseOffUiThreshold)
            {
                applyParsed(parse(text));
                return;
            }

            var context = SynchronizationContext.Current;
            _ = Task.Run(() =>
            {
                var parsed = parse(text);

                void Commit()
                {
                    if (IsLoading || !string.Equals(getCurrentText(), text, StringComparison.Ordinal))
                    {
                        return;
                    }

                    applyParsed(parsed);
                }

                if (context is null)
                {
                    Commit();
                    return;
                }

                context.Post(_ => Commit(), state: null);
            });
        }

        /// <summary>
        /// Runs a pending debounced list-text apply immediately (e.g. when the editor is replaced).
        /// </summary>
        internal void FlushPendingLiveListTextApply()
        {
            var pending = _pendingLiveListTextApply;
            _CancelLiveListTextApplyTimer();
            _pendingLiveListTextApply = null;
            pending?.Invoke();
        }

        private void _CancelLiveListTextApplyTimer()
        {
            if (_liveListTextApplyCts is null)
            {
                return;
            }

            _liveListTextApplyCts.Cancel();
            _liveListTextApplyCts.Dispose();
            _liveListTextApplyCts = null;
        }

        private async Task _RunDebouncedLiveListTextApplyAsync(
            Action apply,
            int delayMilliseconds,
            CancellationToken cancellationToken,
            SynchronizationContext? context
        )
        {
            try
            {
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            void Run()
            {
                if (!ReferenceEquals(_pendingLiveListTextApply, apply))
                {
                    return;
                }

                _pendingLiveListTextApply = null;
                apply();
            }

            if (context is null)
            {
                Run();
                return;
            }

            context.Post(_ => Run(), state: null);
        }
    }
}

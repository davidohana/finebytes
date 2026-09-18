namespace Mfr.Engine.Config
{
    /// <summary>
    /// Reads and mutates <see cref="OptionsConfig.SuppressedConfirmations"/> on <see cref="ConfigStore.Options"/> to decide
    /// whether a gated confirm should show.
    /// <para>
    /// <see cref="Suppress"/> / <see cref="ClearSuppressions"/> update in-memory prefs only; callers persist via
    /// <see cref="ConfigStore.Save"/> (or Options <c>Commit</c> then Save).
    /// </para>
    /// </summary>
    public static class ConfirmationPolicy
    {
        /// <summary>
        /// Returns whether the UI should confirm for <paramref name="kind"/>.
        /// </summary>
        /// <param name="kind">Suppressible confirmation kind.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="kind"/> is not in the suppress list (show dialog).
        /// </returns>
        public static bool ShouldConfirm(ConfirmationKind kind)
        {
            return !ConfigStore.Options.SuppressedConfirmations.Contains(kind);
        }

        /// <summary>
        /// Adds <paramref name="kind"/> to the in-memory suppress list when not already present.
        /// </summary>
        /// <param name="kind">Confirmation kind to stop showing.</param>
        public static void Suppress(ConfirmationKind kind)
        {
            var suppressed = ConfigStore.Options.SuppressedConfirmations;
            if (suppressed.Contains(kind))
            {
                return;
            }

            suppressed.Add(kind);
        }

        /// <summary>
        /// Clears the in-memory suppress list so all suppressible confirms show again.
        /// </summary>
        public static void ClearSuppressions()
        {
            ConfigStore.Options.SuppressedConfirmations.Clear();
        }
    }
}

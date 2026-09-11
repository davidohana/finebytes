namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Whether the Save Preset dialog updates the last-loaded preset or creates/overwrites by name.
    /// </summary>
    public enum SavePresetDialogMode
    {
        /// <summary>Update the last-loaded preset (same Id; name may change when free).</summary>
        Update,

        /// <summary>Save under a chosen name (new or overwrite).</summary>
        SaveAs,
    }
}

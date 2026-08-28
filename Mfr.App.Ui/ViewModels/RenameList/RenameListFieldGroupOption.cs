namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One property group in the Rename List field shuttle group dropdown.
    /// </summary>
    /// <param name="GroupId">MFR7 property group id.</param>
    /// <param name="DisplayName">User-visible group label.</param>
    public sealed record RenameListFieldGroupOption(string GroupId, string DisplayName);
}

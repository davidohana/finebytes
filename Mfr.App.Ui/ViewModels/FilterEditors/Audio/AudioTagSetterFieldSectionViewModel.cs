namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// One FieldsetGroup of Audio Tag Setter field rows.
    /// </summary>
    /// <param name="Header">Fieldset header text.</param>
    /// <param name="Rows">Rows shown in this group.</param>
    internal sealed record AudioTagSetterFieldSectionViewModel(
        string Header,
        IReadOnlyList<AudioTagSetterFieldRowViewModel> Rows
    );
}

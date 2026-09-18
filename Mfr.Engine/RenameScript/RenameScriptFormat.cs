namespace Mfr.Engine.RenameScript
{
    /// <summary>
    /// Output script dialect for <see cref="RenameList.RenameList.ExportRenameScript"/>.
    /// </summary>
    public enum RenameScriptFormat
    {
        /// <summary>
        /// Windows <c>.bat</c> / cmd.exe script (<c>ren</c>, <c>move</c>, <c>attrib</c>).
        /// </summary>
        Bat,

        /// <summary>
        /// Windows PowerShell <c>.ps1</c> script (<c>Rename-Item</c>, <c>Move-Item</c>, attribute bit ops).
        /// </summary>
        PowerShell,
    }
}

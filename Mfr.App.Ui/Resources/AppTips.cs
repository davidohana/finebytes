using Mfr.App.Ui.Input;

namespace Mfr.App.Ui.Resources
{
    /// <summary>
    /// Toolbar, menu, and control tooltip text.
    /// <para>
    /// Shortcut gestures live in <see cref="AppShortcuts"/>; keep gesture text in these tips
    /// aligned with that type and <c>docs/keyboard-shortcuts.md</c>.
    /// </para>
    /// </summary>
    public static class AppTips
    {
        /// <summary>Toolbar and menu tip for GO.</summary>
        public const string Go = "GO (Ctrl+G)";

        /// <summary>Toolbar tip for Undo Last.</summary>
        public const string UndoLast = "Undo last (Ctrl+Z)";

        /// <summary>Toolbar tip for the log window.</summary>
        public const string ShowLog = "Log (Ctrl+Shift+L)";

        /// <summary>Toolbar tip for Options.</summary>
        public const string ShowOptions = "Options (Ctrl+,)";

        /// <summary>Rename List Add Selected button and menu tip.</summary>
        public const string AddSelected = "Add selected (Ctrl+Shift+S)";

        /// <summary>Rename List Add All button and menu tip.</summary>
        public const string AddAll = "Add all (Ctrl+Shift+A)";

        /// <summary>Rename List Remove Selected button and menu tip.</summary>
        public const string RemoveSelected = "Remove selected (Ctrl+Shift+R)";

        /// <summary>Rename List Remove All But Selected menu tip.</summary>
        public const string RemoveAllButSelected = "Remove all but selected (Ctrl+Shift+B)";

        /// <summary>Rename List Clear button and menu tip.</summary>
        public const string ClearRenameList = "Clear Rename List (Ctrl+Shift+C)";

        /// <summary>Rename List Locate in File List menu tip.</summary>
        public const string LocateInFileList = "Locate in File List (F4)";

        /// <summary>Rename List Move Selected Up button and menu tip.</summary>
        public const string MoveSelectedUp = "Move selected up (Ctrl+Up)";

        /// <summary>Rename List Move Selected Down button and menu tip.</summary>
        public const string MoveSelectedDown = "Move selected down (Ctrl+Down)";

        /// <summary>Rename List sort editor flyout (Rename List grid context menu).</summary>
        public const string EditSortFields = "Edit sort fields (Rename List right-click menu)";

        /// <summary>File List refresh button tip.</summary>
        public const string Refresh = "Refresh (F5)";

        /// <summary>Address bar tip for focusing the typed path.</summary>
        public const string GoToAddress = "Go to folder (Ctrl+L)";

        /// <summary>Up button tip. Backspace is File List–focused, not a window hotkey.</summary>
        public const string GoUp = "Up (Backspace)";

        /// <summary>Typed-path box tip.</summary>
        public const string AddressEdit = "Enter to go, Esc to cancel";
    }
}

using Avalonia.Input;

namespace Mfr.App.Ui.Input
{
    /// <summary>
    /// Keyboard shortcuts shown in menus and tooltips, and bound on the main window.
    /// <para>
    /// Keep <c>docs/keyboard-shortcuts.md</c> in sync with these gestures.
    /// Window-level <see cref="KeyBinding"/>s use the members here, including File List
    /// view types (Ctrl+1 through Ctrl+6). Pane-local keys (Backspace, thumbnail zoom,
    /// address Enter/Esc) are handled in the File List view.
    /// </para>
    /// </summary>
    public static class AppShortcuts
    {
        /// <summary>Applies pending rename changes.</summary>
        public static KeyGesture Go { get; } = new(Key.G, KeyModifiers.Control);

        /// <summary>Undoes the last GO.</summary>
        public static KeyGesture UndoLast { get; } = new(Key.Z, KeyModifiers.Control);

        /// <summary>Opens the rename log. Ctrl+L is the address bar.</summary>
        public static KeyGesture ShowLog { get; } = new(Key.L, KeyModifiers.Control | KeyModifiers.Shift);

        /// <summary>Opens Options.</summary>
        public static KeyGesture ShowOptions { get; } = new(Key.OemComma, KeyModifiers.Control);

        /// <summary>Exits the application. Bound by the OS on Windows, not as a window key binding.</summary>
        public static KeyGesture Exit { get; } = new(Key.F4, KeyModifiers.Alt);

        /// <summary>Reloads the File List folder listing.</summary>
        public static KeyGesture Refresh { get; } = new(Key.F5);

        /// <summary>Focuses the File List address bar for typing a path.</summary>
        public static KeyGesture GoToAddress { get; } = new(Key.L, KeyModifiers.Control);

        /// <summary>Alternate address-bar focus, matching File Explorer.</summary>
        public static KeyGesture GoToAddressAlt { get; } = new(Key.D, KeyModifiers.Alt);

        /// <summary>Goes to the parent folder when the File List has focus.</summary>
        public static KeyGesture GoUp { get; } = new(Key.Back);

        /// <summary>Next larger Thumbnails size. Also Ctrl+Shift+= and numpad plus.</summary>
        public static KeyGesture ZoomIn { get; } = new(Key.OemPlus, KeyModifiers.Control);

        /// <summary>Next smaller Thumbnails size. Also numpad minus.</summary>
        public static KeyGesture ZoomOut { get; } = new(Key.OemMinus, KeyModifiers.Control);

        /// <summary>Restores the default Thumbnails size. Also numpad 0.</summary>
        public static KeyGesture ResetZoom { get; } = new(Key.D0, KeyModifiers.Control);

        /// <summary>Switches the File List to Large Icons.</summary>
        public static KeyGesture ViewLargeIcons { get; } = new(Key.D1, KeyModifiers.Control);

        /// <summary>Switches the File List to Small Icons.</summary>
        public static KeyGesture ViewSmallIcons { get; } = new(Key.D2, KeyModifiers.Control);

        /// <summary>Switches the File List to Report (details grid).</summary>
        public static KeyGesture ViewReport { get; } = new(Key.D3, KeyModifiers.Control);

        /// <summary>Switches the File List to List.</summary>
        public static KeyGesture ViewList { get; } = new(Key.D4, KeyModifiers.Control);

        /// <summary>Switches the File List to Tiles.</summary>
        public static KeyGesture ViewTiles { get; } = new(Key.D5, KeyModifiers.Control);

        /// <summary>Switches the File List to Thumbnails.</summary>
        public static KeyGesture ViewThumbnails { get; } = new(Key.D6, KeyModifiers.Control);

        /// <summary>Adds the File List selection to the Rename List.</summary>
        public static KeyGesture AddSelected { get; } = new(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

        /// <summary>Adds every File List item to the Rename List.</summary>
        public static KeyGesture AddAll { get; } = new(Key.A, KeyModifiers.Control | KeyModifiers.Shift);

        /// <summary>Removes the Rename List selection.</summary>
        public static KeyGesture RemoveSelected { get; } = new(Key.R, KeyModifiers.Control | KeyModifiers.Shift);

        /// <summary>Clears the Rename List.</summary>
        public static KeyGesture ClearRenameList { get; } = new(Key.C, KeyModifiers.Control | KeyModifiers.Shift);

        /// <summary>Toolbar and menu tip for GO.</summary>
        public const string GoTip = "GO (Ctrl+G)";

        /// <summary>Toolbar tip for Undo Last.</summary>
        public const string UndoLastTip = "Undo last (Ctrl+Z)";

        /// <summary>Toolbar tip for the log window.</summary>
        public const string ShowLogTip = "Log (Ctrl+Shift+L)";

        /// <summary>Toolbar tip for Options.</summary>
        public const string ShowOptionsTip = "Options (Ctrl+,)";

        /// <summary>File List refresh button tip.</summary>
        public const string RefreshTip = "Refresh (F5)";

        /// <summary>Address bar tip for focusing the typed path.</summary>
        public const string GoToAddressTip = "Go to folder (Ctrl+L)";

        /// <summary>Up button tip. Backspace is File List–focused, not a window hotkey.</summary>
        public const string GoUpTip = "Up (Backspace)";

        /// <summary>Typed-path box tip.</summary>
        public const string AddressEditTip = "Enter to go, Esc to cancel";
    }
}

using Avalonia.Input;
using Mfr.App.Ui.Input;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests the documented keyboard shortcut gestures.
    /// </summary>
    public sealed class AppShortcutsTests
    {
        /// <summary>
        /// Verifies global gestures match docs/keyboard-shortcuts.md.
        /// </summary>
        [Fact]
        public void Global_Gestures_Match_Documented_Keys()
        {
            Assert.Equal(new KeyGesture(Key.G, KeyModifiers.Control), AppShortcuts.Go);
            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control), AppShortcuts.UndoLast);
            Assert.Equal(new KeyGesture(Key.L, KeyModifiers.Control | KeyModifiers.Shift), AppShortcuts.ShowLog);
            Assert.Equal(new KeyGesture(Key.OemComma, KeyModifiers.Control), AppShortcuts.ShowOptions);
            Assert.Equal(new KeyGesture(Key.F4, KeyModifiers.Alt), AppShortcuts.Exit);
            Assert.Equal(new KeyGesture(Key.F5), AppShortcuts.Refresh);
            Assert.Equal(new KeyGesture(Key.L, KeyModifiers.Control), AppShortcuts.GoToAddress);
            Assert.Equal(new KeyGesture(Key.D, KeyModifiers.Alt), AppShortcuts.GoToAddressAlt);
        }

        /// <summary>
        /// Verifies File List and reserved Rename List gestures match the cheatsheet.
        /// </summary>
        [Fact]
        public void File_And_Rename_List_Gestures_Match_Documented_Keys()
        {
            Assert.Equal(new KeyGesture(Key.Back), AppShortcuts.GoUp);
            Assert.Equal(new KeyGesture(Key.OemPlus, KeyModifiers.Control), AppShortcuts.ZoomIn);
            Assert.Equal(new KeyGesture(Key.OemMinus, KeyModifiers.Control), AppShortcuts.ZoomOut);
            Assert.Equal(new KeyGesture(Key.D0, KeyModifiers.Control), AppShortcuts.ResetZoom);
            Assert.Equal(new KeyGesture(Key.D1, KeyModifiers.Control), AppShortcuts.ViewLargeIcons);
            Assert.Equal(new KeyGesture(Key.D2, KeyModifiers.Control), AppShortcuts.ViewSmallIcons);
            Assert.Equal(new KeyGesture(Key.D3, KeyModifiers.Control), AppShortcuts.ViewReport);
            Assert.Equal(new KeyGesture(Key.D4, KeyModifiers.Control), AppShortcuts.ViewList);
            Assert.Equal(new KeyGesture(Key.D5, KeyModifiers.Control), AppShortcuts.ViewTiles);
            Assert.Equal(new KeyGesture(Key.D6, KeyModifiers.Control), AppShortcuts.ViewThumbnails);
            Assert.Equal(new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift), AppShortcuts.AddSelected);
            Assert.Equal(new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Shift), AppShortcuts.AddAll);
            Assert.Equal(new KeyGesture(Key.R, KeyModifiers.Control | KeyModifiers.Shift), AppShortcuts.RemoveSelected);
            Assert.Equal(
                new KeyGesture(Key.B, KeyModifiers.Control | KeyModifiers.Shift),
                AppShortcuts.RemoveAllButSelected
            );
            Assert.Equal(
                new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Shift),
                AppShortcuts.ClearRenameList
            );
            Assert.Equal(new KeyGesture(Key.Delete), AppShortcuts.RemoveSelectedDelete);
            Assert.Equal(new KeyGesture(Key.F4), AppShortcuts.LocateInFileList);
            Assert.Equal(new KeyGesture(Key.Up, KeyModifiers.Control), AppShortcuts.MoveSelectedUp);
            Assert.Equal(new KeyGesture(Key.Down, KeyModifiers.Control), AppShortcuts.MoveSelectedDown);
        }
    }
}

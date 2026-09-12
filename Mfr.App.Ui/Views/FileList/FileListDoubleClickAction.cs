using System.Windows.Input;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// Chooses Open Selected vs Add Selected for File List double-tap.
    /// </summary>
    internal static class FileListDoubleClickAction
    {
        /// <summary>
        /// Runs Add Selected when <paramref name="addsToRenameList"/> is true and that command can execute;
        /// otherwise Open Selected.
        /// </summary>
        /// <param name="addsToRenameList">Config flag <c>ui.doubleClickAddsToRenameList</c>.</param>
        /// <param name="addSelectedCommand">Rename List Add Selected (toolbar) command.</param>
        /// <param name="openSelectedCommand">File List Open Selected command.</param>
        public static void Execute(bool addsToRenameList, ICommand? addSelectedCommand, ICommand? openSelectedCommand)
        {
            if (addsToRenameList && _TryExecute(addSelectedCommand))
            {
                return;
            }

            _ = _TryExecute(openSelectedCommand);
        }

        /// <summary>
        /// Runs <paramref name="command"/> when it is non-null and <see cref="ICommand.CanExecute"/> is true.
        /// </summary>
        /// <param name="command">Command to run, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the command ran; otherwise <see langword="false"/>.</returns>
        private static bool _TryExecute(ICommand? command)
        {
            if (command is null || !command.CanExecute(null))
            {
                return false;
            }

            command.Execute(null);
            return true;
        }
    }
}

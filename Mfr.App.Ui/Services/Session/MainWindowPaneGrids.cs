using Avalonia.Controls;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Named pane grids used for session splitter ratios.
    /// </summary>
    /// <remarks>
    /// Built by the main window view so session services never import Views.
    /// </remarks>
    internal sealed class MainWindowPaneGrids
    {
        /// <summary>File List | filters column grid.</summary>
        public required Grid TopPanes { get; init; }

        /// <summary>Available | Applied filters column grid.</summary>
        public required Grid FilterLists { get; init; }

        /// <summary>Filter lists | Filter Configuration row grid.</summary>
        public required Grid FilterPanes { get; init; }

        /// <summary>Top panes | Rename List row grid.</summary>
        public required Grid MainPanes { get; init; }
    }
}

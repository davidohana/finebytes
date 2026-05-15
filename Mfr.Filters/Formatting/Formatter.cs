using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Compiled formatter delegate that resolves template output for one rename item.
    /// </summary>
    /// <param name="item">The row being renamed.</param>
    /// <returns>Expanded text for this item.</returns>
    internal delegate string Formatter(RenameItem item);
}

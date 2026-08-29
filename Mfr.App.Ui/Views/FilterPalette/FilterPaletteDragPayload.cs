using Avalonia.Input;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.FilterPalette
{
    /// <summary>
    /// Drag-and-drop payload for catalog rows dragged from Available Filters.
    /// </summary>
    /// <param name="CatalogTypes">Ordered <see cref="Filters.FilterCatalogEntry.Type"/> values.</param>
    internal sealed record FilterPaletteDragPayload(IReadOnlyList<string> CatalogTypes)
    {
        /// <summary>
        /// Avalonia data format for Available Filters drag payloads.
        /// </summary>
        public static readonly DataFormat<string> Format = JsonDragPayload.CreateFormat("Mfr.FilterPaletteDragPayload");

        /// <summary>
        /// Serializes the payload for drag transport.
        /// </summary>
        /// <returns>JSON payload string.</returns>
        public string Serialize()
        {
            return JsonDragPayload.Serialize(this);
        }

        /// <summary>
        /// Reads an Available Filters catalog payload from drag data.
        /// </summary>
        /// <param name="dataTransfer">Drag data, or <see langword="null"/>.</param>
        /// <returns>Payload when it contains at least one catalog type; otherwise <see langword="null"/>.</returns>
        public static FilterPaletteDragPayload? TryRead(IDataTransfer? dataTransfer)
        {
            var payload = JsonDragPayload.TryRead<FilterPaletteDragPayload>(dataTransfer, Format);
            return payload?.CatalogTypes is { Count: > 0 } ? payload : null;
        }
    }
}

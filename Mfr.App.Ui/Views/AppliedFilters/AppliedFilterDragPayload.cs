using Avalonia.Input;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Drag-and-drop payload for reordering Applied Filters rows.
    /// </summary>
    /// <param name="SourceIndices">Selected row indices when the drag started.</param>
    internal sealed record AppliedFilterDragPayload(IReadOnlyList<int> SourceIndices)
    {
        /// <summary>
        /// Avalonia data format for Applied Filters drag payloads.
        /// </summary>
        public static readonly DataFormat<string> Format = JsonDragPayload.CreateFormat("Mfr.AppliedFilterDragPayload");

        /// <summary>
        /// Serializes the payload for drag transport.
        /// </summary>
        /// <returns>JSON payload string.</returns>
        public string Serialize()
        {
            return JsonDragPayload.Serialize(this);
        }

        /// <summary>
        /// Builds a transfer containing only this payload.
        /// </summary>
        /// <returns>Transfer ready for <see cref="DragDrop.DoDragDropAsync"/>.</returns>
        public DataTransfer CreateTransfer()
        {
            return JsonDragPayload.CreateTransfer(Format, this);
        }

        /// <summary>
        /// Appends this payload to an existing <paramref name="dataTransfer"/>.
        /// </summary>
        /// <param name="dataTransfer">Transfer to extend.</param>
        public void AddTo(DataTransfer dataTransfer)
        {
            JsonDragPayload.Add(dataTransfer, Format, this);
        }

        /// <summary>
        /// Reads an Applied Filters reorder payload from drag data.
        /// </summary>
        /// <param name="dataTransfer">Drag data, or <see langword="null"/>.</param>
        /// <returns>Payload when it contains at least one source index; otherwise <see langword="null"/>.</returns>
        public static AppliedFilterDragPayload? TryRead(IDataTransfer? dataTransfer)
        {
            var payload = JsonDragPayload.TryRead<AppliedFilterDragPayload>(dataTransfer, Format);
            return payload?.SourceIndices is { Count: > 0 } ? payload : null;
        }
    }
}

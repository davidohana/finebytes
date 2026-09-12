using Avalonia.Input;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Drag-and-drop payload for reordering list rows by selected indices.
    /// </summary>
    /// <param name="SourceIndices">Selected row indices when the drag started.</param>
    /// <remarks>
    /// Applied Filters and Preset Manager share this shape but use distinct
    /// <see cref="DataFormat{T}"/> values so cross-pane drops do not match.
    /// </remarks>
    internal sealed record IndicesDragPayload(IReadOnlyList<int> SourceIndices)
    {
        /// <summary>
        /// Avalonia data format for Applied Filters reorder (and Palette remove-drop).
        /// </summary>
        public static readonly DataFormat<string> AppliedFiltersFormat = JsonDragPayload.CreateFormat(
            "Mfr.AppliedFilterDragPayload"
        );

        /// <summary>
        /// Avalonia data format for Preset Manager reorder.
        /// </summary>
        public static readonly DataFormat<string> PresetsFormat = JsonDragPayload.CreateFormat("Mfr.PresetDragPayload");

        /// <summary>
        /// Builds a transfer containing only this payload under <paramref name="format"/>.
        /// </summary>
        /// <param name="format">Pane-specific data format.</param>
        /// <returns>Transfer ready for <see cref="DragDrop.DoDragDropAsync"/>.</returns>
        public DataTransfer CreateTransfer(DataFormat<string> format)
        {
            return JsonDragPayload.CreateTransfer(format, this);
        }

        /// <summary>
        /// Appends this payload to an existing <paramref name="dataTransfer"/> under <paramref name="format"/>.
        /// </summary>
        /// <param name="dataTransfer">Transfer to extend.</param>
        /// <param name="format">Pane-specific data format.</param>
        public void AddTo(DataTransfer dataTransfer, DataFormat<string> format)
        {
            JsonDragPayload.Add(dataTransfer, format, this);
        }

        /// <summary>
        /// Reads an indices reorder payload from drag data for <paramref name="format"/>.
        /// </summary>
        /// <param name="dataTransfer">Drag data, or <see langword="null"/>.</param>
        /// <param name="format">Pane-specific data format.</param>
        /// <returns>Payload when it contains at least one source index; otherwise <see langword="null"/>.</returns>
        public static IndicesDragPayload? TryRead(IDataTransfer? dataTransfer, DataFormat<string> format)
        {
            var payload = JsonDragPayload.TryRead<IndicesDragPayload>(dataTransfer, format);
            return payload?.SourceIndices is { Count: > 0 } ? payload : null;
        }
    }
}

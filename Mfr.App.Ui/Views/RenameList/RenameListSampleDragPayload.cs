using Avalonia.Input;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Drag payload so the Visual Trim Helper can resolve a Rename List row by original path.
    /// </summary>
    /// <param name="FullPath">Original full path of the dragged entry.</param>
    internal sealed record RenameListSampleDragPayload(string FullPath)
    {
        /// <summary>
        /// Gets the Avalonia data format for this payload.
        /// </summary>
        public static readonly DataFormat<string> Format = JsonDragPayload.CreateFormat(
            "Mfr.RenameListSampleDragPayload"
        );

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
        /// Appends this payload to an existing <paramref name="dataTransfer"/> (e.g. with reorder marker).
        /// </summary>
        /// <param name="dataTransfer">Transfer to extend.</param>
        public void AddTo(DataTransfer dataTransfer)
        {
            JsonDragPayload.Add(dataTransfer, Format, this);
        }

        /// <summary>
        /// Reads a sample payload from <paramref name="dataTransfer"/> when present.
        /// </summary>
        /// <param name="dataTransfer">Drag data.</param>
        /// <returns>Payload when present; otherwise <see langword="null"/>.</returns>
        public static RenameListSampleDragPayload? TryRead(IDataTransfer? dataTransfer)
        {
            var payload = JsonDragPayload.TryRead<RenameListSampleDragPayload>(dataTransfer, Format);
            return string.IsNullOrEmpty(payload?.FullPath) ? null : payload;
        }
    }
}

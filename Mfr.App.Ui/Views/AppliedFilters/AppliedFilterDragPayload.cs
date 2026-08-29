using System.Text.Json;
using Avalonia.Input;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Drag-and-drop payload for reordering Applied Filters rows.
    /// </summary>
    /// <param name="SourceIndices">Selected row indices when the drag started.</param>
    internal sealed record AppliedFilterDragPayload(IReadOnlyList<int> SourceIndices)
    {
        private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = false };

        /// <summary>
        /// Avalonia data format for Applied Filters drag payloads.
        /// </summary>
        public static readonly DataFormat<string> Format = DataFormat.CreateStringApplicationFormat(
            "Mfr.AppliedFilterDragPayload"
        );

        /// <summary>
        /// Serializes the payload for drag transport.
        /// </summary>
        /// <returns>JSON payload string.</returns>
        public string Serialize()
        {
            return JsonSerializer.Serialize(this, _JsonOptions);
        }

        /// <summary>
        /// Deserializes a drag payload from transport JSON.
        /// </summary>
        /// <param name="json">Serialized payload.</param>
        /// <returns>Payload when valid; otherwise <see langword="null"/>.</returns>
        public static AppliedFilterDragPayload? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<AppliedFilterDragPayload>(json, _JsonOptions);
        }
    }
}

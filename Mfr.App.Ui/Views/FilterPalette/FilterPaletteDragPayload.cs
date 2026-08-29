using System.Text.Json;
using Avalonia.Input;

namespace Mfr.App.Ui.Views.FilterPalette
{
    /// <summary>
    /// Drag-and-drop payload for catalog rows dragged from Available Filters.
    /// </summary>
    /// <param name="CatalogTypes">Ordered <see cref="Filters.FilterCatalogEntry.Type"/> values.</param>
    internal sealed record FilterPaletteDragPayload(IReadOnlyList<string> CatalogTypes)
    {
        private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = false };

        /// <summary>
        /// Avalonia data format for Available Filters drag payloads.
        /// </summary>
        public static readonly DataFormat<string> Format = DataFormat.CreateStringApplicationFormat(
            "Mfr.FilterPaletteDragPayload"
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
        public static FilterPaletteDragPayload? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<FilterPaletteDragPayload>(json, _JsonOptions);
        }
    }
}

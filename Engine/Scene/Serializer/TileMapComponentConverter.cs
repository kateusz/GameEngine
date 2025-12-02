using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Scene.Components;
using Engine.Tiles;

namespace Engine.Scene.Serializer;

/// <summary>
/// Custom JSON converter for TileMapComponent to handle 2D array serialization
/// </summary>
internal sealed class TileMapComponentConverter : JsonConverter<TileMapComponent>
{
    public override TileMapComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var component = new TileMapComponent();
        component.SetWidth(root.GetProperty("Width").GetInt32());
        component.SetHeight(root.GetProperty("Height").GetInt32());
        component.SetTileSize(JsonSerializer.Deserialize<System.Numerics.Vector2>(
            root.GetProperty("TileSize").GetRawText(), options));
        component.SetTileSetPath(root.GetProperty("TileSetPath").GetString() ??
                                 throw new SerializationException("Missing TileSetPath"));
        component.SetTileSetColumns(root.GetProperty("TileSetColumns").GetInt32());
        component.SetTileSetRows(root.GetProperty("TileSetRows").GetInt32());
        component.SetActiveLayerIndex(root.GetProperty("ActiveLayerIndex").GetInt32());
        
        if (!root.TryGetProperty("Layers", out var layersElement))
        {
            // No layers in JSON; keep the default layer created by the component constructor.
            return component;
        }

        component.Layers.Clear();

        foreach (var layerElement in layersElement.EnumerateArray())
        {
            var layer = new TileMapLayer(component.Width, component.Height)
            {
                Name = layerElement.GetProperty("Name").GetString() ?? "Layer",
                Visible = layerElement.GetProperty("Visible").GetBoolean(),
                ZIndex = layerElement.GetProperty("ZIndex").GetInt32()
            };

            // Deserialize 2D tile array
            if (layerElement.TryGetProperty("Tiles", out var tilesElement))
            {
                var tilesArray = tilesElement.EnumerateArray().ToArray();
                for (var y = 0; y < component.Height && y < tilesArray.Length; y++)
                {
                    var rowArray = tilesArray[y].EnumerateArray().ToArray();
                    for (var x = 0; x < component.Width && x < rowArray.Length; x++)
                    {
                        layer.Tiles[x, y] = rowArray[x].GetInt32();
                    }
                }
            }

            component.Layers.Add(layer);
        }

        return component;
    }

    public override void Write(Utf8JsonWriter writer, TileMapComponent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("Width", value.Width);
        writer.WriteNumber("Height", value.Height);
        writer.WritePropertyName("TileSize");
        JsonSerializer.Serialize(writer, value.TileSize, options);
        writer.WriteString("TileSetPath", value.TileSetPath);
        writer.WriteNumber("TileSetColumns", value.TileSetColumns);
        writer.WriteNumber("TileSetRows", value.TileSetRows);
        writer.WriteNumber("ActiveLayerIndex", value.ActiveLayerIndex);

        // Serialize layers
        writer.WritePropertyName("Layers");
        writer.WriteStartArray();

        foreach (var layer in value.Layers)
        {
            writer.WriteStartObject();

            writer.WriteString("Name", layer.Name);
            writer.WriteBoolean("Visible", layer.Visible);
            writer.WriteNumber("ZIndex", layer.ZIndex);

            // Serialize 2D tile array as array of arrays
            writer.WritePropertyName("Tiles");
            writer.WriteStartArray();

            for (var y = 0; y < value.Height; y++)
            {
                writer.WriteStartArray();
                for (var x = 0; x < value.Width; x++)
                {
                    writer.WriteNumberValue(layer.Tiles[x, y]);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndArray(); // End Tiles array
            writer.WriteEndObject(); // End layer object
        }

        writer.WriteEndArray(); // End Layers array
        writer.WriteEndObject(); // End component object
    }
}
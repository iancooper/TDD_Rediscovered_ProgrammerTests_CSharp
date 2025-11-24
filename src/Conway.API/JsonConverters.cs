// Author: Ian Cooper
// Date: 24 November 2025
// Notes: JSON converters for Conway Game of Life API

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conway.API;

/// <summary>
/// Custom JSON converter for multi-dimensional arrays
/// </summary>
public class MultiDimensionalArrayConverter : JsonConverter<char[,]>
{
    public override char[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var rows = new List<List<char>>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var row = new List<char>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    row.Add(reader.GetString()?[0] ?? '.');
                }
                rows.Add(row);
            }
        }

        if (rows.Count == 0)
            return new char[0, 0];

        var result = new char[rows.Count, rows[0].Count];
        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows[i].Count; j++)
            {
                result[i, j] = rows[i][j];
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, char[,] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        for (int i = 0; i < value.GetLength(0); i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < value.GetLength(1); j++)
            {
                writer.WriteStringValue(value[i, j].ToString());
            }
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}

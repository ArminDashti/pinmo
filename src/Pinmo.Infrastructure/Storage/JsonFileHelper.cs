using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pinmo.Infrastructure.Storage;

internal static class JsonFileHelper
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<T> ReadAsync<T>(string filePath, T defaultValue, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return defaultValue;
        }

        await using var stream = File.OpenRead(filePath);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
        return value ?? defaultValue;
    }

    public static async Task WriteAsync<T>(string filePath, T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
        }

        if (File.Exists(filePath))
        {
            File.Replace(tempPath, filePath, null);
        }
        else
        {
            File.Move(tempPath, filePath);
        }
    }
}

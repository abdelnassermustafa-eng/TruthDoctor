using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TruthDoctor.Graph;

/// <summary>
/// Persists the complete saved-view catalog in one provider-neutral JSON
/// document. Writes use a temporary file in the destination directory and
/// are published with one same-volume move.
/// </summary>
public sealed class TopologySavedViewFileStore
{
    public const int CurrentStoreSchemaVersion = 1;

    private readonly string _filePath;

    private readonly TopologySavedViewSerializer
        _viewSerializer =
            new();

    private readonly JsonSerializerOptions
        _documentOptions =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                WriteIndented =
                    true
            };

    public TopologySavedViewFileStore(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            filePath);

        _filePath =
            Path.GetFullPath(
                filePath.Trim());
    }

    public string FilePath =>
        _filePath;

    public void Save(
        IEnumerable<TopologySavedView> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        var validatedCatalog =
            new TopologySavedViewCatalog();

        validatedCatalog.ReplaceAll(
            views);

        var document =
            new StoreDocument
            {
                SchemaVersion =
                    CurrentStoreSchemaVersion,

                Views =
                    validatedCatalog.All
                        .Select(ToJsonElement)
                        .ToArray()
            };

        var json =
            JsonSerializer.Serialize(
                document,
                _documentOptions);

        var directory =
            Path.GetDirectoryName(
                _filePath)
            ?? throw new InvalidOperationException(
                "The saved-view file has no parent directory.");

        Directory.CreateDirectory(
            directory);

        var temporaryPath =
            Path.Combine(
                directory,
                $".{Path.GetFileName(_filePath)}." +
                $"{Guid.NewGuid():N}.tmp");

        try
        {
            WriteDurably(
                temporaryPath,
                json);

            File.Move(
                temporaryPath,
                _filePath,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(
                    temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }
        }
    }

    public IReadOnlyList<TopologySavedView> Load()
    {
        if (!File.Exists(
                _filePath))
        {
            return Array.Empty<TopologySavedView>();
        }

        var json =
            File.ReadAllText(
                _filePath,
                Encoding.UTF8);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        EnsureValidDocumentShape(
            root);

        var schemaVersion =
            root.GetProperty(
                    "schemaVersion")
                .GetInt32();

        if (schemaVersion !=
            CurrentStoreSchemaVersion)
        {
            throw new JsonException(
                $"Unsupported saved-view store schema " +
                $"version '{schemaVersion}'.");
        }

        var views =
            root.GetProperty(
                    "views")
                .EnumerateArray()
                .Select(element =>
                    _viewSerializer.Deserialize(
                        element.GetRawText()))
                .ToArray();

        var validatedCatalog =
            new TopologySavedViewCatalog();

        validatedCatalog.ReplaceAll(
            views);

        return validatedCatalog.All;
    }

    public TopologySavedViewStoreLoadResult TryLoad()
    {
        if (!File.Exists(
                _filePath))
        {
            return TopologySavedViewStoreLoadResult
                .Success(
                    Array.Empty<TopologySavedView>(),
                    fileWasMissing: true);
        }

        try
        {
            return TopologySavedViewStoreLoadResult
                .Success(
                    Load());
        }
        catch (JsonException exception)
        {
            return FailureFor(
                exception);
        }
        catch (IOException exception)
        {
            return FailureFor(
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return FailureFor(
                exception);
        }
        catch (ArgumentException exception)
        {
            return FailureFor(
                exception);
        }
        catch (InvalidOperationException exception)
        {
            return FailureFor(
                exception);
        }
    }

    private JsonElement ToJsonElement(
        TopologySavedView view)
    {
        using var document =
            JsonDocument.Parse(
                _viewSerializer.Serialize(
                    view));

        return document.RootElement.Clone();
    }

    private static void EnsureValidDocumentShape(
        JsonElement root)
    {
        if (root.ValueKind !=
            JsonValueKind.Object)
        {
            throw new JsonException(
                "Saved-view store must be a JSON object.");
        }

        foreach (var property in
                 root.EnumerateObject())
        {
            if (property.Name is not
                ("schemaVersion" or "views"))
            {
                throw new JsonException(
                    $"Unknown saved-view store property " +
                    $"'{property.Name}'.");
            }
        }

        if (!root.TryGetProperty(
                "schemaVersion",
                out var schemaVersion) ||
            schemaVersion.ValueKind !=
                JsonValueKind.Number ||
            !schemaVersion.TryGetInt32(
                out _))
        {
            throw new JsonException(
                "Saved-view store schemaVersion is required.");
        }

        if (!root.TryGetProperty(
                "views",
                out var views) ||
            views.ValueKind !=
                JsonValueKind.Array)
        {
            throw new JsonException(
                "Saved-view store views array is required.");
        }
    }

    private static void WriteDurably(
        string path,
        string content)
    {
        using var stream =
            new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough);

        using var writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false),
                bufferSize: 4096,
                leaveOpen: true);

        writer.Write(
            content);

        writer.Flush();
        stream.Flush(
            flushToDisk: true);
    }

    private static
        TopologySavedViewStoreLoadResult FailureFor(
            Exception exception)
    {
        return TopologySavedViewStoreLoadResult
            .Failure(
                $"Saved views could not be loaded: " +
                $"{exception.Message}");
    }

    private sealed class StoreDocument
    {
        public int SchemaVersion { get; init; }

        public JsonElement[] Views { get; init; } =
            [];
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TruthDoctor.Graph;

public sealed class TopologySavedViewSerializer
{
    private readonly TopologySavedViewValidator
        _validator =
            new();

    private readonly JsonSerializerOptions
        _options =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                PropertyNameCaseInsensitive =
                    true,

                WriteIndented =
                    true,

                UnmappedMemberHandling =
                    JsonUnmappedMemberHandling.Disallow,

                Converters =
                {
                    new JsonStringEnumConverter(
                        JsonNamingPolicy.CamelCase,
                        allowIntegerValues: false)
                }
            };

    public string Serialize(
        TopologySavedView view)
    {
        _validator.EnsureValid(view);

        return JsonSerializer.Serialize(
            view,
            _options);
    }

    public TopologySavedView Deserialize(
        string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            json);

        var view =
            JsonSerializer.Deserialize<
                TopologySavedView>(
                    json,
                    _options)
            ?? throw new JsonException(
                "Saved topology view is empty.");

        _validator.EnsureValid(view);

        return view;
    }
}

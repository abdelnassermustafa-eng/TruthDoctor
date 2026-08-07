using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TruthDoctor.Models.Platform;

public sealed class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public sealed class PlatformState
{
    [JsonPropertyName("context")]
    public PlatformContext Context { get; set; } = new();

    [JsonPropertyName("domains")]
    public List<InfrastructureDomain> Domains { get; set; } = [];

    [JsonPropertyName("resources")]
    public List<InfrastructureResource> Resources { get; set; } = [];

    [JsonPropertyName("relationships")]
    public List<InfrastructureRelationship> Relationships { get; set; } = [];

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonPropertyName("discoveredAt")]
    public DateTimeOffset DiscoveredAt { get; set; }

    [JsonPropertyName("totalResourceCount")]
    public int TotalResourceCount { get; set; }
}

public sealed class PlatformContext
{
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = "";

    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = "";

    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = "";

    [JsonPropertyName("identityId")]
    public string IdentityId { get; set; } = "";

    [JsonPropertyName("identityArn")]
    public string IdentityArn { get; set; } = "";

    [JsonPropertyName("defaultLocation")]
    public string DefaultLocation { get; set; } = "";

    [JsonPropertyName("locations")]
    public List<string> Locations { get; set; } = [];
}

public sealed class InfrastructureDomain
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("iconKey")]
    public string IconKey { get; set; } = "resource";

    [JsonPropertyName("accentKey")]
    public string AccentKey { get; set; } = "default";

    [JsonPropertyName("resourceCount")]
    public int ResourceCount { get; set; }

    [JsonPropertyName("resourceTypes")]
    public List<string> ResourceTypes { get; set; } = [];
}

public sealed class InfrastructureResource
{
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = "";

    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";

    [JsonPropertyName("domainId")]
    public string DomainId { get; set; } = "";

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "";

    [JsonPropertyName("resourceId")]
    public string ResourceId { get; set; } = "";

    [JsonPropertyName("nativeId")]
    public string NativeId { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("availabilityZone")]
    public string AvailabilityZone { get; set; } = "";

    [JsonPropertyName("arn")]
    public string Arn { get; set; } = "";

    [JsonPropertyName("iconKey")]
    public string IconKey { get; set; } = "resource";

    [JsonPropertyName("accentKey")]
    public string AccentKey { get; set; } = "default";

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = [];

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];

    [JsonPropertyName("relationships")]
    public List<InfrastructureRelationship> Relationships { get; set; } = [];

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    [JsonPropertyName("discoveredAt")]
    public DateTimeOffset DiscoveredAt { get; set; }
}

public sealed class InfrastructureRelationship
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("sourceResourceId")]
    public string SourceResourceId { get; set; } = "";

    [JsonPropertyName("targetResourceId")]
    public string TargetResourceId { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

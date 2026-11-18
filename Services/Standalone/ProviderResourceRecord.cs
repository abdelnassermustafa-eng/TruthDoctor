using System.Collections.Generic;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Immutable provider-neutral resource metadata discovered by an adapter.
/// </summary>
public sealed record ProviderResourceRecord(
    string ProviderId,
    string ResourceId,
    string ResourceType,
    string DisplayName,
    string State = "",
    string AvailabilityZone = "",
    string Arn = "",
    IReadOnlyDictionary<string, string>? Properties = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    ResourceLifecycleEvidenceSet? LifecycleEvidence = null);

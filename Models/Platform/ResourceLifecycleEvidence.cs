using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Models.Platform;

public enum ResourceLifecycleEventKind
{
    Created,
    Started,
    Stopped,
    Restarted,
    ConfigurationChanged,
    StateChanged,
    MaintenancePerformed,
    Deleted
}

public enum ResourceLifecycleEvidenceQuality
{
    Unverified,
    ProviderReported,
    Corroborated
}

/// <summary>
/// Immutable event claim with provider-neutral provenance.
/// Structural validation alone does not authenticate a claim.
/// </summary>
public sealed class ResourceLifecycleEventEvidence
{
    public ResourceLifecycleEventEvidence(
        ResourceLifecycleEventKind kind,
        DateTimeOffset occurredAt,
        ResourceLifecycleEvidenceQuality quality,
        string source,
        string? reference = null,
        string? actor = null)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        if (!Enum.IsDefined(quality))
            throw new ArgumentOutOfRangeException(nameof(quality));

        if (occurredAt == default)
            throw new ArgumentException(
                "A real event timestamp is required.",
                nameof(occurredAt));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "Evidence source must be provided.",
                nameof(source));

        Kind = kind;
        OccurredAt = occurredAt;
        Quality = quality;
        Source = source.Trim();
        Reference = reference?.Trim() ?? "";
        Actor = actor?.Trim() ?? "";
    }

    public ResourceLifecycleEventKind Kind { get; }
    public DateTimeOffset OccurredAt { get; }
    public ResourceLifecycleEvidenceQuality Quality { get; }
    public string Source { get; }
    public string Reference { get; }
    public string Actor { get; }

    // Adapters remain responsible for authenticating evidence
    // before assigning ProviderReported or Corroborated.
    public bool IsVerified =>
        Quality is ResourceLifecycleEvidenceQuality.ProviderReported
            or ResourceLifecycleEvidenceQuality.Corroborated;
}

/// <summary>
/// Original creation is distinct from subsequent activity.
/// Unknown data must remain explicitly unknown.
/// </summary>
public sealed class ResourceLifecycleEvidenceSet
{
    public ResourceLifecycleEvidenceSet(
        ResourceLifecycleEventEvidence? creation = null,
        IEnumerable<ResourceLifecycleEventEvidence>? activities = null)
    {
        if (creation is not null &&
            creation.Kind != ResourceLifecycleEventKind.Created)
        {
            throw new ArgumentException(
                "Creation must use the Created event kind.",
                nameof(creation));
        }

        var entries = activities?.ToArray() ??
            Array.Empty<ResourceLifecycleEventEvidence>();

        if (entries.Any(x => x is null))
        {
            throw new ArgumentException(
                "Activity collection cannot contain null.",
                nameof(activities));
        }

        if (entries.Any(x =>
            x.Kind == ResourceLifecycleEventKind.Created))
        {
            throw new ArgumentException(
                "Creation belongs in creation evidence.",
                nameof(activities));
        }

        Creation = creation;
        Activities = Array.AsReadOnly(entries);
    }

    public ResourceLifecycleEventEvidence? Creation { get; }

    public IReadOnlyList<ResourceLifecycleEventEvidence> Activities
    { get; }

    public ResourceLifecycleEventEvidence? VerifiedCreation =>
        Creation?.IsVerified == true ? Creation : null;

    public ResourceLifecycleEventEvidence? LastVerifiedActivity =>
        Activities
            .Where(x => x.IsVerified)
            .OrderByDescending(x => x.OccurredAt)
            .FirstOrDefault();
}

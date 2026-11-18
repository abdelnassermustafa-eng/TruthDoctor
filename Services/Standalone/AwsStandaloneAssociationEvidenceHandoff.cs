using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Bridges verified AWS association evidence into the
/// provider-neutral association consumer.
///
/// This component is read-only. It does not modify
/// discovery resources, graph edges, or topology.
/// </summary>
public static class AwsStandaloneAssociationEvidenceHandoff
{
    public static IReadOnlyList<InfrastructureAssociationView> Build(
        IReadOnlyList<InfrastructureResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        var awsEvidence =
            AwsStandaloneAssociationEvidenceExtractor
                .Extract(resources);

        var neutralEvidence = awsEvidence
            .Select(item =>
                new InfrastructureAssociationEvidence(
                    item.SourceResourceId,
                    item.TargetResourceId,
                    item.AssociationType,
                    item.PropertyName))
            .ToArray();

        return InfrastructureAssociationEvidenceConsumer
            .Resolve(resources, neutralEvidence);
    }
}

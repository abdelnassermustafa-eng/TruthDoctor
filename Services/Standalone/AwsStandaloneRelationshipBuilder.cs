using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Resolves explicitly discovered AWS resource references.
/// No missing targets or ambiguous matches are fabricated.
/// R8B adds typed AWS reference rules with strict scope isolation.
/// </summary>
public static class AwsStandaloneRelationshipBuilder
{
    public static List<InfrastructureRelationship> Build(
        IReadOnlyList<InfrastructureResource> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        var output = new List<InfrastructureRelationship>();
        var identities = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var resource in resources)
        {
            if (!StringComparer.Ordinal.Equals(
                resource.ProviderId, "aws"))
            {
                continue;
            }

            switch (resource.ResourceType)
            {
                case "subnet":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    break;

                case "compute-instance":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    AddReference(resource, "SubnetId", "subnet",
                        "hosted-on", resources, output, identities);
                    AddIndexed(resource,
                        @"^NetworkInterfaces\[\d+\]\.NetworkInterfaceId$",
                        "network-interface", "uses",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^SecurityGroups\[\d+\]\.GroupId$",
                        "security-group", "secured-by",
                        resources, output, identities);
                    break;

                case "network-interface":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    AddReference(resource, "SubnetId", "subnet",
                        "hosted-on", resources, output, identities);
                    AddIndexed(resource,
                        @"^Groups\[\d+\]\.GroupId$",
                        "security-group", "secured-by",
                        resources, output, identities);
                    break;

                case "volume":
                    AddIndexed(resource,
                        @"^Attachments\[\d+\]\.InstanceId$",
                        "compute-instance", "attached-to",
                        resources, output, identities);
                    break;

                case "route-table":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    // AWS exposes subnet association on the route table,
                    // but the dependency points from subnet to route table.
                    AddReverseIndexed(resource,
                        @"^Associations\[\d+\]\.SubnetId$",
                        "subnet", "uses",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^Routes\[\d+\]\.NatGatewayId$",
                        "nat-gateway", "uses",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^Routes\[\d+\]\.GatewayId$",
                        "internet-gateway", "uses",
                        resources, output, identities);
                    break;

                case "security-group":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    // R8E.2: Explicit ingress and egress group references.
                    // Preserve owning-to-referenced direction.
                    // Do not infer network reachability or traffic flow.
                    AddIndexed(resource,
                        @"^IpPermissions\[\d+\]\.UserIdGroupPairs\[\d+\]\.GroupId$",
                        "security-group", "references",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^IpPermissionsEgress\[\d+\]\.UserIdGroupPairs\[\d+\]\.GroupId$",
                        "security-group", "references",
                        resources, output, identities);
                    break;

                case "network-acl":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    // The subnet is secured by its associated NACL.
                    AddReverseIndexed(resource,
                        @"^Associations\[\d+\]\.SubnetId$",
                        "subnet", "secured-by",
                        resources, output, identities);
                    break;

                case "nat-gateway":
                    AddReference(resource, "VpcId", "virtual-network",
                        "member-of", resources, output, identities);
                    AddReference(resource, "SubnetId", "subnet",
                        "hosted-on", resources, output, identities);
                    break;

                case "internet-gateway":
                    AddIndexed(resource,
                        @"^Attachments\[\d+\]\.VpcId$",
                        "virtual-network", "attached-to",
                        resources, output, identities);
                    break;

                case "database-instance":
                    AddReference(resource, "DBSubnetGroup.VpcId",
                        "virtual-network", "member-of",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^DBSubnetGroup\.Subnets\[\d+\]\.SubnetIdentifier$",
                        "subnet", "uses",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^VpcSecurityGroups\[\d+\]\.VpcSecurityGroupId$",
                        "security-group", "secured-by",
                        resources, output, identities);
                    break;

                case "function":
                    AddReference(resource, "VpcConfig.VpcId",
                        "virtual-network", "member-of",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^VpcConfig\.SubnetIds\[\d+\]$",
                        "subnet", "uses",
                        resources, output, identities);
                    AddIndexed(resource,
                        @"^VpcConfig\.SecurityGroupIds\[\d+\]$",
                        "security-group", "secured-by",
                        resources, output, identities);
                    break;
            }
        }

        return output;
    }

    private static void AddIndexed(
        InfrastructureResource source,
        string propertyPattern,
        string targetType,
        string relationshipType,
        IReadOnlyList<InfrastructureResource> resources,
        List<InfrastructureRelationship> output,
        HashSet<string> identities)
    {
        foreach (var property in source.Properties
                     .OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!Regex.IsMatch(
                property.Key, propertyPattern,
                RegexOptions.CultureInvariant))
            {
                continue;
            }

            AddResolvedReference(
                source, property.Key, property.Value,
                targetType, relationshipType,
                resources, output, identities);
        }
    }

    // R8E.1: A provider response can carry a reference in the
    // owning resource while the semantic edge points the other way.
    // Only a single, precisely scoped discovered source is accepted.
    private static void AddReverseIndexed(
        InfrastructureResource owner,
        string propertyPattern,
        string sourceType,
        string relationshipType,
        IReadOnlyList<InfrastructureResource> resources,
        List<InfrastructureRelationship> output,
        HashSet<string> identities)
    {
        foreach (var property in owner.Properties
                     .OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!Regex.IsMatch(
                property.Key,
                propertyPattern,
                RegexOptions.CultureInvariant)
                || string.IsNullOrWhiteSpace(property.Value))
            {
                continue;
            }

            var candidates = resources.Where(candidate =>
                StringComparer.Ordinal.Equals(
                    candidate.ProviderId, owner.ProviderId)
                && StringComparer.Ordinal.Equals(
                    candidate.AccountId, owner.AccountId)
                && StringComparer.Ordinal.Equals(
                    candidate.Location, owner.Location)
                && StringComparer.Ordinal.Equals(
                    candidate.ResourceType, sourceType)
                && StringComparer.Ordinal.Equals(
                    candidate.NativeId, property.Value)
                && !StringComparer.Ordinal.Equals(
                    candidate.ResourceId, owner.ResourceId))
                .ToArray();

            if (candidates.Length != 1)
            {
                continue;
            }

            AddResolvedReference(
                candidates[0], property.Key, owner.NativeId,
                owner.ResourceType, relationshipType,
                resources, output, identities);
        }
    }

    private static void AddReference(
        InfrastructureResource source,
        string propertyName,
        string targetType,
        string relationshipType,
        IReadOnlyList<InfrastructureResource> resources,
        List<InfrastructureRelationship> output,
        HashSet<string> identities)
    {
        if (!source.Properties.TryGetValue(
            propertyName, out var nativeId))
        {
            return;
        }

        AddResolvedReference(
            source, propertyName, nativeId,
            targetType, relationshipType,
            resources, output, identities);
    }

    private static void AddResolvedReference(
        InfrastructureResource source,
        string propertyName,
        string? targetNativeId,
        string targetType,
        string relationshipType,
        IReadOnlyList<InfrastructureResource> resources,
        List<InfrastructureRelationship> output,
        HashSet<string> identities)
    {
        if (string.IsNullOrWhiteSpace(targetNativeId))
        {
            return;
        }

        var targets = resources.Where(target =>
            StringComparer.Ordinal.Equals(
                target.ProviderId, source.ProviderId)
            && StringComparer.Ordinal.Equals(
                target.AccountId, source.AccountId)
            && StringComparer.Ordinal.Equals(
                target.Location, source.Location)
            && StringComparer.Ordinal.Equals(
                target.ResourceType, targetType)
            && StringComparer.Ordinal.Equals(
                target.NativeId, targetNativeId)
            && !StringComparer.Ordinal.Equals(
                target.ResourceId, source.ResourceId))
            .ToArray();

        if (targets.Length != 1)
        {
            return;
        }

        var targetId = targets[0].ResourceId;
        var identity = string.Join(
            "|", source.ResourceId,
            targetId, relationshipType);

        if (!identities.Add(identity))
        {
            return;
        }

        output.Add(new InfrastructureRelationship
        {
            SourceResourceId = source.ResourceId,
            TargetResourceId = targetId,
            Type = relationshipType,
            Description = propertyName
        });
    }
}

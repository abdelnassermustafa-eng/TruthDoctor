using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// AWS resource-type classification for the desktop workbench.
/// Discovery scope and authorization remain independent of UI domains.
/// </summary>
public static class AwsResourceDomainClassifier
{
    private static readonly IReadOnlyDictionary<string, string> Domains =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["compute-instance"] = "compute",
            ["instance"] = "compute",
            ["key-pair"] = "compute",
            ["virtual-network"] = "networking",
            ["vpc"] = "networking",
            ["subnet"] = "networking",
            ["route-table"] = "networking",
            ["internet-gateway"] = "networking",
            ["nat-gateway"] = "networking",
            ["security-group"] = "networking",
            ["network-acl"] = "networking",
            ["network-interface"] = "networking",
            ["vpc-endpoint"] = "networking",
            ["elastic-ip"] = "networking",
            ["volume"] = "storage",
            ["storage-bucket"] = "storage",
            ["database-instance"] = "database",
            ["function"] = "serverless"
        };

    public static string Classify(string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        return Domains.TryGetValue(resourceType, out var domain)
            ? domain
            : "infrastructure";
    }

    public static string DisplayName(string domainId) =>
        domainId switch
        {
            "compute" => "Compute",
            "networking" => "Networking",
            "storage" => "Storage",
            "database" => "Databases",
            "serverless" => "Serverless",
            _ => "Infrastructure"
        };
}

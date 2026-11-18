using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// AWS CLI failure routing shared by the production process
/// runner and offline contract tests.
///
/// AWS STS/CloudTrail failures use sanitized typed transport.
/// Other discovery commands preserve their existing behavior.
///
/// No raw stderr is retained in returned exception objects.
/// </summary>
public static class AwsCliFailureRouter
{
    public static Exception CreateException(
        IReadOnlyList<string> arguments,
        string? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        bool isEnrichmentCommand = false;

        foreach (string argument in arguments)
        {
            if (StringComparer.Ordinal.Equals(
                    argument, "sts") ||
                StringComparer.Ordinal.Equals(
                    argument, "cloudtrail"))
            {
                isEnrichmentCommand = true;
                break;
            }
        }

        if (isEnrichmentCommand)
        {
            return AwsCloudTrailCliFailureTransport
                .FromDiagnostic(diagnostic);
        }

        // Preserve the existing ordinary-discovery behavior.
        if (!string.IsNullOrEmpty(diagnostic) &&
            (diagnostic.Contains(
                "AccessDenied",
                StringComparison.OrdinalIgnoreCase) ||
             diagnostic.Contains(
                "UnauthorizedOperation",
                StringComparison.OrdinalIgnoreCase)))
        {
            return new AwsDiscoveryAccessDeniedException();
        }

        return new InvalidOperationException(
            "AWS CLI read-only discovery failed.");
    }
}

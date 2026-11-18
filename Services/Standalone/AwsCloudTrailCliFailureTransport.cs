using System;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Sanitized AWS CLI transport failure.
/// No raw AWS stderr or credential material is retained.
/// </summary>
public sealed class AwsCloudTrailCliFailureException : Exception
{
    public AwsCloudTrailEnrichmentFailureKind FailureKind
    {
        get;
    }

    internal AwsCloudTrailCliFailureException(
        AwsCloudTrailEnrichmentFailureKind failureKind)
        : base("AWS read-only command failed.")
    {
        FailureKind = failureKind;
    }
}

/// <summary>
/// Extracts only a bounded AWS service-error code from CLI
/// diagnostics. All other information is discarded.
/// </summary>
public static class AwsCloudTrailCliFailureTransport
{
    public static AwsCloudTrailCliFailureException FromDiagnostic(
        string? diagnostic)
        => new(ClassifyDiagnostic(diagnostic));

    public static AwsCloudTrailEnrichmentFailureKind
        ClassifyDiagnostic(string? diagnostic)
    {
        if (string.IsNullOrWhiteSpace(diagnostic) ||
            diagnostic.Length > 65536)
        {
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;
        }

        const string prefix = "An error occurred (";
        const string suffix = ") when calling the ";

        int begin = diagnostic.IndexOf(
            prefix, StringComparison.Ordinal);

        if (begin < 0)
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;

        begin += prefix.Length;

        int end = diagnostic.IndexOf(
            suffix, begin, StringComparison.Ordinal);

        if (end <= begin || end - begin > 64)
            return AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable;

        string code = diagnostic[begin..end];

        foreach (char c in code)
        {
            if (!char.IsAsciiLetterOrDigit(c))
                return AwsCloudTrailEnrichmentFailureKind
                    .ProviderUnavailable;
        }

        return code switch
        {
            "ExpiredToken" or
            "ExpiredTokenException" or
            "InvalidClientTokenId" or
            "UnrecognizedClientException" or
            "InvalidAccessKeyId" or
            "SignatureDoesNotMatch" or
            "IncompleteSignature" or
            "RequestExpired"
                => AwsCloudTrailEnrichmentFailureKind
                    .CredentialRejected,

            "AccessDenied" or
            "AccessDeniedException" or
            "UnauthorizedOperation"
                => AwsCloudTrailEnrichmentFailureKind
                    .PermissionDenied,

            _ => AwsCloudTrailEnrichmentFailureKind
                .ProviderUnavailable
        };
    }
}

using System;
using System.Collections.Generic;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3InitialAuthorityEvidenceShapeInspectorHardeningTests
{
    private static Dictionary<string, string> ValidEvidence()
    {
        return new Dictionary<string, string>(
            StringComparer.Ordinal)
        {
            ["authority_origin"] = "synthetic-origin",
            ["publisher_identity"] = "synthetic-publisher",
            ["artifact_identity"] = "synthetic-artifact",
            ["root_fingerprint"] = "synthetic-fingerprint",
            ["schema_version"] = "1",
            ["product_identity"] = "TruthDoctorV3",
            ["environment_identity"] = "synthetic-test"
        };
    }

    private static string Inspect(
        IReadOnlyDictionary<string, string>? evidence)
    {
        return TruthDoctorV3InitialAuthorityEvidenceShapeInspector
            .Inspect(
                evidence,
                "TruthDoctorV3",
                "synthetic-test");
    }

    [Fact]
    public void EmptyAuthorityOriginMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["authority_origin"] = string.Empty;

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void WhitespacePublisherIdentityMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["publisher_identity"] = "   ";

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void BlankRootFingerprintMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["root_fingerprint"] = "\t \t";

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void OversizedFingerprintMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["root_fingerprint"] = new string('x', 257);

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void ProvisionalLengthBoundaryIsReviewOnly()
    {
        var evidence = ValidEvidence();
        evidence["root_fingerprint"] = new string('x', 256);

        Assert.Equal("REVIEW_ONLY", Inspect(evidence));
    }

    [Fact]
    public void AlternateSchemaFormattingMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["schema_version"] = "01";

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void ProductScopeComparisonMustBeOrdinal()
    {
        var evidence = ValidEvidence();
        evidence["product_identity"] = "truthdoctorv3";

        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void EnvironmentScopeComparisonMustBeOrdinal()
    {
        var evidence = ValidEvidence();
        evidence["environment_identity"] = "Synthetic-Test";

        Assert.Equal("DENY", Inspect(evidence));
    }
}

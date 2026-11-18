using System;
using System.Collections.Generic;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3InitialAuthorityEvidenceBindingReferenceHardeningTests
{
    private static Dictionary<string, string> Evidence()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
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

    private static string Evaluate(
        IReadOnlyDictionary<string, string>? claimed,
        IReadOnlyDictionary<string, string>? expected)
    {
        return TruthDoctorV3InitialAuthorityEvidenceBindingReferenceEvaluator
            .Evaluate(claimed, expected);
    }

    [Fact]
    public void MissingClaimedAuthorityOriginMustBeDenied()
    {
        var claimed = Evidence();
        claimed.Remove("authority_origin");

        Assert.Equal("DENY", Evaluate(claimed, Evidence()));
    }

    [Fact]
    public void MissingExpectedRootFingerprintMustBeDenied()
    {
        var expected = Evidence();
        expected.Remove("root_fingerprint");

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void BlankExpectedPublisherMustBeDenied()
    {
        var expected = Evidence();
        expected["publisher_identity"] = "   ";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void OversizedClaimedArtifactIdentityMustBeDenied()
    {
        var claimed = Evidence();
        claimed["artifact_identity"] = new string('a', 257);

        Assert.Equal("DENY", Evaluate(claimed, Evidence()));
    }

    [Fact]
    public void OversizedExpectedFingerprintMustBeDenied()
    {
        var expected = Evidence();
        expected["root_fingerprint"] = new string('f', 257);

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void AuthorityOriginMismatchMustBeDenied()
    {
        var expected = Evidence();
        expected["authority_origin"] = "different-origin";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void SchemaVersionMismatchMustBeDenied()
    {
        var expected = Evidence();
        expected["schema_version"] = "01";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void PublisherIdentityMatchingMustBeOrdinal()
    {
        var expected = Evidence();
        expected["publisher_identity"] = "Synthetic-Publisher";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }
}

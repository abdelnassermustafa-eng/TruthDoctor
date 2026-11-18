using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3InitialAuthorityEvidenceBindingReferenceEvaluatorTests
{
    private const string EvaluatorName =
        "TruthDoctorV3InitialAuthorityEvidenceBindingReferenceEvaluator";

    private const string MissingEvaluator =
        "RED: initial authority evidence binding reference evaluator not implemented.";

    private static Type RequireEvaluator()
    {
        Assembly application = Assembly.Load("TruthDoctor");

        Type? evaluator = application.GetTypes()
            .SingleOrDefault(type => type.Name == EvaluatorName);

        Assert.True(evaluator is not null, MissingEvaluator);
        return evaluator!;
    }

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

    private static Dictionary<string, string> Expected()
    {
        // This is an independent caller-supplied test record,
        // NOT an authenticated external trust authority.
        return Evidence();
    }

    private static string Evaluate(
        IReadOnlyDictionary<string, string>? claimed,
        IReadOnlyDictionary<string, string>? expected)
    {
        Type type = RequireEvaluator();

        MethodInfo? method = type.GetMethod(
            "Evaluate",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(IReadOnlyDictionary<string, string>),
                typeof(IReadOnlyDictionary<string, string>)
            },
            modifiers: null);

        Assert.NotNull(method);

        object? value = method!.Invoke(
            null,
            new object?[] { claimed, expected });

        Assert.IsType<string>(value);
        return (string)value!;
    }

    [Fact]
    public void NullClaimedEvidenceMustBeDenied()
    {
        Assert.Equal("DENY", Evaluate(null, Expected()));
    }

    [Fact]
    public void NullExpectedReferenceMustBeDenied()
    {
        Assert.Equal("DENY", Evaluate(Evidence(), null));
    }

    [Fact]
    public void MissingExpectedPublisherMustBeDenied()
    {
        var expected = Expected();
        expected.Remove("publisher_identity");

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void PublisherMismatchMustBeDenied()
    {
        var expected = Expected();
        expected["publisher_identity"] = "different-publisher";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void ArtifactIdentityMismatchMustBeDenied()
    {
        var expected = Expected();
        expected["artifact_identity"] = "different-artifact";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void RootFingerprintMismatchMustBeDenied()
    {
        var expected = Expected();
        expected["root_fingerprint"] = "different-fingerprint";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void ProductAndEnvironmentMismatchMustBeDenied()
    {
        var expected = Expected();
        expected["product_identity"] = "OtherProduct";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));

        expected = Expected();
        expected["environment_identity"] = "other-environment";

        Assert.Equal("DENY", Evaluate(Evidence(), expected));
    }

    [Fact]
    public void MatchingSyntheticReferencesRemainReviewOnly()
    {
        var actual = Evidence();
        var expected = Expected();

        Assert.Equal(
            "REVIEW_ONLY",
            Evaluate(actual, expected));

        Assert.NotEqual(
            "TRUSTED",
            Evaluate(actual, expected));

        Assert.NotEqual(
            "INSTALL_ALLOWED",
            Evaluate(actual, expected));
    }
}

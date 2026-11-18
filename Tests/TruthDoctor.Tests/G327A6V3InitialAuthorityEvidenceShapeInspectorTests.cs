using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3InitialAuthorityEvidenceShapeInspectorTests
{
    private const string InspectorName =
        "TruthDoctorV3InitialAuthorityEvidenceShapeInspector";

    private const string MissingInspector =
        "RED: initial authority evidence shape inspector not implemented.";

    private static Type RequireInspector()
    {
        Assembly application = Assembly.Load("TruthDoctor");

        Type? inspector = application.GetTypes()
            .SingleOrDefault(type => type.Name == InspectorName);

        Assert.True(inspector is not null, MissingInspector);
        return inspector!;
    }

    private static Dictionary<string, string> ValidEvidence()
    {
        return new Dictionary<string, string>(
            StringComparer.Ordinal)
        {
            ["authority_origin"] = "synthetic-offline-origin",
            ["publisher_identity"] = "synthetic-publisher",
            ["artifact_identity"] = "synthetic-artifact",
            ["root_fingerprint"] = "synthetic-root-reference",
            ["schema_version"] = "1",
            ["product_identity"] = "TruthDoctorV3",
            ["environment_identity"] = "synthetic-test"
        };
    }

    private static string Inspect(
        IReadOnlyDictionary<string, string>? evidence,
        string product = "TruthDoctorV3",
        string environment = "synthetic-test")
    {
        Type type = RequireInspector();

        MethodInfo? method = type.GetMethod(
            "Inspect",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(IReadOnlyDictionary<string, string>),
                typeof(string),
                typeof(string)
            },
            modifiers: null);

        Assert.NotNull(method);

        object? outcome = method!.Invoke(
            null,
            new object?[] { evidence, product, environment });

        Assert.IsType<string>(outcome);
        return (string)outcome!;
    }

    [Fact]
    public void NullEvidenceMustBeDenied()
    {
        Assert.Equal("DENY", Inspect(null));
    }

    [Fact]
    public void MissingAuthorityOriginMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence.Remove("authority_origin");
        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void MissingPublisherIdentityMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence.Remove("publisher_identity");
        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void MissingArtifactIdentityMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence.Remove("artifact_identity");
        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void MissingRootFingerprintMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence.Remove("root_fingerprint");
        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void UnknownSchemaVersionMustBeDenied()
    {
        var evidence = ValidEvidence();
        evidence["schema_version"] = "99";
        Assert.Equal("DENY", Inspect(evidence));
    }

    [Fact]
    public void CrossScopeIdentityMismatchMustBeDenied()
    {
        var evidence = ValidEvidence();

        Assert.Equal(
            "DENY",
            Inspect(
                evidence,
                product: "DifferentProduct",
                environment: "synthetic-test"));

        Assert.Equal(
            "DENY",
            Inspect(
                evidence,
                product: "TruthDoctorV3",
                environment: "DifferentEnvironment"));
    }

    [Fact]
    public void WellFormedSyntheticEvidenceMustRemainReviewOnly()
    {
        var evidence = ValidEvidence();

        Assert.Equal(
            "REVIEW_ONLY",
            Inspect(evidence));

        Assert.NotEqual(
            "TRUSTED",
            Inspect(evidence));

        Assert.NotEqual(
            "INSTALL_ALLOWED",
            Inspect(evidence));
    }
}

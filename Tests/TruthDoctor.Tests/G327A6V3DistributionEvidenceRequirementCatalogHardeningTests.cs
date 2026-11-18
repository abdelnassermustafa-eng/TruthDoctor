using System;
using System.Linq;
using TruthDoctor.Services.Providers;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327A6V3DistributionEvidenceRequirementCatalogHardeningTests
{
    private static Type Catalog =>
        typeof(TruthDoctorV3DistributionEvidenceRequirementCatalog);

    [Fact]
    public void NullIdentifierIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage(null!));
    }

    [Fact]
    public void EmptyIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage(string.Empty));
    }

    [Fact]
    public void WhitespaceIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage("   "));
    }

    [Fact]
    public void LowercaseIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage("d01-r01"));
    }

    [Fact]
    public void MixedCaseIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage("D01-r01"));
    }

    [Fact]
    public void WhitespacePaddedIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage(" D01-R01 "));
    }

    [Fact]
    public void UnknownWellFormedIdentifierIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage("D99-R99"));
    }

    [Fact]
    public void ReturnedIdentifierArrayCannotMutateCatalogState()
    {
        string[] first = Assert.IsType<string[]>(
            TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetAllRequirementIds());

        Assert.Equal(16, first.Length);
        Assert.Equal("D01-R01", first[0]);

        first[0] = "SUBSTITUTED";
        first[15] = "SUBSTITUTED";

        string[] second = Assert.IsType<string[]>(
            TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetAllRequirementIds());

        Assert.NotSame(first, second);
        Assert.Equal(16, second.Length);
        Assert.Equal("D01-R01", second[0]);
        Assert.Equal("D04-R04", second[15]);
        Assert.DoesNotContain("SUBSTITUTED", second);

        Assert.Equal(
            "publisher_identity",
            TruthDoctorV3DistributionEvidenceRequirementCatalog
                .GetStage("D01-R01"));

        Assert.True(Catalog.IsClass);
    }
}

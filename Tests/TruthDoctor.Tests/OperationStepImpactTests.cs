using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationStepImpactTests
{
    private static OperationStepImpact Create(
        string stepId = "step-a",
        OperationImpactSeverity severity = OperationImpactSeverity.Unknown,
        OperationImpactConfidence confidence = OperationImpactConfidence.Unknown,
        int? estimatedAffectedResourceCount = null,
        bool? availabilityDisruptionExpected = null,
        string? rationale = null)
    {
        return new OperationStepImpact(
            stepId,
            severity,
            confidence,
            estimatedAffectedResourceCount,
            availabilityDisruptionExpected,
            rationale);
    }

    [Fact]
    public void SeverityEnumHasExactApprovedValues()
    {
        Assert.Equal(
            new[] { "Unknown", "None", "Low", "Moderate", "High" },
            Enum.GetNames<OperationImpactSeverity>());
        Assert.Equal(0, (int)OperationImpactSeverity.Unknown);
        Assert.Equal(1, (int)OperationImpactSeverity.None);
        Assert.Equal(2, (int)OperationImpactSeverity.Low);
        Assert.Equal(3, (int)OperationImpactSeverity.Moderate);
        Assert.Equal(4, (int)OperationImpactSeverity.High);
    }

    [Fact]
    public void ConfidenceEnumHasExactApprovedValues()
    {
        Assert.Equal(
            new[] { "Unknown", "Low", "Medium", "High" },
            Enum.GetNames<OperationImpactConfidence>());
        Assert.Equal(0, (int)OperationImpactConfidence.Unknown);
        Assert.Equal(1, (int)OperationImpactConfidence.Low);
        Assert.Equal(2, (int)OperationImpactConfidence.Medium);
        Assert.Equal(3, (int)OperationImpactConfidence.High);
    }

    [Fact]
    public void PublicInstancePropertiesAreExactlySixGetterOnlyProperties()
    {
        var properties = typeof(OperationStepImpact).GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[]
            {
                nameof(OperationStepImpact.StepId),
                nameof(OperationStepImpact.Severity),
                nameof(OperationStepImpact.Confidence),
                nameof(OperationStepImpact.EstimatedAffectedResourceCount),
                nameof(OperationStepImpact.AvailabilityDisruptionExpected),
                nameof(OperationStepImpact.Rationale)
            }.OrderBy(x => x),
            properties.Select(x => x.Name).OrderBy(x => x));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.StepId))!.PropertyType);
        Assert.Equal(typeof(OperationImpactSeverity),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.Severity))!.PropertyType);
        Assert.Equal(typeof(OperationImpactConfidence),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.Confidence))!.PropertyType);
        Assert.Equal(typeof(int?),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.EstimatedAffectedResourceCount))!.PropertyType);
        Assert.Equal(typeof(bool?),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.AvailabilityDisruptionExpected))!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(OperationStepImpact).GetProperty(
                nameof(OperationStepImpact.Rationale))!.PropertyType);
    }

    [Fact]
    public void ExactlyOnePublicConstructorHasLockedSignatureAndDefaults()
    {
        var constructor = Assert.Single(
            typeof(OperationStepImpact).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(
            new[]
            {
                "stepId",
                "severity",
                "confidence",
                "estimatedAffectedResourceCount",
                "availabilityDisruptionExpected",
                "rationale"
            },
            parameters.Select(x => x.Name));

        Assert.Equal(
            new[]
            {
                typeof(string),
                typeof(OperationImpactSeverity),
                typeof(OperationImpactConfidence),
                typeof(int?),
                typeof(bool?),
                typeof(string)
            },
            parameters.Select(x => x.ParameterType));

        Assert.All(parameters.Take(3), parameter =>
            Assert.False(parameter.IsOptional));

        Assert.All(parameters.Skip(3), parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void TypeIsSealedAndPublic()
    {
        Assert.True(typeof(OperationStepImpact).IsSealed);
        Assert.True(typeof(OperationStepImpact).IsPublic);
    }

    [Fact]
    public void DefaultOptionalDimensionsRemainUnknown()
    {
        var impact = Create();

        Assert.Equal("step-a", impact.StepId);
        Assert.Equal(OperationImpactSeverity.Unknown, impact.Severity);
        Assert.Equal(OperationImpactConfidence.Unknown, impact.Confidence);
        Assert.Null(impact.EstimatedAffectedResourceCount);
        Assert.Null(impact.AvailabilityDisruptionExpected);
        Assert.Null(impact.Rationale);
    }

    [Fact]
    public void AllDeclaredDimensionsArePreserved()
    {
        var impact = Create(
            "step-b",
            OperationImpactSeverity.High,
            OperationImpactConfidence.Medium,
            12,
            true,
            "May interrupt dependent services");

        Assert.Equal("step-b", impact.StepId);
        Assert.Equal(OperationImpactSeverity.High, impact.Severity);
        Assert.Equal(OperationImpactConfidence.Medium, impact.Confidence);
        Assert.Equal(12, impact.EstimatedAffectedResourceCount);
        Assert.True(impact.AvailabilityDisruptionExpected);
        Assert.Equal(
            "May interrupt dependent services",
            impact.Rationale);
    }

    [Fact]
    public void StepIdIsTrimmedWithoutChangingCase()
    {
        Assert.Equal("Step-A", Create("  Step-A  ").StepId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankStepIdIsRejected(string? stepId)
    {
        Assert.Throws<ArgumentException>(() => Create(stepId!));
    }

    [Theory]
    [InlineData("step\nbreak")]
    [InlineData("step\tbreak")]
    [InlineData("step\0break")]
    public void StepIdControlCharactersAreRejected(string stepId)
    {
        Assert.Throws<ArgumentException>(() => Create(stepId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    public void InvalidSeverityValuesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(severity: (OperationImpactSeverity)value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public void InvalidConfidenceValuesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(confidence: (OperationImpactConfidence)value));
    }

    [Fact]
    public void UnknownAndExplicitNoneSeverityRemainDistinct()
    {
        var unknown = Create();
        var none = Create(severity: OperationImpactSeverity.None);

        Assert.Equal(OperationImpactSeverity.Unknown, unknown.Severity);
        Assert.Equal(OperationImpactSeverity.None, none.Severity);
        Assert.NotEqual(unknown.Severity, none.Severity);
    }

    [Fact]
    public void UnknownConfidenceCanAccompanyHighSeverity()
    {
        var impact = Create(
            severity: OperationImpactSeverity.High);

        Assert.Equal(OperationImpactSeverity.High, impact.Severity);
        Assert.Equal(OperationImpactConfidence.Unknown, impact.Confidence);
    }

    [Fact]
    public void HighConfidenceCanAccompanyUnknownSeverity()
    {
        var impact = Create(
            confidence: OperationImpactConfidence.High);

        Assert.Equal(OperationImpactSeverity.Unknown, impact.Severity);
        Assert.Equal(OperationImpactConfidence.High, impact.Confidence);
    }

    [Fact]
    public void ZeroAffectedResourcesIsDistinctFromUnknown()
    {
        var unknown = Create();
        var zero = Create(estimatedAffectedResourceCount: 0);

        Assert.Null(unknown.EstimatedAffectedResourceCount);
        Assert.Equal(0, zero.EstimatedAffectedResourceCount);
    }

    [Fact]
    public void PositiveAffectedResourceCountIsPreserved()
    {
        var impact = Create(
            estimatedAffectedResourceCount: int.MaxValue);

        Assert.Equal(
            int.MaxValue,
            impact.EstimatedAffectedResourceCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void NegativeAffectedResourceCountIsRejected(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(estimatedAffectedResourceCount: count));
    }

    [Fact]
    public void FalseDisruptionIsDistinctFromUnknown()
    {
        var unknown = Create();
        var explicitlyFalse = Create(
            availabilityDisruptionExpected: false);

        Assert.Null(unknown.AvailabilityDisruptionExpected);
        Assert.False(explicitlyFalse.AvailabilityDisruptionExpected);
    }

    [Fact]
    public void TrueDisruptionCanAccompanyUnknownSeverity()
    {
        var impact = Create(
            availabilityDisruptionExpected: true);

        Assert.Equal(OperationImpactSeverity.Unknown, impact.Severity);
        Assert.True(impact.AvailabilityDisruptionExpected);
    }

    [Fact]
    public void RationaleIsTrimmedWithoutChangingCase()
    {
        Assert.Equal(
            "Potential Service Interruption",
            Create(rationale: "  Potential Service Interruption  ").Rationale);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void BlankRationaleBecomesNull(string? rationale)
    {
        Assert.Null(Create(rationale: rationale).Rationale);
    }

    [Theory]
    [InlineData("before\nafter")]
    [InlineData("before\tafter")]
    [InlineData("before\0after")]
    public void RationaleControlCharactersAreRejected(string rationale)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(rationale: rationale));
    }

    [Fact]
    public void ExplicitNoneMayRetainRationaleAndUnknownDimensions()
    {
        var impact = Create(
            severity: OperationImpactSeverity.None,
            rationale: "No expected change");

        Assert.Equal(OperationImpactSeverity.None, impact.Severity);
        Assert.Null(impact.EstimatedAffectedResourceCount);
        Assert.Null(impact.AvailabilityDisruptionExpected);
        Assert.Equal("No expected change", impact.Rationale);
    }
}

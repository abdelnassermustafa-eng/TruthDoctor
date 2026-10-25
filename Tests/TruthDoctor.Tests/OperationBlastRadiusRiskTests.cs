using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationBlastRadiusRiskTests
{
    [Fact]
    public void PublicApiIsSealedAndHasThreeGetterOnlyProperties()
    {
        var type = typeof(OperationBlastRadiusRisk);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[]
            {
                "AffectedDependentResourceCount",
                "Confidence",
                "DeclaredRiskLevel"
            },
            properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(
            typeof(int?),
            type.GetProperty("AffectedDependentResourceCount")!.PropertyType);
        Assert.Equal(
            typeof(OperationImpactConfidence),
            type.GetProperty("Confidence")!.PropertyType);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasThreeOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationBlastRadiusRisk).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(3, parameters.Length);

        Assert.Equal(
            new[]
            {
                "affectedDependentResourceCount",
                "confidence",
                "declaredRiskLevel"
            },
            parameters.Select(p => p.Name));

        Assert.Equal(typeof(int?), parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationImpactConfidence),
            parameters[1].ParameterType);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            parameters[2].ParameterType);

        Assert.All(parameters, parameter =>
            Assert.True(parameter.IsOptional));

        Assert.Null(parameters[0].DefaultValue);
        Assert.Equal(
            OperationImpactConfidence.Unknown,
            parameters[1].DefaultValue);
        Assert.Null(parameters[2].DefaultValue);
    }

    [Fact]
    public void DefaultsPreserveUnknownRatherThanImplyingZeroOrR0()
    {
        var risk = new OperationBlastRadiusRisk();

        Assert.Null(risk.AffectedDependentResourceCount);
        Assert.Equal(OperationImpactConfidence.Unknown, risk.Confidence);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void ExplicitZeroIsDistinctFromUnknown()
    {
        var unknown = new OperationBlastRadiusRisk();
        var zero = new OperationBlastRadiusRisk(
            affectedDependentResourceCount: 0);

        Assert.Null(unknown.AffectedDependentResourceCount);
        Assert.Equal(0, zero.AffectedDependentResourceCount);
        Assert.Null(zero.DeclaredRiskLevel);
    }

    [Fact]
    public void LargeNonnegativeCountIsPreservedWithoutRiskInference()
    {
        var risk = new OperationBlastRadiusRisk(
            affectedDependentResourceCount: int.MaxValue);

        Assert.Equal(int.MaxValue, risk.AffectedDependentResourceCount);
        Assert.Equal(OperationImpactConfidence.Unknown, risk.Confidence);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void AllThreeDimensionsRemainIndependent()
    {
        var first = new OperationBlastRadiusRisk(
            affectedDependentResourceCount: 12,
            confidence: OperationImpactConfidence.Low,
            declaredRiskLevel: OperationRiskLevel.R1);

        var second = new OperationBlastRadiusRisk(
            affectedDependentResourceCount: 12,
            confidence: OperationImpactConfidence.High,
            declaredRiskLevel: OperationRiskLevel.R5);

        Assert.Equal(12, first.AffectedDependentResourceCount);
        Assert.Equal(12, second.AffectedDependentResourceCount);
        Assert.Equal(OperationImpactConfidence.Low, first.Confidence);
        Assert.Equal(OperationImpactConfidence.High, second.Confidence);
        Assert.Equal(OperationRiskLevel.R1, first.DeclaredRiskLevel);
        Assert.Equal(OperationRiskLevel.R5, second.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void NegativeAffectedResourceCountsAreRejected(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationBlastRadiusRisk(
                affectedDependentResourceCount: count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ValidConfidenceValuesArePreserved(int value)
    {
        var expected = (OperationImpactConfidence)value;

        var risk = new OperationBlastRadiusRisk(
            confidence: expected);

        Assert.Equal(expected, risk.Confidence);
        Assert.Null(risk.AffectedDependentResourceCount);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public void UndefinedConfidenceValuesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationBlastRadiusRisk(
                confidence: (OperationImpactConfidence)value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllRiskLevelsMayBeExplicitlyDeclared(int value)
    {
        var expected = (OperationRiskLevel)value;

        var risk = new OperationBlastRadiusRisk(
            declaredRiskLevel: expected);

        Assert.Equal(expected, risk.DeclaredRiskLevel);
        Assert.Null(risk.AffectedDependentResourceCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    public void UndefinedRiskLevelsAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationBlastRadiusRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}

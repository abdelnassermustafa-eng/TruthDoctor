using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationReversibilityRiskTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        Assert.True(typeof(OperationReversibilityRisk).IsPublic);
        Assert.True(typeof(OperationReversibilityRisk).IsSealed);
    }

    [Fact]
    public void PublicApiHasExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationReversibilityRisk)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "DeclaredRiskLevel", "Status" },
            properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, p =>
        {
            Assert.NotNull(p.GetMethod);
            Assert.Null(p.SetMethod);
        });

        Assert.Equal(
            typeof(CapabilityReversibilityStatus),
            typeof(OperationReversibilityRisk)
                .GetProperty("Status")!.PropertyType);

        Assert.Equal(
            typeof(OperationRiskLevel?),
            typeof(OperationReversibilityRisk)
                .GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var ctor = Assert.Single(
            typeof(OperationReversibilityRisk).GetConstructors());

        var parameters = ctor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("status", parameters[0].Name);
        Assert.Equal("declaredRiskLevel", parameters[1].Name);
        Assert.Equal(
            typeof(CapabilityReversibilityStatus),
            parameters[0].ParameterType);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            parameters[1].ParameterType);
        Assert.All(parameters, p => Assert.True(p.IsOptional));
        Assert.Equal(
            CapabilityReversibilityStatus.Unknown,
            parameters[0].DefaultValue);
        Assert.Null(parameters[1].DefaultValue);
    }

    [Fact]
    public void DefaultRemainsUnknownAndUnassessed()
    {
        var risk = new OperationReversibilityRisk();

        Assert.Equal(CapabilityReversibilityStatus.Unknown, risk.Status);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void ReversibilityAndDeclaredRiskRemainIndependent()
    {
        var supportedHigh = new OperationReversibilityRisk(
            CapabilityReversibilityStatus.Supported,
            OperationRiskLevel.R5);

        var unsupportedLow = new OperationReversibilityRisk(
            CapabilityReversibilityStatus.Unsupported,
            OperationRiskLevel.R0);

        Assert.Equal(
            CapabilityReversibilityStatus.Supported,
            supportedHigh.Status);
        Assert.Equal(OperationRiskLevel.R5, supportedHigh.DeclaredRiskLevel);
        Assert.Equal(
            CapabilityReversibilityStatus.Unsupported,
            unsupportedLow.Status);
        Assert.Equal(OperationRiskLevel.R0, unsupportedLow.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ValidReversibilityStatusesArePreserved(int value)
    {
        var status = (CapabilityReversibilityStatus)value;
        var risk = new OperationReversibilityRisk(status);

        Assert.Equal(status, risk.Status);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void UndefinedReversibilityStatusesAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationReversibilityRisk(
                (CapabilityReversibilityStatus)value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EveryR0ToR5LevelCanBeDeclared(int value)
    {
        var level = (OperationRiskLevel)value;
        var risk = new OperationReversibilityRisk(
            declaredRiskLevel: level);

        Assert.Equal(level, risk.DeclaredRiskLevel);
        Assert.Equal(CapabilityReversibilityStatus.Unknown, risk.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    public void UndefinedRiskLevelsAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationReversibilityRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}

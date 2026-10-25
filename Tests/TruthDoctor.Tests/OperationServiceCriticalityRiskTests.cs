using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationServiceCriticalityRiskTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        var type = typeof(OperationServiceCriticalityRisk);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void PublicApiHasExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationServiceCriticalityRisk);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "CriticalityLabel", "DeclaredRiskLevel" },
            properties.Select(p => p.Name).OrderBy(n => n).ToArray());

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            type.GetProperty("CriticalityLabel")!.PropertyType);
        Assert.Equal(typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationServiceCriticalityRisk).GetConstructors());
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("criticalityLabel", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("declaredRiskLevel", parameters[1].Name);
        Assert.Equal(typeof(OperationRiskLevel?),
            parameters[1].ParameterType);
        Assert.All(parameters, parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void DefaultDoesNotInventCriticalityOrRisk()
    {
        var risk = new OperationServiceCriticalityRisk();
        Assert.Null(risk.CriticalityLabel);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void CriticalityLabelAndRiskAreIndependent()
    {
        var important = new OperationServiceCriticalityRisk(
            "  Mission Critical  ", OperationRiskLevel.R0);
        var ordinary = new OperationServiceCriticalityRisk(
            "Standard Service", OperationRiskLevel.R5);
        var unassessed = new OperationServiceCriticalityRisk(
            "Tier 1");

        Assert.Equal("Mission Critical", important.CriticalityLabel);
        Assert.Equal(OperationRiskLevel.R0, important.DeclaredRiskLevel);
        Assert.Equal("Standard Service", ordinary.CriticalityLabel);
        Assert.Equal(OperationRiskLevel.R5, ordinary.DeclaredRiskLevel);
        Assert.Equal("Tier 1", unassessed.CriticalityLabel);
        Assert.Null(unassessed.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void AbsentLabelsNormalizeToNull(string? label)
    {
        var risk = new OperationServiceCriticalityRisk(
            label, OperationRiskLevel.R2);

        Assert.Null(risk.CriticalityLabel);
        Assert.Equal(OperationRiskLevel.R2, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData("mission\ncritical")]
    [InlineData("mission\rcritical")]
    [InlineData("mission\0critical")]
    public void EmbeddedControlCharactersAreRejected(string label)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationServiceCriticalityRisk(label));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EveryR0ToR5LevelMayBeDeclared(int value)
    {
        var level = (OperationRiskLevel)value;
        var risk = new OperationServiceCriticalityRisk(
            declaredRiskLevel: level);

        Assert.Null(risk.CriticalityLabel);
        Assert.Equal(level, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void UndefinedRiskLevelsAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationServiceCriticalityRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}

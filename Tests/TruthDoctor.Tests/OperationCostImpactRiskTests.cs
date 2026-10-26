using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationCostImpactRiskTests
{
    [Fact]
    public void TypeIsPublicAndSealed()
    {
        var type = typeof(OperationCostImpactRisk);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void PublicApiHasExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationCostImpactRisk);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[] { "CostImpactDescription", "DeclaredRiskLevel" },
            properties.Select(p => p.Name).OrderBy(n => n).ToArray());

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            type.GetProperty("CostImpactDescription")!.PropertyType);
        Assert.Equal(typeof(OperationRiskLevel?),
            type.GetProperty("DeclaredRiskLevel")!.PropertyType);
    }

    [Fact]
    public void ConstructorHasExactlyTwoOptionalParameters()
    {
        var constructor = Assert.Single(
            typeof(OperationCostImpactRisk).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("costImpactDescription", parameters[0].Name);
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
    public void DefaultDoesNotInventCostImpactOrRisk()
    {
        var risk = new OperationCostImpactRisk();

        Assert.Null(risk.CostImpactDescription);
        Assert.Null(risk.DeclaredRiskLevel);
    }

    [Fact]
    public void CostDescriptionAndRiskAreIndependent()
    {
        var first = new OperationCostImpactRisk(
            "  Potential Additional Operating Cost  ",
            OperationRiskLevel.R0);

        var second = new OperationCostImpactRisk(
            "Possible Billing Increase",
            OperationRiskLevel.R5);

        var unassessed = new OperationCostImpactRisk(
            "Potential Maintenance Expense");

        Assert.Equal(
            "Potential Additional Operating Cost",
            first.CostImpactDescription);
        Assert.Equal(
            OperationRiskLevel.R0,
            first.DeclaredRiskLevel);

        Assert.Equal(
            "Possible Billing Increase",
            second.CostImpactDescription);
        Assert.Equal(
            OperationRiskLevel.R5,
            second.DeclaredRiskLevel);

        Assert.Equal(
            "Potential Maintenance Expense",
            unassessed.CostImpactDescription);
        Assert.Null(unassessed.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankDescriptionsNormalizeToNull(string? description)
    {
        var risk = new OperationCostImpactRisk(
            description,
            OperationRiskLevel.R2);

        Assert.Null(risk.CostImpactDescription);
        Assert.Equal(OperationRiskLevel.R2, risk.DeclaredRiskLevel);
    }

    [Theory]
    [InlineData("cost\nimpact")]
    [InlineData("cost\rimpact")]
    [InlineData("cost\0impact")]
    public void EmbeddedControlCharactersAreRejected(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationCostImpactRisk(description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllApprovedRiskLevelsMayBeDeclared(int value)
    {
        var level = (OperationRiskLevel)value;

        var risk = new OperationCostImpactRisk(
            declaredRiskLevel: level);

        Assert.Null(risk.CostImpactDescription);
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
            new OperationCostImpactRisk(
                declaredRiskLevel: (OperationRiskLevel)value));
    }
}

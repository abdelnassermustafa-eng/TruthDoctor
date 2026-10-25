using System;
using System.Linq;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationRiskLevelTests
{
    [Fact]
    public void EnumHasExactlySixApprovedNames()
    {
        Assert.Equal(
            new[] { "R0", "R1", "R2", "R3", "R4", "R5" },
            Enum.GetNames<OperationRiskLevel>());
    }

    [Fact]
    public void NumericIdentitiesAreStable()
    {
        Assert.Equal(0, (int)OperationRiskLevel.R0);
        Assert.Equal(1, (int)OperationRiskLevel.R1);
        Assert.Equal(2, (int)OperationRiskLevel.R2);
        Assert.Equal(3, (int)OperationRiskLevel.R3);
        Assert.Equal(4, (int)OperationRiskLevel.R4);
        Assert.Equal(5, (int)OperationRiskLevel.R5);
    }

    [Fact]
    public void EnumHasApprovedPublicShape()
    {
        var type = typeof(OperationRiskLevel);

        Assert.True(type.IsPublic);
        Assert.True(type.IsEnum);
        Assert.False(type.IsDefined(typeof(FlagsAttribute), false));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
    }

    [Fact]
    public void DefaultValueIsR0()
    {
        Assert.Equal(OperationRiskLevel.R0, default(OperationRiskLevel));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void AllApprovedValuesAreDefined(int value)
    {
        Assert.True(Enum.IsDefined((OperationRiskLevel)value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void UnsupportedValuesAreNotDefined(int value)
    {
        Assert.False(Enum.IsDefined((OperationRiskLevel)value));
    }

    [Fact]
    public void RiskLevelIsIndependentOfImpactSeverity()
    {
        Assert.NotEqual(
            typeof(OperationRiskLevel),
            typeof(OperationImpactSeverity));

        Assert.Equal(
            new[] { "Unknown", "None", "Low", "Moderate", "High" },
            Enum.GetNames<OperationImpactSeverity>());
    }

    [Fact]
    public void RiskValuesEnumerateInAscendingOrder()
    {
        Assert.Equal(
            Enumerable.Range(0, 6),
            Enum.GetValues<OperationRiskLevel>()
                .Select(level => (int)level));
    }
}

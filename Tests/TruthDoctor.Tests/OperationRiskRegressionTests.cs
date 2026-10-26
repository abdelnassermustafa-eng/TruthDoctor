using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Cross-contract regression coverage for independently declared risk data.
/// Does not introduce risk aggregation, policy decisions, or authorization.
/// </summary>
public sealed class OperationRiskRegressionTests
{
    private static object[] RiskFactors(OperationRiskLevel? level)
    {
        return new object[]
        {
            new OperationEnvironmentRisk(declaredRiskLevel: level),
            new OperationBlastRadiusRisk(declaredRiskLevel: level),
            new OperationReversibilityRisk(declaredRiskLevel: level),
            new OperationServiceCriticalityRisk(declaredRiskLevel: level),
            new OperationSecurityImpactRisk(declaredRiskLevel: level),
            new OperationCostImpactRisk(declaredRiskLevel: level),
            new OperationBackupRedundancyRisk(declaredRiskLevel: level),
            new OperationRiskExplanation(declaredRiskLevel: level)
        };
    }

    private static OperationRiskLevel? DeclaredLevel(object factor)
    {
        var property = factor.GetType().GetProperty(
            "DeclaredRiskLevel",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(property);
        Assert.Equal(
            typeof(OperationRiskLevel?),
            property.PropertyType);

        return (OperationRiskLevel?)property.GetValue(factor);
    }

    [Fact]
    public void IndependentExplicitClassifications()
    {
        var levels = new[]
        {
            OperationRiskLevel.R0,
            OperationRiskLevel.R1,
            OperationRiskLevel.R2,
            OperationRiskLevel.R3,
            OperationRiskLevel.R4,
            OperationRiskLevel.R5,
            OperationRiskLevel.R1,
            OperationRiskLevel.R4
        };

        var factors = new object[]
        {
            new OperationEnvironmentRisk(declaredRiskLevel: levels[0]),
            new OperationBlastRadiusRisk(declaredRiskLevel: levels[1]),
            new OperationReversibilityRisk(declaredRiskLevel: levels[2]),
            new OperationServiceCriticalityRisk(declaredRiskLevel: levels[3]),
            new OperationSecurityImpactRisk(declaredRiskLevel: levels[4]),
            new OperationCostImpactRisk(declaredRiskLevel: levels[5]),
            new OperationBackupRedundancyRisk(declaredRiskLevel: levels[6]),
            new OperationRiskExplanation(declaredRiskLevel: levels[7])
        };

        Assert.Equal(8, factors.Length);

        for (var index = 0; index < factors.Length; index++)
        {
            Assert.Equal(
                (OperationRiskLevel?)levels[index],
                DeclaredLevel(factors[index]));
        }
    }

    [Fact]
    public void MixedRiskLevelsNeverOverwriteEachOther()
    {
        var low = new OperationEnvironmentRisk(
            declaredRiskLevel: OperationRiskLevel.R0);
        var high = new OperationSecurityImpactRisk(
            declaredRiskLevel: OperationRiskLevel.R5);
        var medium = new OperationBackupRedundancyRisk(
            declaredRiskLevel: OperationRiskLevel.R3);

        var policy = new OperationPolicyOverrideEscalation(
            overridePolicyId: "POL-001",
            escalationRequestReason: "Review requested");

        Assert.Equal(OperationRiskLevel.R0, low.DeclaredRiskLevel);
        Assert.Equal(OperationRiskLevel.R5, high.DeclaredRiskLevel);
        Assert.Equal(OperationRiskLevel.R3, medium.DeclaredRiskLevel);
        Assert.Equal("POL-001", policy.OverridePolicyId);
        Assert.Equal(OperationRiskLevel.R5, high.DeclaredRiskLevel);
        Assert.Equal(OperationRiskLevel.R0, low.DeclaredRiskLevel);
    }

    [Fact]
    public void MissingClassificationsRemainUnassessed()
    {
        var factors = RiskFactors(null);

        Assert.Equal(8, factors.Length);
        Assert.All(factors, factor =>
            Assert.Null(DeclaredLevel(factor)));

        var policy = new OperationPolicyOverrideEscalation();
        Assert.Null(policy.OverridePolicyId);
        Assert.Null(policy.OverrideRequestReason);
        Assert.Null(policy.EscalationRequestReason);

        Assert.All(factors, factor =>
            Assert.Null(DeclaredLevel(factor)));
    }

    [Fact]
    public void ExplicitR0DiffersFromUnassessed()
    {
        var unassessed = RiskFactors(null);
        var declaredZero = RiskFactors(OperationRiskLevel.R0);

        for (var index = 0; index < unassessed.Length; index++)
        {
            Assert.Null(DeclaredLevel(unassessed[index]));
            Assert.Equal(
                (OperationRiskLevel?)OperationRiskLevel.R0,
                DeclaredLevel(declaredZero[index]));
        }
    }

    [Fact]
    public void ExplanationCannotClassifyOtherFactors()
    {
        var explanation = new OperationRiskExplanation(
            explanation: "Manual risk review",
            declaredRiskLevel: OperationRiskLevel.R5);

        var factors = new object[]
        {
            new OperationEnvironmentRisk("Production"),
            new OperationSecurityImpactRisk("Security review required"),
            new OperationCostImpactRisk("Cost impact unknown"),
            new OperationBackupRedundancyRisk(
                backupDescription: "Backup availability unknown")
        };

        Assert.Equal("Manual risk review", explanation.Explanation);
        Assert.Equal(
            OperationRiskLevel.R5,
            explanation.DeclaredRiskLevel);

        Assert.All(factors, factor =>
            Assert.Null(DeclaredLevel(factor)));
    }

    [Fact]
    public void PolicyRequestsCannotMutateRiskDeclarations()
    {
        var factors = RiskFactors(OperationRiskLevel.R4);

        var declaration = new OperationPolicyOverrideEscalation(
            overridePolicyId: "POL-CHANGE",
            overrideRequestReason: "Exception review requested",
            escalationRequestReason: "Escalate to reviewer");

        Assert.Equal("POL-CHANGE", declaration.OverridePolicyId);
        Assert.Equal(
            "Exception review requested",
            declaration.OverrideRequestReason);
        Assert.Equal(
            "Escalate to reviewer",
            declaration.EscalationRequestReason);

        Assert.All(factors, factor =>
            Assert.Equal(
                (OperationRiskLevel?)OperationRiskLevel.R4,
                DeclaredLevel(factor)));
    }

    [Fact]
    public void PublicRiskPropertiesRemainGetterOnly()
    {
        var types = RiskFactors(null)
            .Select(factor => factor.GetType())
            .ToArray();

        Assert.Equal(8, types.Length);

        foreach (var type in types)
        {
            Assert.True(type.IsPublic);
            Assert.True(type.IsSealed);

            var property = type.GetProperty("DeclaredRiskLevel");
            Assert.NotNull(property);
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod.IsPublic);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void DeclarationsNeverConstituteApproval()
    {
        var policy = new OperationPolicyOverrideEscalation(
            overridePolicyId: "POL-EXCEPTION",
            overrideRequestReason: "Request only",
            escalationRequestReason: "Review requested");

        Assert.Equal("POL-EXCEPTION", policy.OverridePolicyId);
        Assert.Equal("Request only", policy.OverrideRequestReason);
        Assert.Equal(
            "Review requested",
            policy.EscalationRequestReason);

        var approvedNames = new[]
        {
            "Approved", "IsApproved", "Authorized",
            "IsAuthorized", "OverrideGranted",
            "EscalationCompleted", "EffectiveRiskLevel"
        };

        var declarations = RiskFactors(OperationRiskLevel.R5)
            .Concat(new object[] { policy })
            .ToArray();

        foreach (var declaration in declarations)
        {
            var type = declaration.GetType();

            Assert.All(
                approvedNames,
                name => Assert.Null(
                    type.GetProperty(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.Static)));
        }

        Assert.All(
            RiskFactors(null),
            factor => Assert.Null(DeclaredLevel(factor)));
    }
}

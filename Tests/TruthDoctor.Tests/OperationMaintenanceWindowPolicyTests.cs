using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationMaintenanceWindowPolicyTests
{
    private static OperationAuthorizationConditions Conditions()
    {
        return new OperationAuthorizationConditions(
            new[]
            {
                new OperationAuthorizationCondition(
                    "environment.name", "Production")
            });
    }

    private static OperationMaintenanceWindowPolicy Create(
        bool amendment = true,
        bool earlyClosure = true,
        bool cancellation = true,
        bool overrun = true,
        bool emergency = true,
        bool verification = true)
    {
        return new OperationMaintenanceWindowPolicy(
            "maintenance-policy",
            Conditions(),
            amendment,
            earlyClosure,
            cancellation,
            overrun,
            emergency,
            verification);
    }

    [Fact]
    public void PreservesNormalizedPolicyId()
    {
        var policy = new OperationMaintenanceWindowPolicy(
            "  Policy-ABC  ",
            Conditions(),
            true, true, true, true, true, true);

        Assert.Equal("Policy-ABC", policy.PolicyId);
    }

    [Fact]
    public void PreservesPolicyIdCase()
    {
        var policy = new OperationMaintenanceWindowPolicy(
            "Policy-MixedCase",
            Conditions(),
            false, false, false, false, false, false);

        Assert.Equal("Policy-MixedCase", policy.PolicyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsNullOrBlankPolicyId(string? policyId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowPolicy(
                policyId!,
                Conditions(),
                false, false, false, false, false, false));
    }

    [Theory]
    [InlineData("policy\nname")]
    [InlineData("policy\rname")]
    [InlineData("policy\tname")]
    [InlineData("policy\0name")]
    public void RejectsEmbeddedControlCharacters(string policyId)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationMaintenanceWindowPolicy(
                policyId,
                Conditions(),
                false, false, false, false, false, false));
    }

    [Fact]
    public void RejectsNullAuthorizationConditions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationMaintenanceWindowPolicy(
                "policy",
                null!,
                true, true, true, true, true, true));
    }

    [Fact]
    public void PreservesExactConditionsReference()
    {
        var conditions = Conditions();

        var policy = new OperationMaintenanceWindowPolicy(
            "policy",
            conditions,
            true, true, true, true, true, true);

        Assert.Same(conditions, policy.Conditions);
        Assert.Single(policy.Conditions.Conditions);
        Assert.Equal(
            "environment.name",
            policy.Conditions.Conditions[0].Key);
    }

    [Fact]
    public void AcceptsExistingEmptyConditionsWithoutAuthorizationInference()
    {
        var empty = new OperationAuthorizationConditions(
            Array.Empty<OperationAuthorizationCondition>());

        var policy = new OperationMaintenanceWindowPolicy(
            "policy",
            empty,
            false, false, false, false, false, false);

        Assert.Same(empty, policy.Conditions);
        Assert.Empty(policy.Conditions.Conditions);
    }

    [Fact]
    public void PreservesAllTrueRequirements()
    {
        var policy = Create();

        Assert.True(policy.RequiresAmendmentApproval);
        Assert.True(policy.RequiresEarlyClosureReview);
        Assert.True(policy.RequiresCancellationReview);
        Assert.True(policy.RequiresOverrunEscalation);
        Assert.True(policy.RequiresEmergencyApproval);
        Assert.True(policy.RequiresPostMaintenanceVerification);
    }

    [Fact]
    public void PreservesAllFalseDeclarations()
    {
        var policy = Create(
            false, false, false, false, false, false);

        Assert.False(policy.RequiresAmendmentApproval);
        Assert.False(policy.RequiresEarlyClosureReview);
        Assert.False(policy.RequiresCancellationReview);
        Assert.False(policy.RequiresOverrunEscalation);
        Assert.False(policy.RequiresEmergencyApproval);
        Assert.False(policy.RequiresPostMaintenanceVerification);
    }

    [Theory]
    [InlineData(true, false, true, false, true, false)]
    [InlineData(false, true, false, true, false, true)]
    [InlineData(true, true, false, false, true, false)]
    [InlineData(false, false, true, true, false, true)]
    public void PreservesIndependentGovernanceDeclarations(
        bool amendment,
        bool earlyClosure,
        bool cancellation,
        bool overrun,
        bool emergency,
        bool verification)
    {
        var policy = Create(
            amendment,
            earlyClosure,
            cancellation,
            overrun,
            emergency,
            verification);

        Assert.Equal(amendment, policy.RequiresAmendmentApproval);
        Assert.Equal(earlyClosure, policy.RequiresEarlyClosureReview);
        Assert.Equal(cancellation, policy.RequiresCancellationReview);
        Assert.Equal(overrun, policy.RequiresOverrunEscalation);
        Assert.Equal(emergency, policy.RequiresEmergencyApproval);
        Assert.Equal(
            verification,
            policy.RequiresPostMaintenanceVerification);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationMaintenanceWindowPolicy);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyEightGetterOnlyProperties()
    {
        var type = typeof(OperationMaintenanceWindowPolicy);
        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(8, properties.Length);

        Assert.Equal(
            typeof(string),
            type.GetProperty("PolicyId")!.PropertyType);

        Assert.Equal(
            typeof(OperationAuthorizationConditions),
            type.GetProperty("Conditions")!.PropertyType);

        var booleanProperties = new[]
        {
            "RequiresAmendmentApproval",
            "RequiresEarlyClosureReview",
            "RequiresCancellationReview",
            "RequiresOverrunEscalation",
            "RequiresEmergencyApproval",
            "RequiresPostMaintenanceVerification"
        };

        foreach (var name in booleanProperties)
        {
            Assert.Equal(typeof(bool),
                type.GetProperty(name)!.PropertyType);
        }

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void ExposesExactlyOneEightParameterConstructor()
    {
        var type = typeof(OperationMaintenanceWindowPolicy);

        var constructor = Assert.Single(
            type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(8, parameters.Length);

        Assert.Equal("policyId", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);

        Assert.Equal("conditions", parameters[1].Name);
        Assert.Equal(
            typeof(OperationAuthorizationConditions),
            parameters[1].ParameterType);

        var expectedNames = new[]
        {
            "requiresAmendmentApproval",
            "requiresEarlyClosureReview",
            "requiresCancellationReview",
            "requiresOverrunEscalation",
            "requiresEmergencyApproval",
            "requiresPostMaintenanceVerification"
        };

        for (var i = 0; i < expectedNames.Length; i++)
        {
            Assert.Equal(expectedNames[i], parameters[i + 2].Name);
            Assert.Equal(typeof(bool), parameters[i + 2].ParameterType);
            Assert.False(parameters[i + 2].IsOptional);
        }

        Assert.All(parameters, parameter =>
            Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoOperationalInstanceMethods()
    {
        var methods = typeof(OperationMaintenanceWindowPolicy)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotExposePublicStaticOperations()
    {
        var methods = typeof(OperationMaintenanceWindowPolicy)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationMaintenanceWindowPolicy).GetInterfaces());
    }

    [Fact]
    public void IndependentPoliciesPreserveIndependentConditions()
    {
        var firstConditions = Conditions();
        var secondConditions = Conditions();

        var first = new OperationMaintenanceWindowPolicy(
            "policy-one",
            firstConditions,
            true, false, false, true, false, true);

        var second = new OperationMaintenanceWindowPolicy(
            "policy-two",
            secondConditions,
            false, true, true, false, true, false);

        Assert.Same(firstConditions, first.Conditions);
        Assert.Same(secondConditions, second.Conditions);
        Assert.NotSame(first.Conditions, second.Conditions);
    }
}

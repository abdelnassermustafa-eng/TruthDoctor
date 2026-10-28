using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationFlexibleSeparationOfDutiesPolicyTests
{
    [Fact]
    public void PreservesOriginalRequirementReference()
    {
        var requirement = CreateRequirement();

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(requirement);

        Assert.Same(requirement, policy.Requirement);
    }

    [Fact]
    public void PreservesApprovalChainThroughRequirement()
    {
        var requirement = CreateRequirement();

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(requirement);

        Assert.Same(
            requirement.Chain,
            policy.Requirement.Chain);
        Assert.Same(
            requirement.Chain.Request,
            policy.Requirement.Chain.Request);
    }

    [Fact]
    public void PreservesRequesterIdentityThroughRequirement()
    {
        var requirement = CreateRequirement();

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(requirement);

        Assert.Equal(
            requirement.Chain.Request.Requester,
            policy.Requirement.Chain.Request.Requester);
    }

    [Fact]
    public void RejectsNullRequirement()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationFlexibleSeparationOfDutiesPolicy(null!));
    }

    [Fact]
    public void DefaultsOverrideEscalationToNull()
    {
        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(
                CreateRequirement());

        Assert.Null(policy.OverrideEscalation);
        Assert.NotNull(policy.Requirement);
    }

    [Fact]
    public void PreservesProvidedOverrideEscalationReference()
    {
        var request = new OperationPolicyOverrideEscalation(
            overridePolicyId: "policy-1",
            overrideRequestReason: "Review requested");

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(
                CreateRequirement(),
                request);

        Assert.Same(request, policy.OverrideEscalation);
    }

    [Fact]
    public void PreservesOverrideAndEscalationRequestData()
    {
        var request = new OperationPolicyOverrideEscalation(
            overridePolicyId: "policy-ALPHA",
            overrideRequestReason: "Exception REVIEW",
            escalationRequestReason: "Security REVIEW");

        var requirement = CreateRequirement();

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(
                requirement,
                request);

        Assert.Same(requirement, policy.Requirement);
        Assert.Same(request, policy.OverrideEscalation);
        Assert.Equal(
            "policy-ALPHA",
            policy.OverrideEscalation!.OverridePolicyId);
        Assert.Equal(
            "Exception REVIEW",
            policy.OverrideEscalation.OverrideRequestReason);
        Assert.Equal(
            "Security REVIEW",
            policy.OverrideEscalation.EscalationRequestReason);
    }

    [Fact]
    public void DoesNotInventOverrideOrEscalationRequests()
    {
        var emptyRequest = new OperationPolicyOverrideEscalation();

        var policy =
            new OperationFlexibleSeparationOfDutiesPolicy(
                CreateRequirement(),
                emptyRequest);

        Assert.Same(emptyRequest, policy.OverrideEscalation);
        Assert.Null(policy.OverrideEscalation!.OverridePolicyId);
        Assert.Null(policy.OverrideEscalation.OverrideRequestReason);
        Assert.Null(policy.OverrideEscalation.EscalationRequestReason);
        Assert.NotNull(policy.Requirement);
    }

    [Fact]
    public void IndependentPoliciesPreserveTheirOwnRequirements()
    {
        var firstRequirement = CreateRequirement();
        var secondRequirement = CreateRequirement();

        var first =
            new OperationFlexibleSeparationOfDutiesPolicy(
                firstRequirement);

        var second =
            new OperationFlexibleSeparationOfDutiesPolicy(
                secondRequirement);

        Assert.Same(firstRequirement, first.Requirement);
        Assert.Same(secondRequirement, second.Requirement);
        Assert.NotSame(first.Requirement, second.Requirement);
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationFlexibleSeparationOfDutiesPolicy);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var type = typeof(OperationFlexibleSeparationOfDutiesPolicy);

        var properties = type.GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var requirement = Assert.Single(
            properties,
            property => property.Name == "Requirement");

        Assert.Equal(
            typeof(OperationSeparationOfDutiesRequirement),
            requirement.PropertyType);

        var overrideEscalation = Assert.Single(
            properties,
            property => property.Name == "OverrideEscalation");

        Assert.Equal(
            typeof(OperationPolicyOverrideEscalation),
            overrideEscalation.PropertyType);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void HasExactlyOnePublicConstructorWithOptionalOverride()
    {
        var type = typeof(OperationFlexibleSeparationOfDutiesPolicy);

        var constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);

        Assert.Equal("requirement", parameters[0].Name);
        Assert.Equal(
            typeof(OperationSeparationOfDutiesRequirement),
            parameters[0].ParameterType);
        Assert.False(parameters[0].IsOptional);

        Assert.Equal("overrideEscalation", parameters[1].Name);
        Assert.Equal(
            typeof(OperationPolicyOverrideEscalation),
            parameters[1].ParameterType);
        Assert.True(parameters[1].IsOptional);
        Assert.Null(parameters[1].DefaultValue);
    }

    [Fact]
    public void DoesNotExposeApprovalAuthorizationOrExecutionMethods()
    {
        var type = typeof(OperationFlexibleSeparationOfDutiesPolicy);

        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly);

        Assert.Equal(
            new[] { "get_OverrideEscalation", "get_Requirement" },
            methods.Select(method => method.Name).OrderBy(name => name));

        Assert.All(methods, method =>
            Assert.True(method.IsSpecialName));
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        var type = typeof(OperationFlexibleSeparationOfDutiesPolicy);

        Assert.Empty(type.GetInterfaces());
    }

    private static OperationSeparationOfDutiesRequirement
        CreateRequirement()
    {
        var requester = new OperationActorIdentity(
            OperationActorKind.Human,
            "requester-1");

        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-flexible-sod-1"),
            requester,
            "Independent review required");

        var stage = new OperationApprovalStage(
            "review",
            new OperationRoleIdentity("approver"));

        var chain = new OperationApprovalChain(
            request,
            new[] { stage });

        return new OperationSeparationOfDutiesRequirement(chain);
    }
}

using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationSeparationOfDutiesRequirementTests
{
    [Fact]
    public void PreservesOriginalApprovalChainReference()
    {
        var chain = CreateChain();

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Same(chain, requirement.Chain);
    }

    [Fact]
    public void PreservesNativeRequestThroughApprovalChain()
    {
        var chain = CreateChain();

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Same(chain.Request, requirement.Chain.Request);
    }

    [Fact]
    public void PreservesRequesterIdentityThroughChain()
    {
        var chain = CreateChain();

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Equal(
            chain.Request.Requester,
            requirement.Chain.Request.Requester);
    }

    [Fact]
    public void PreservesApprovalStagesThroughChain()
    {
        var chain = CreateChain();

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Same(chain.Stages, requirement.Chain.Stages);
    }

    [Fact]
    public void RejectsNullApprovalChain()
    {
        Assert.Throws<ArgumentNullException>(
            () => new OperationSeparationOfDutiesRequirement(null!));
    }

    [Fact]
    public void IndependentRequirementsPreserveTheirOwnChains()
    {
        var firstChain = CreateChain();
        var secondChain = CreateChain();

        var first =
            new OperationSeparationOfDutiesRequirement(firstChain);

        var second =
            new OperationSeparationOfDutiesRequirement(secondChain);

        Assert.Same(firstChain, first.Chain);
        Assert.Same(secondChain, second.Chain);
        Assert.NotSame(first.Chain, second.Chain);
    }

    [Fact]
    public void AllowsSameRoleAcrossDistinctApprovalStages()
    {
        var requester = new OperationActorIdentity(
            OperationActorKind.Human, "requester-1");

        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-sod-2"),
            requester,
            "Require independent review");

        var role = new OperationRoleIdentity("reviewer");

        var firstStage = new OperationApprovalStage(
            "stage-1", role);

        var secondStage = new OperationApprovalStage(
            "stage-2", role);

        var chain = new OperationApprovalChain(
            request, new[] { firstStage, secondStage });

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Same(firstStage, requirement.Chain.Stages[0]);
        Assert.Same(secondStage, requirement.Chain.Stages[1]);
    }

    [Fact]
    public void RequesterIdentityRetainsKindAndCaseSemantics()
    {
        var requester = new OperationActorIdentity(
            OperationActorKind.Human, "User-123");

        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-sod-3"),
            requester,
            "Require independent review");

        var chain = new OperationApprovalChain(
            request,
            new[]
            {
                new OperationApprovalStage(
                    "review",
                    new OperationRoleIdentity("approver"))
            });

        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Equal(requester, requirement.Chain.Request.Requester);

        Assert.NotEqual(
            requirement.Chain.Request.Requester,
            new OperationActorIdentity(
                OperationActorKind.Human, "user-123"));

        Assert.NotEqual(
            requirement.Chain.Request.Requester,
            new OperationActorIdentity(
                OperationActorKind.Service, "User-123"));
    }

    [Fact]
    public void IsPublicSealedReferenceType()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
    }

    [Fact]
    public void ExposesExactlyOneGetterOnlyProperty()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.DeclaredOnly);

        var property = Assert.Single(properties);

        Assert.Equal("Chain", property.Name);
        Assert.Equal(typeof(OperationApprovalChain), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void HasExactlyOnePublicConstructor()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        var constructor = Assert.Single(type.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.DeclaredOnly));

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("chain", parameter.Name);
        Assert.Equal(typeof(OperationApprovalChain), parameter.ParameterType);
    }

    [Fact]
    public void DoesNotExposeApprovalOrExecutionMethods()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        var methods = type.GetMethods(
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.DeclaredOnly);

        var getter = Assert.Single(methods);

        Assert.Equal("get_Chain", getter.Name);
        Assert.True(getter.IsSpecialName);
    }

    [Fact]
    public void DoesNotExposeAuthorizationOrPolicyProperties()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            properties,
            property =>
                property.Name.Contains(
                    "Authorized", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains(
                    "Approved", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains(
                    "Override", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains(
                    "Policy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DoesNotDeclareProviderIntegrationInterfaces()
    {
        var type = typeof(OperationSeparationOfDutiesRequirement);

        Assert.Empty(type.GetInterfaces());
    }

    private static OperationApprovalChain CreateChain()
    {
        var requester = new OperationActorIdentity(
            OperationActorKind.Human, "requester-1");

        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-sod-1"),
            requester,
            "Require independent review");

        var stage = new OperationApprovalStage(
            "review",
            new OperationRoleIdentity("approver"));

        return new OperationApprovalChain(
            request,
            new[] { stage });
    }
}

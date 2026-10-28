using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Regression guards for the declarative authorization boundary.
///
/// These tests inspect model behavior and public API shape.
/// They do not test an authorization evaluator, enforce access,
/// authenticate identities, or establish runtime bypass resistance.
/// </summary>
public sealed class OperationAuthorizationBypassTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-7));

    private static OperationActorIdentity Requester() =>
        new(OperationActorKind.Human, "requester-1");

    private static OperationRoleIdentity ApproverRole() =>
        new("approver");

    private static OperationRoleAssignment Assignment() =>
        new(Requester(), ApproverRole());

    private static OperationApprovalChain Chain()
    {
        var request = new OperationNativeApprovalRequest(
            new OperationCorrelationId("operation-security-1"),
            Requester(),
            "Security boundary regression");

        var stage = new OperationApprovalStage(
            "stage-1", ApproverRole());

        return new OperationApprovalChain(
            request, new[] { stage });
    }

    private static OperationApprovalLifecycle Lifecycle(
        OperationApprovalLifecycleStatus status)
    {
        DateTimeOffset? expiresAt =
            status == OperationApprovalLifecycleStatus.Expired
                ? Observed.AddDays(-1)
                : null;

        DateTimeOffset? revokedAt =
            status == OperationApprovalLifecycleStatus.Revoked
                ? Observed.AddMinutes(-1)
                : null;

        OperationActorIdentity? revokedBy =
            status == OperationApprovalLifecycleStatus.Revoked
                ? new OperationActorIdentity(
                    OperationActorKind.Human, "revoker-1")
                : null;

        return new OperationApprovalLifecycle(
            "approval-1",
            Chain(),
            status,
            Observed,
            expiresAt,
            revokedAt,
            revokedBy,
            "Reported lifecycle state");
    }

    private static OperationExternalApprovalAdapter External(
        OperationExternalApprovalStatus status)
    {
        return new OperationExternalApprovalAdapter(
            "adapter-1",
            Chain(),
            "approval-system-1",
            "external-request-1",
            status,
            Observed,
            "Caller-reported external status");
    }

    private static void AssertNoAuthoritySurface(Type type)
    {
        var forbidden = new[]
        {
            "IsAuthorized",
            "CanExecute",
            "HasPermission",
            "IsApproved",
            "IsAdmin",
            "IsAdministrator",
            "Approve",
            "Authorize",
            "Execute",
            "BypassAuthorization",
            "SkipAuthorization",
            "OverrideGranted"
        };

        foreach (var name in forbidden)
        {
            Assert.Null(type.GetProperty(
                name,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.FlattenHierarchy));

            Assert.DoesNotContain(
                type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.FlattenHierarchy),
                method => method.Name == name);
        }
    }

    [Fact]
    public void T01_AdministratorLikeRoleNameDoesNotGrantAuthority()
    {
        var role = new OperationRoleIdentity("administrator");

        Assert.Equal("administrator", role.Value);
        AssertNoAuthoritySurface(typeof(OperationRoleIdentity));
    }

    [Fact]
    public void T02_ActorIdentityDoesNotAuthenticateActor()
    {
        var actor = Requester();

        Assert.Equal(OperationActorKind.Human, actor.Kind);
        Assert.Equal("requester-1", actor.Value);
        AssertNoAuthoritySurface(typeof(OperationActorIdentity));
    }

    [Fact]
    public void T03_RoleDefinitionDoesNotGrantEffectiveAccess()
    {
        var role = new OperationRoleIdentity("administrator");
        var permission = new OperationPermissionIdentity(
            "operations.execute");

        var definition = new OperationRoleDefinition(
            role, new[] { permission });

        Assert.Equal(role, definition.Role);
        Assert.Contains(permission, definition.Permissions);
        AssertNoAuthoritySurface(typeof(OperationRoleDefinition));
    }

    [Fact]
    public void T04_RoleAssignmentIsNotExecutionPermission()
    {
        var assignment = Assignment();

        Assert.Equal(Requester(), assignment.Actor);
        Assert.Equal(ApproverRole(), assignment.Role);
        AssertNoAuthoritySurface(typeof(OperationRoleAssignment));
    }

    [Fact]
    public void T05_ScopedAssignmentDoesNotEnforceScope()
    {
        var type = typeof(OperationScopedRoleAssignment);

        Assert.Equal(
            typeof(OperationRoleAssignment),
            type.GetProperty("Assignment")?.PropertyType);
        Assert.Equal(
            typeof(OperationResourceScope),
            type.GetProperty("Scope")?.PropertyType);

        AssertNoAuthoritySurface(type);
    }

    [Fact]
    public void T06_ConditionalAssignmentDoesNotEvaluateConditions()
    {
        var type = typeof(OperationConditionalRoleAssignment);

        Assert.Equal(
            typeof(OperationRoleAssignment),
            type.GetProperty("Assignment")?.PropertyType);
        Assert.Equal(
            typeof(OperationAuthorizationConditions),
            type.GetProperty("Conditions")?.PropertyType);

        AssertNoAuthoritySurface(type);
    }

    [Fact]
    public void T07_EmptyConditionsCannotBeTreatedAsAuthorization()
    {
        var type = typeof(OperationAuthorizationConditions);

        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        AssertNoAuthoritySurface(type);
        AssertNoAuthoritySurface(
            typeof(OperationAuthorizationCondition));
    }

    [Fact]
    public void T08_PermissionIdentityDoesNotGrantPermission()
    {
        var permission = new OperationPermissionIdentity(
            "operations.execute");

        Assert.Equal("operations.execute", permission.Value);
        AssertNoAuthoritySurface(
            typeof(OperationPermissionIdentity));
    }

    [Fact]
    public void T09_ApprovalChainDoesNotImplyCompletedApproval()
    {
        var chain = Chain();

        Assert.Single(chain.Stages);
        Assert.Equal(
            "operation-security-1",
            chain.Request.Operation.Value);
        AssertNoAuthoritySurface(typeof(OperationApprovalChain));
    }

    [Fact]
    public void T10_RequesterCannotBecomeVerifiedApproverByIdentity()
    {
        var chain = Chain();

        Assert.Equal(Requester(), chain.Request.Requester);
        Assert.Equal(
            ApproverRole(),
            chain.Stages[0].ApproverRole);

        AssertNoAuthoritySurface(
            typeof(OperationNativeApprovalRequest));
        AssertNoAuthoritySurface(typeof(OperationApprovalStage));
    }

    [Fact]
    public void T11_SeparationRequirementDoesNotEnforceItself()
    {
        var chain = Chain();
        var requirement =
            new OperationSeparationOfDutiesRequirement(chain);

        Assert.Same(chain, requirement.Chain);
        AssertNoAuthoritySurface(
            typeof(OperationSeparationOfDutiesRequirement));
    }

    [Fact]
    public void T12_OverrideRequestIsNotApprovedException()
    {
        var request = new OperationPolicyOverrideEscalation(
            overridePolicyId: "policy-1",
            overrideRequestReason: "Request review",
            escalationRequestReason: "Escalate for review");

        Assert.Equal("policy-1", request.OverridePolicyId);
        Assert.Equal(
            "Request review",
            request.OverrideRequestReason);

        AssertNoAuthoritySurface(
            typeof(OperationPolicyOverrideEscalation));
    }

    [Fact]
    public void T13_FlexiblePolicyPreservesUnderlyingRequirement()
    {
        var requirement =
            new OperationSeparationOfDutiesRequirement(Chain());

        var overrideRequest =
            new OperationPolicyOverrideEscalation(
                overridePolicyId: "policy-1");

        var policy = new OperationFlexibleSeparationOfDutiesPolicy(
            requirement, overrideRequest);

        Assert.Same(requirement, policy.Requirement);
        Assert.Same(overrideRequest, policy.OverrideEscalation);

        AssertNoAuthoritySurface(
            typeof(OperationFlexibleSeparationOfDutiesPolicy));
    }

    [Fact]
    public void T14_ReportedActiveIsNotAuthorization()
    {
        var lifecycle = Lifecycle(
            OperationApprovalLifecycleStatus.ReportedActive);

        Assert.Equal(
            OperationApprovalLifecycleStatus.ReportedActive,
            lifecycle.Status);

        AssertNoAuthoritySurface(typeof(OperationApprovalLifecycle));
    }

    [Fact]
    public void T15_ExpiredStatusRemainsExplicitlyExpired()
    {
        var lifecycle = Lifecycle(
            OperationApprovalLifecycleStatus.Expired);

        Assert.Equal(
            OperationApprovalLifecycleStatus.Expired,
            lifecycle.Status);
        Assert.NotNull(lifecycle.ExpiresAt);

        AssertNoAuthoritySurface(typeof(OperationApprovalLifecycle));
    }

    [Fact]
    public void T16_RevokedStatusDoesNotRestoreAuthorization()
    {
        var lifecycle = Lifecycle(
            OperationApprovalLifecycleStatus.Revoked);

        Assert.Equal(
            OperationApprovalLifecycleStatus.Revoked,
            lifecycle.Status);
        Assert.NotNull(lifecycle.RevokedAt);
        Assert.NotNull(lifecycle.RevokedBy);

        AssertNoAuthoritySurface(typeof(OperationApprovalLifecycle));
    }

    [Fact]
    public void T17_ReportedExternalApprovalIsNotAuthorization()
    {
        var adapter = External(
            OperationExternalApprovalStatus.ReportedApproved);

        Assert.Equal(
            OperationExternalApprovalStatus.ReportedApproved,
            adapter.Status);

        AssertNoAuthoritySurface(
            typeof(OperationExternalApprovalAdapter));
    }

    [Fact]
    public void T18_UnavailableExternalStatusIsNotApproved()
    {
        var adapter = External(
            OperationExternalApprovalStatus.Unavailable);

        Assert.Equal(
            OperationExternalApprovalStatus.Unavailable,
            adapter.Status);

        AssertNoAuthoritySurface(
            typeof(OperationExternalApprovalAdapter));
    }

    [Fact]
    public void T19_RevalidationFindingIsNotExecutionPermission()
    {
        var type = typeof(OperationPreExecutionRevalidation);

        Assert.Equal(
            typeof(string),
            type.GetProperty("Finding")?.PropertyType);
        Assert.Equal(
            typeof(string),
            type.GetProperty("Reason")?.PropertyType);

        AssertNoAuthoritySurface(type);
    }

    [Fact]
    public void T20_DeclarativeContractsExposeNoAuthorityMethods()
    {
        Type[] contracts =
        {
            typeof(OperationActorIdentity),
            typeof(OperationRoleIdentity),
            typeof(OperationPermissionIdentity),
            typeof(OperationRoleDefinition),
            typeof(OperationRoleAssignment),
            typeof(OperationScopedRoleAssignment),
            typeof(OperationConditionalRoleAssignment),
            typeof(OperationAuthorizationCondition),
            typeof(OperationAuthorizationConditions),
            typeof(OperationNativeApprovalRequest),
            typeof(OperationApprovalStage),
            typeof(OperationApprovalChain),
            typeof(OperationSeparationOfDutiesRequirement),
            typeof(OperationPolicyOverrideEscalation),
            typeof(OperationFlexibleSeparationOfDutiesPolicy),
            typeof(OperationApprovalLifecycle),
            typeof(OperationExternalApprovalAdapter),
            typeof(OperationPreExecutionRevalidation)
        };

        Assert.Equal(18, contracts.Length);

        foreach (var type in contracts)
        {
            AssertNoAuthoritySurface(type);
        }
    }
}

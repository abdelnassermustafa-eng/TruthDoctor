using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationExpectedOutcomeDefinitionTests
{
    private static OperationPlanReview Plan(
        string capability = "compute.instance.start")
    {
        var identity = new UniversalCapabilityIdentity(capability);

        return new OperationPlanReview(
            "Review expected resource state",
            new OperationIntent(identity, "Start resource"),
            new OperationTarget(
                "provider", "account", "location",
                "compute", "instance", "resource-01"),
            new OperationDesiredOutcome(new[]
            {
                new OperationOutcomeCondition(
                    "resource.state", "running")
            }),
            new OperationPlannedSteps(new[]
            {
                new OperationPlannedStep(
                    "step-01", identity, "Start instance")
            }));
    }

    private static CapabilityVerificationMetadata Verification(
        CapabilityVerificationStatus status =
            CapabilityVerificationStatus.Unknown,
        string capability = "compute.instance.start")
    {
        return new CapabilityVerificationMetadata(
            new UniversalCapabilityIdentity(capability),
            status);
    }

    [Fact]
    public void PreservesOriginalPlanAndVerificationReferences()
    {
        var plan = Plan();
        var verification = Verification();

        var definition = new OperationExpectedOutcomeDefinition(
            plan, verification);

        Assert.Same(plan, definition.Plan);
        Assert.Same(verification, definition.Verification);
        Assert.Same(plan.DesiredOutcome,
            definition.Plan.DesiredOutcome);
        Assert.Same(plan.Target, definition.Plan.Target);
    }

    [Theory]
    [InlineData(CapabilityVerificationStatus.Unknown)]
    [InlineData(CapabilityVerificationStatus.Unsupported)]
    [InlineData(CapabilityVerificationStatus.Supported)]
    public void PreservesAllDeclaredVerificationSupportStates(
        CapabilityVerificationStatus status)
    {
        var definition = new OperationExpectedOutcomeDefinition(
            Plan(), Verification(status));

        Assert.Equal(status, definition.Verification.Status);
        Assert.Equal("running",
            definition.Plan.DesiredOutcome.Conditions[0].ExpectedValue);
    }

    [Fact]
    public void RejectsNullPlan()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationExpectedOutcomeDefinition(
                null!, Verification()));
    }

    [Fact]
    public void RejectsNullVerificationMetadata()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationExpectedOutcomeDefinition(
                Plan(), null!));
    }

    [Fact]
    public void RejectsMismatchedOperationCapability()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationExpectedOutcomeDefinition(
                Plan("compute.instance.start"),
                Verification(
                    CapabilityVerificationStatus.Supported,
                    "compute.instance.stop")));
    }

    [Fact]
    public void IsASealedReadOnlyDeclaration()
    {
        var type = typeof(OperationExpectedOutcomeDefinition);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(2, properties.Length);

        Assert.Equal(
            new[] { "Plan", "Verification" },
            properties.Select(x => x.Name).OrderBy(x => x).ToArray());

        Assert.Equal(typeof(OperationPlanReview),
            type.GetProperty("Plan")!.PropertyType);

        Assert.Equal(typeof(CapabilityVerificationMetadata),
            type.GetProperty("Verification")!.PropertyType);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        var constructor = Assert.Single(type.GetConstructors());
        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(OperationPlanReview),
            parameters[0].ParameterType);
        Assert.Equal(typeof(CapabilityVerificationMetadata),
            parameters[1].ParameterType);
    }

    [Fact]
    public void DefinitionDoesNotExposeOperationalSuccessOrExecutionState()
    {
        var properties = typeof(OperationExpectedOutcomeDefinition)
            .GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(properties, property =>
            property.Name is "IsVerified" or "IsSuccessful"
                or "IsApproved" or "IsAuthorized"
                or "ExecutionStatus" or "VerificationResult");
    }
}

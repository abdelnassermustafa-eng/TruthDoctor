using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPlanReviewTests
{
    private static OperationIntent Intent() =>
        new(new UniversalCapabilityIdentity("compute.instance.start"),
            "Start the instance");

    private static OperationTarget Target() =>
        new("provider", "account", "location", "compute",
            "instance", "resource-01");

    private static OperationDesiredOutcome Outcome() =>
        new(new[]
        {
            new OperationOutcomeCondition("state", "running")
        });

    private static OperationPlannedSteps Steps() =>
        new(new[]
        {
            new OperationPlannedStep(
                "step-a",
                new UniversalCapabilityIdentity("compute.instance.start"),
                "Start instance")
        });

    private static OperationPlanReview Create(
        string summary = "Review proposed start",
        OperationIntent? intent = null,
        OperationTarget? target = null,
        OperationDesiredOutcome? desiredOutcome = null,
        OperationPlannedSteps? plannedSteps = null) =>
        new(summary, intent ?? Intent(), target ?? Target(),
            desiredOutcome ?? Outcome(), plannedSteps ?? Steps());

    [Fact]
    public void TypeIsPublicSealedAndInPlatformNamespace()
    {
        var type = typeof(OperationPlanReview);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void HasExactlyTenPublicGetterOnlyProperties()
    {
        var properties = typeof(OperationPlanReview)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(10, properties.Length);
        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        var expected = new Dictionary<string, Type>
        {
            ["Summary"] = typeof(string),
            ["Intent"] = typeof(OperationIntent),
            ["Target"] = typeof(OperationTarget),
            ["DesiredOutcome"] = typeof(OperationDesiredOutcome),
            ["PlannedSteps"] = typeof(OperationPlannedSteps),
            ["Preconditions"] = typeof(OperationPreconditions),
            ["Dependencies"] = typeof(OperationStepDependencies),
            ["RecoveryPlan"] = typeof(OperationRecoveryPlan),
            ["EstimatedImpact"] = typeof(OperationEstimatedImpact),
            ["EvidenceRequirements"] = typeof(OperationEvidenceRequirements)
        };

        Assert.Equal(
            expected.Keys.OrderBy(x => x),
            properties.Select(x => x.Name).OrderBy(x => x));

        foreach (var property in properties)
        {
            Assert.Equal(expected[property.Name], property.PropertyType);
        }
    }

    [Fact]
    public void ConstructorHasExactParameterNamesTypesAndDefaults()
    {
        var constructor = Assert.Single(
            typeof(OperationPlanReview).GetConstructors());

        var parameters = constructor.GetParameters();
        Assert.Equal(10, parameters.Length);

        var names = new[]
        {
            "summary", "intent", "target", "desiredOutcome",
            "plannedSteps", "preconditions", "dependencies",
            "recoveryPlan", "estimatedImpact", "evidenceRequirements"
        };

        var types = new[]
        {
            typeof(string), typeof(OperationIntent),
            typeof(OperationTarget), typeof(OperationDesiredOutcome),
            typeof(OperationPlannedSteps), typeof(OperationPreconditions),
            typeof(OperationStepDependencies),
            typeof(OperationRecoveryPlan),
            typeof(OperationEstimatedImpact),
            typeof(OperationEvidenceRequirements)
        };

        Assert.Equal(names, parameters.Select(p => p.Name));
        Assert.Equal(types, parameters.Select(p => p.ParameterType));

        Assert.All(parameters.Take(5),
            parameter => Assert.False(parameter.IsOptional));

        Assert.All(parameters.Skip(5), parameter =>
        {
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void RequiredContractsArePreservedByReference()
    {
        var intent = Intent();
        var target = Target();
        var outcome = Outcome();
        var steps = Steps();

        var review = new OperationPlanReview(
            "Review", intent, target, outcome, steps);

        Assert.Same(intent, review.Intent);
        Assert.Same(target, review.Target);
        Assert.Same(outcome, review.DesiredOutcome);
        Assert.Same(steps, review.PlannedSteps);
    }

    [Fact]
    public void SummaryIsTrimmedAndCasePreserved()
    {
        var review = Create("  Review Proposed Start  ");
        Assert.Equal("Review Proposed Start", review.Summary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankSummaryIsRejected(string? summary)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(summary!));
    }

    [Theory]
    [InlineData("Review\nNow")]
    [InlineData("Review\rNow")]
    [InlineData("Review\tNow")]
    [InlineData("Review\0Now")]
    public void SummaryRejectsControlCharacters(string value)
    {
        Assert.Throws<ArgumentException>(() => Create(value));
    }

    [Fact]
    public void NullIntentIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPlanReview(
                "Review", null!, Target(), Outcome(), Steps()));
    }

    [Fact]
    public void NullTargetIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPlanReview(
                "Review", Intent(), null!, Outcome(), Steps()));
    }

    [Fact]
    public void NullDesiredOutcomeIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPlanReview(
                "Review", Intent(), Target(), null!, Steps()));
    }

    [Fact]
    public void NullPlannedStepsIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationPlanReview(
                "Review", Intent(), Target(), Outcome(), null!));
    }

    [Fact]
    public void OptionalDeclarationsDefaultToNull()
    {
        var review = Create();

        Assert.Null(review.Preconditions);
        Assert.Null(review.Dependencies);
        Assert.Null(review.RecoveryPlan);
        Assert.Null(review.EstimatedImpact);
        Assert.Null(review.EvidenceRequirements);
    }

    [Fact]
    public void OptionalDeclarationsPreserveOriginalReferences()
    {
        var preconditions = new OperationPreconditions(
            Array.Empty<OperationPrecondition>());
        var dependencies = new OperationStepDependencies(
            Array.Empty<OperationStepDependency>());
        var recovery = new OperationRecoveryPlan(
            Array.Empty<OperationStepRecovery>());
        var impact = new OperationEstimatedImpact(
            Array.Empty<OperationStepImpact>());
        var evidence = new OperationEvidenceRequirements(
            Array.Empty<OperationEvidenceRequirement>());

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            preconditions, dependencies, recovery, impact, evidence);

        Assert.Same(preconditions, review.Preconditions);
        Assert.Same(dependencies, review.Dependencies);
        Assert.Same(recovery, review.RecoveryPlan);
        Assert.Same(impact, review.EstimatedImpact);
        Assert.Same(evidence, review.EvidenceRequirements);
    }

    [Fact]
    public void PresentEmptyDeclarationsRemainDistinctFromNull()
    {
        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            preconditions: new OperationPreconditions(
                Array.Empty<OperationPrecondition>()),
            dependencies: new OperationStepDependencies(
                Array.Empty<OperationStepDependency>()),
            recoveryPlan: new OperationRecoveryPlan(
                Array.Empty<OperationStepRecovery>()),
            estimatedImpact: new OperationEstimatedImpact(
                Array.Empty<OperationStepImpact>()),
            evidenceRequirements: new OperationEvidenceRequirements(
                Array.Empty<OperationEvidenceRequirement>()));

        Assert.Empty(review.Preconditions!.Conditions);
        Assert.Empty(review.Dependencies!.Dependencies);
        Assert.Empty(review.RecoveryPlan!.Recoveries);
        Assert.Empty(review.EstimatedImpact!.Impacts);
        Assert.Empty(review.EvidenceRequirements!.Requirements);
    }

    [Fact]
    public void DoesNotAutomaticallyValidateUnknownDependencies()
    {
        var dependencies = new OperationStepDependencies(
            new[]
            {
                new OperationStepDependency("step-a", "unknown-step")
            });

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            dependencies: dependencies);

        Assert.Same(dependencies, review.Dependencies);
    }

    [Fact]
    public void DoesNotAutomaticallyValidateDependencyCycles()
    {
        var steps = new OperationPlannedSteps(new[]
        {
            new OperationPlannedStep(
                "step-a",
                new UniversalCapabilityIdentity("compute.instance.start"),
                "First"),
            new OperationPlannedStep(
                "step-b",
                new UniversalCapabilityIdentity("compute.instance.start"),
                "Second")
        });

        var dependencies = new OperationStepDependencies(new[]
        {
            new OperationStepDependency("step-a", "step-b"),
            new OperationStepDependency("step-b", "step-a")
        });

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), steps,
            dependencies: dependencies);

        Assert.Same(steps, review.PlannedSteps);
        Assert.Same(dependencies, review.Dependencies);
    }

    [Fact]
    public void DoesNotRequireRecoveryImpactOrEvidenceCoverage()
    {
        var review = Create();
        Assert.Single(review.PlannedSteps.Steps);
        Assert.Null(review.RecoveryPlan);
        Assert.Null(review.EstimatedImpact);
        Assert.Null(review.EvidenceRequirements);
    }

    [Fact]
    public void DoesNotValidateRecoveryStepReferences()
    {
        var recovery = new OperationRecoveryPlan(new[]
        {
            new OperationStepRecovery(
                "unknown-step",
                OperationRecoveryStatus.Unknown,
                OperationRecoveryKind.None)
        });

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            recoveryPlan: recovery);

        Assert.Same(recovery, review.RecoveryPlan);
    }

    [Fact]
    public void DoesNotValidateImpactStepReferences()
    {
        var impact = new OperationEstimatedImpact(new[]
        {
            new OperationStepImpact(
                "unknown-step",
                OperationImpactSeverity.Unknown,
                OperationImpactConfidence.Unknown)
        });

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            estimatedImpact: impact);

        Assert.Same(impact, review.EstimatedImpact);
    }

    [Fact]
    public void DoesNotValidateEvidenceStepReferences()
    {
        var evidence = new OperationEvidenceRequirements(new[]
        {
            new OperationEvidenceRequirement(
                "requirement-a", "unknown-step",
                "observation", "Record resulting state")
        });

        var review = new OperationPlanReview(
            "Review", Intent(), Target(), Outcome(), Steps(),
            evidenceRequirements: evidence);

        Assert.Same(evidence, review.EvidenceRequirements);
    }

    [Fact]
    public void DoesNotInferApprovalOrExecutionState()
    {
        var properties = typeof(OperationPlanReview)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(properties,
            property => property.Name == "IsApproved");
        Assert.DoesNotContain(properties,
            property => property.Name == "IsAuthorized");
        Assert.DoesNotContain(properties,
            property => property.Name == "ExecutionStatus");
        Assert.DoesNotContain(properties,
            property => property.Name == "IsReady");
    }
}

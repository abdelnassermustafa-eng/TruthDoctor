using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationDependencyOrderingTests
{
    private static OperationPlannedStep Step(string id) =>
        new(id, new UniversalCapabilityIdentity("compute.instance.start"),
            "Planned operation " + id);

    private static OperationPlannedSteps Plan(params string[] ids)
    {
        var steps = new List<OperationPlannedStep>();

        foreach (var id in ids)
        {
            steps.Add(Step(id));
        }

        return new OperationPlannedSteps(steps);
    }

    private static OperationStepDependencies Dependencies(
        params (string StepId, string PrerequisiteStepId)[] edges)
    {
        var dependencies = new List<OperationStepDependency>();

        foreach (var edge in edges)
        {
            dependencies.Add(new OperationStepDependency(
                edge.StepId, edge.PrerequisiteStepId));
        }

        return new OperationStepDependencies(dependencies);
    }

    private static void AssertOrder(
        IReadOnlyList<OperationPlannedStep> result,
        params string[] expected)
    {
        Assert.Equal(expected.Length, result.Count);

        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index], result[index].StepId);
        }
    }

    [Fact]
    public void NoDependenciesPreservesDeclarationOrder()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-c", "step-a", "step-b"),
            Dependencies());

        AssertOrder(result, "step-c", "step-a", "step-b");
    }

    [Fact]
    public void OrdersChainPrerequisiteFirst()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-c", "step-b", "step-a"),
            Dependencies(
                ("step-c", "step-b"),
                ("step-b", "step-a")));

        AssertOrder(result, "step-a", "step-b", "step-c");
    }

    [Fact]
    public void OrdersBranchAfterBothPrerequisites()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-c", "step-b", "step-a"),
            Dependencies(
                ("step-c", "step-a"),
                ("step-c", "step-b")));

        AssertOrder(result, "step-b", "step-a", "step-c");
    }

    [Fact]
    public void EdgeDeclarationOrderDoesNotAffectResult()
    {
        var steps = Plan("step-c", "step-b", "step-a");

        var forward = OperationDependencyOrdering.Order(
            steps,
            Dependencies(
                ("step-c", "step-a"),
                ("step-c", "step-b")));

        var reversed = OperationDependencyOrdering.Order(
            steps,
            Dependencies(
                ("step-c", "step-b"),
                ("step-c", "step-a")));

        AssertOrder(forward, "step-b", "step-a", "step-c");
        AssertOrder(reversed, "step-b", "step-a", "step-c");
    }

    [Fact]
    public void UsesDeclarationOrderForNewlyAvailableSteps()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-c", "step-b", "step-a", "step-d"),
            Dependencies(("step-c", "step-a")));

        AssertOrder(
            result, "step-b", "step-a", "step-c", "step-d");
    }

    [Fact]
    public void EarliestEligibleStepWinsAfterPrerequisiteCompletion()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-c", "step-b", "step-a"),
            Dependencies(("step-c", "step-a")));

        AssertOrder(result, "step-b", "step-a", "step-c");
    }

    [Fact]
    public void ResolvesReferencesIgnoringCase()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("Step-C", "Step-A"),
            Dependencies(("step-c", "STEP-A")));

        AssertOrder(result, "Step-A", "Step-C");
    }

    [Fact]
    public void RejectsUnknownPrerequisite()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-a", "step-b"),
                Dependencies(("step-b", "missing"))));
    }

    [Fact]
    public void RejectsUnknownDependent()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-a", "step-b"),
                Dependencies(("missing", "step-a"))));
    }

    [Fact]
    public void RejectsTwoStepCycle()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-a", "step-b"),
                Dependencies(
                    ("step-a", "step-b"),
                    ("step-b", "step-a"))));
    }

    [Fact]
    public void RejectsLongerCycle()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-a", "step-b", "step-c"),
                Dependencies(
                    ("step-a", "step-b"),
                    ("step-b", "step-c"),
                    ("step-c", "step-a"))));
    }

    [Fact]
    public void RejectsCycleEvenWhenIndependentStepsExist()
    {
        Assert.Throws<ArgumentException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-free", "step-a", "step-b"),
                Dependencies(
                    ("step-a", "step-b"),
                    ("step-b", "step-a"))));
    }

    [Fact]
    public void RejectsNullSteps()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationDependencyOrdering.Order(
                null!, Dependencies()));
    }

    [Fact]
    public void RejectsNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationDependencyOrdering.Order(
                Plan("step-a"), null!));
    }

    [Fact]
    public void PreservesOriginalStepInstances()
    {
        var first = Step("step-a");
        var second = Step("step-b");
        var steps = new OperationPlannedSteps(
            new[] { second, first });

        var result = OperationDependencyOrdering.Order(
            steps, Dependencies(("step-b", "step-a")));

        Assert.Same(first, result[0]);
        Assert.Same(second, result[1]);
        Assert.Same(second, steps.Steps[0]);
        Assert.Same(first, steps.Steps[1]);
    }

    [Fact]
    public void ReturnsReadOnlyCollection()
    {
        var result = OperationDependencyOrdering.Order(
            Plan("step-a", "step-b"), Dependencies());

        var list = Assert.IsAssignableFrom<
            IList<OperationPlannedStep>>(result);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Step("replacement"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Step("additional")));
    }

    [Fact]
    public void DoesNotMutateDependencyDeclarationOrder()
    {
        var edges = Dependencies(
            ("step-c", "step-a"),
            ("step-c", "step-b"));

        OperationDependencyOrdering.Order(
            Plan("step-c", "step-b", "step-a"), edges);

        Assert.Equal("step-a", edges.Dependencies[0].PrerequisiteStepId);
        Assert.Equal("step-b", edges.Dependencies[1].PrerequisiteStepId);
    }

    [Fact]
    public void ExposesExactlyOnePublicOrderingMethod()
    {
        var type = typeof(OperationDependencyOrdering);

        Assert.True(type.IsAbstract && type.IsSealed);

        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly);

        var method = Assert.Single(methods);

        Assert.Equal("Order", method.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationPlannedStep>),
            method.ReturnType);

        var parameters = method.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("steps", parameters[0].Name);
        Assert.Equal(
            typeof(OperationPlannedSteps),
            parameters[0].ParameterType);
        Assert.Equal("dependencies", parameters[1].Name);
        Assert.Equal(
            typeof(OperationStepDependencies),
            parameters[1].ParameterType);
    }
}

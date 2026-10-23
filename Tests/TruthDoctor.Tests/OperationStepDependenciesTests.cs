using System;
using System.Collections.Generic;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationStepDependenciesTests
{
    private static OperationStepDependency Edge(
        string stepId, string prerequisiteStepId)
    {
        return new OperationStepDependency(
            stepId, prerequisiteStepId);
    }

    [Fact]
    public void PreservesDeclaredDependencyOrder()
    {
        var first = Edge("step-3", "step-1");
        var second = Edge("step-3", "step-2");

        var dependencies = new OperationStepDependencies(
            new[] { first, second });

        Assert.Equal(2, dependencies.Dependencies.Count);
        Assert.Same(first, dependencies.Dependencies[0]);
        Assert.Same(second, dependencies.Dependencies[1]);
    }

    [Fact]
    public void RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationStepDependencies(null!));
    }

    [Fact]
    public void AllowsEmptyCollection()
    {
        var dependencies = new OperationStepDependencies(
            Array.Empty<OperationStepDependency>());

        Assert.Empty(dependencies.Dependencies);
    }

    [Fact]
    public void RejectsNullEntry()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependencies(
                new OperationStepDependency[]
                {
                    Edge("step-2", "step-1"),
                    null!
                }));
    }

    [Fact]
    public void RejectsDuplicateDirectedEdges()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependencies(
                new[]
                {
                    Edge("step-3", "step-1"),
                    Edge("step-3", "step-1")
                }));
    }

    [Fact]
    public void RejectsDuplicateEdgesIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependencies(
                new[]
                {
                    Edge("Step-C", "Step-A"),
                    Edge("step-c", "step-a")
                }));
    }

    [Fact]
    public void RejectsDuplicateEdgesAfterNormalization()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationStepDependencies(
                new[]
                {
                    Edge(" step-3 ", "step-1"),
                    Edge("step-3", " step-1 ")
                }));
    }

    [Fact]
    public void TreatsReversedEdgesAsDistinct()
    {
        var dependencies = new OperationStepDependencies(
            new[]
            {
                Edge("step-2", "step-1"),
                Edge("step-1", "step-2")
            });

        Assert.Equal(2, dependencies.Dependencies.Count);
    }

    [Fact]
    public void SnapshotsInputAndRejectsCollectionMutation()
    {
        var source = new List<OperationStepDependency>
        {
            Edge("step-3", "step-1")
        };

        var dependencies = new OperationStepDependencies(source);

        source.Clear();
        source.Add(Edge("step-4", "step-2"));

        Assert.Single(dependencies.Dependencies);
        Assert.Equal(
            "step-3", dependencies.Dependencies[0].StepId);

        var list = Assert.IsAssignableFrom<
            IList<OperationStepDependency>>(
                dependencies.Dependencies);

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Edge("step-5", "step-1"));

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Edge("step-5", "step-2")));
    }

    [Fact]
    public void ExposesExactlyOneReadOnlyPropertyAndConstructor()
    {
        var type = typeof(OperationStepDependencies);

        Assert.True(type.IsSealed);

        var property = Assert.Single(type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal("Dependencies", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationStepDependency>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);

        var constructor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("dependencies", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationStepDependency>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }
}

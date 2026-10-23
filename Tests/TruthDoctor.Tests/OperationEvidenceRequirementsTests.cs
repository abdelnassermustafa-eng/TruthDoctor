using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationEvidenceRequirementsTests
{
    private static OperationEvidenceRequirement Requirement(
        string requirementId,
        string stepId = "step-a",
        bool isMandatory = true)
    {
        return new OperationEvidenceRequirement(
            requirementId,
            stepId,
            "state-observation",
            "Observe resource state",
            isMandatory);
    }

    [Fact]
    public void TypeIsPublicAndSealed()
    {
        Assert.True(typeof(OperationEvidenceRequirements).IsPublic);
        Assert.True(typeof(OperationEvidenceRequirements).IsSealed);
    }

    [Fact]
    public void OnePublicGetterOnlyCollectionProperty()
    {
        var property = Assert.Single(
            typeof(OperationEvidenceRequirements)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance));

        Assert.Equal("Requirements", property.Name);
        Assert.Equal(
            typeof(IReadOnlyList<OperationEvidenceRequirement>),
            property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void OnePublicConstructorAcceptsEnumerable()
    {
        var constructor = Assert.Single(
            typeof(OperationEvidenceRequirements).GetConstructors());

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal("requirements", parameter.Name);
        Assert.Equal(
            typeof(IEnumerable<OperationEvidenceRequirement>),
            parameter.ParameterType);
        Assert.False(parameter.IsOptional);
    }

    [Fact]
    public void NullCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationEvidenceRequirements(null!));
    }

    [Fact]
    public void NullEntryIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEvidenceRequirements(
                new OperationEvidenceRequirement[]
                {
                    Requirement("one"),
                    null!
                }));
    }

    [Fact]
    public void EmptyCollectionIsAllowed()
    {
        var requirements = new OperationEvidenceRequirements(
            Array.Empty<OperationEvidenceRequirement>());

        Assert.Empty(requirements.Requirements);
    }

    [Fact]
    public void SingleRequirementIsPreservedByReference()
    {
        var original = Requirement("one");

        var requirements = new OperationEvidenceRequirements(
            new[] { original });

        Assert.Same(
            original,
            Assert.Single(requirements.Requirements));
    }

    [Fact]
    public void MultipleRequirementsPreserveDeclarationOrder()
    {
        var first = Requirement("first");
        var second = Requirement("second", "step-b");
        var third = Requirement("third");

        var requirements = new OperationEvidenceRequirements(
            new[] { third, first, second });

        Assert.Same(third, requirements.Requirements[0]);
        Assert.Same(first, requirements.Requirements[1]);
        Assert.Same(second, requirements.Requirements[2]);
    }

    [Fact]
    public void ExactDuplicateRequirementIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEvidenceRequirements(
                new[]
                {
                    Requirement("evidence-a"),
                    Requirement("evidence-a", "step-b")
                }));
    }

    [Fact]
    public void RequirementIdDuplicatesAreRejectedIgnoringCase()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEvidenceRequirements(
                new[]
                {
                    Requirement("EVIDENCE-A"),
                    Requirement("evidence-a", "step-b")
                }));
    }

    [Fact]
    public void RequirementIdDuplicatesAreRejectedAfterTrimming()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationEvidenceRequirements(
                new[]
                {
                    Requirement("  Evidence-A  "),
                    Requirement("evidence-a")
                }));
    }

    [Fact]
    public void SameStepMayHaveSeveralDifferentRequirements()
    {
        var first = Requirement("capture-before", "step-a");
        var second = Requirement("capture-after", "step-a");

        var requirements = new OperationEvidenceRequirements(
            new[] { first, second });

        Assert.Equal(2, requirements.Requirements.Count);
        Assert.All(requirements.Requirements,
            item => Assert.Equal("step-a", item.StepId));
    }

    [Fact]
    public void SameRequirementCategoryCanAppearMultipleTimes()
    {
        var requirements = new OperationEvidenceRequirements(
            new[]
            {
                Requirement("before"),
                Requirement("after")
            });

        Assert.Equal(2, requirements.Requirements.Count);
    }

    [Fact]
    public void MandatoryAndOptionalRequirementsCoexist()
    {
        var required = Requirement("required", isMandatory: true);
        var optional = Requirement("optional", isMandatory: false);

        var requirements = new OperationEvidenceRequirements(
            new[] { required, optional });

        Assert.True(requirements.Requirements[0].IsMandatory);
        Assert.False(requirements.Requirements[1].IsMandatory);
    }

    [Fact]
    public void InputListMutationDoesNotAffectSnapshot()
    {
        var original = Requirement("one");
        var input = new List<OperationEvidenceRequirement>
        {
            original
        };

        var requirements = new OperationEvidenceRequirements(input);

        input.Clear();
        input.Add(Requirement("two"));

        Assert.Same(
            original,
            Assert.Single(requirements.Requirements));
    }

    [Fact]
    public void OutputCollectionRejectsMutation()
    {
        var original = Requirement("one");

        var requirements = new OperationEvidenceRequirements(
            new[] { original });

        var list = Assert.IsAssignableFrom<
            IList<OperationEvidenceRequirement>>(
                requirements.Requirements);

        Assert.True(list.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Requirement("two")));

        Assert.Throws<NotSupportedException>(() =>
            list[0] = Requirement("three"));

        Assert.Same(
            original,
            Assert.Single(requirements.Requirements));
    }

    [Fact]
    public void EmptyRequirementsDoNotCreateSyntheticEvidence()
    {
        var requirements = new OperationEvidenceRequirements(
            Array.Empty<OperationEvidenceRequirement>());

        Assert.Empty(requirements.Requirements);
    }

    [Fact]
    public void CollectionDoesNotCollapseRequirementsByStepId()
    {
        var requirements = new OperationEvidenceRequirements(
            new[]
            {
                Requirement("one", "step-a"),
                Requirement("two", "step-a"),
                Requirement("three", "step-b")
            });

        Assert.Equal(
            new[] { "one", "two", "three" },
            requirements.Requirements.Select(x => x.RequirementId));
    }
}

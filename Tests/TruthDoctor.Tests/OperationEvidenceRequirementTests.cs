using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationEvidenceRequirementTests
{
    private static OperationEvidenceRequirement Create(
        string requirementId = "evidence-a",
        string stepId = "step-a",
        string category = "resource-state",
        string description = "Capture resulting resource state",
        bool isMandatory = true,
        string? sourceHint = null)
    {
        return new OperationEvidenceRequirement(
            requirementId,
            stepId,
            category,
            description,
            isMandatory,
            sourceHint);
    }

    [Fact]
    public void TypeIsPublicAndSealed()
    {
        Assert.True(typeof(OperationEvidenceRequirement).IsPublic);
        Assert.True(typeof(OperationEvidenceRequirement).IsSealed);
    }

    [Fact]
    public void ExactlySixPublicGetterOnlyProperties()
    {
        var properties = typeof(OperationEvidenceRequirement)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[]
            {
                "Category",
                "Description",
                "IsMandatory",
                "RequirementId",
                "SourceHint",
                "StepId"
            },
            properties.Select(p => p.Name).OrderBy(x => x));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });

        Assert.Equal(typeof(string),
            typeof(OperationEvidenceRequirement)
                .GetProperty("RequirementId")!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(OperationEvidenceRequirement)
                .GetProperty("StepId")!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(OperationEvidenceRequirement)
                .GetProperty("Category")!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(OperationEvidenceRequirement)
                .GetProperty("Description")!.PropertyType);
        Assert.Equal(typeof(bool),
            typeof(OperationEvidenceRequirement)
                .GetProperty("IsMandatory")!.PropertyType);
        Assert.Equal(typeof(string),
            typeof(OperationEvidenceRequirement)
                .GetProperty("SourceHint")!.PropertyType);
    }

    [Fact]
    public void OnePublicConstructorHasLockedSignature()
    {
        var constructor = Assert.Single(
            typeof(OperationEvidenceRequirement).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(
            new[]
            {
                "requirementId",
                "stepId",
                "category",
                "description",
                "isMandatory",
                "sourceHint"
            },
            parameters.Select(p => p.Name));

        Assert.Equal(
            new[]
            {
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(bool),
                typeof(string)
            },
            parameters.Select(p => p.ParameterType));

        Assert.All(parameters.Take(4),
            p => Assert.False(p.IsOptional));

        Assert.True(parameters[4].IsOptional);
        Assert.Equal(true, parameters[4].DefaultValue);
        Assert.True(parameters[5].IsOptional);
        Assert.Null(parameters[5].DefaultValue);
    }

    [Fact]
    public void RequiredFieldsAndDefaultsArePreserved()
    {
        var requirement = Create();

        Assert.Equal("evidence-a", requirement.RequirementId);
        Assert.Equal("step-a", requirement.StepId);
        Assert.Equal("resource-state", requirement.Category);
        Assert.Equal(
            "Capture resulting resource state",
            requirement.Description);
        Assert.True(requirement.IsMandatory);
        Assert.Null(requirement.SourceHint);
    }

    [Fact]
    public void ExplicitOptionalRequirementIsPreserved()
    {
        var requirement = Create(isMandatory: false);

        Assert.False(requirement.IsMandatory);
    }

    [Fact]
    public void AllFieldsCanBeDeclaredIndependently()
    {
        var requirement = Create(
            "EVIDENCE-01",
            "Step-B",
            "Audit-Record",
            "Capture the audit event",
            false,
            "Provider audit API");

        Assert.Equal("EVIDENCE-01", requirement.RequirementId);
        Assert.Equal("Step-B", requirement.StepId);
        Assert.Equal("Audit-Record", requirement.Category);
        Assert.Equal("Capture the audit event",
            requirement.Description);
        Assert.False(requirement.IsMandatory);
        Assert.Equal("Provider audit API", requirement.SourceHint);
    }

    [Fact]
    public void RequiredStringsAreTrimmedPreservingCase()
    {
        var requirement = Create(
            "  Evidence-A  ",
            "  Step-B  ",
            "  Audit-Record  ",
            "  Capture New State  ");

        Assert.Equal("Evidence-A", requirement.RequirementId);
        Assert.Equal("Step-B", requirement.StepId);
        Assert.Equal("Audit-Record", requirement.Category);
        Assert.Equal("Capture New State", requirement.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankRequirementIdIsRejected(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(requirementId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankStepIdIsRejected(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(stepId: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankCategoryIsRejected(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(category: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankDescriptionIsRejected(string? value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(description: value!));
    }

    [Theory]
    [InlineData("evidence\ninvalid")]
    [InlineData("evidence\tinvalid")]
    [InlineData("evidence\0invalid")]
    public void RequirementIdRejectsEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(requirementId: value));
    }

    [Theory]
    [InlineData("step\ninvalid")]
    [InlineData("step\tinvalid")]
    [InlineData("step\0invalid")]
    public void StepIdRejectsEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(stepId: value));
    }

    [Theory]
    [InlineData("category\ninvalid")]
    [InlineData("category\tinvalid")]
    [InlineData("category\0invalid")]
    public void CategoryRejectsEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(category: value));
    }

    [Theory]
    [InlineData("description\ninvalid")]
    [InlineData("description\tinvalid")]
    [InlineData("description\0invalid")]
    public void DescriptionRejectsEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(description: value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BlankSourceHintNormalizesToNull(string? value)
    {
        Assert.Null(Create(sourceHint: value).SourceHint);
    }

    [Fact]
    public void SourceHintIsTrimmedPreservingCase()
    {
        Assert.Equal(
            "Provider Audit API",
            Create(sourceHint: "  Provider Audit API  ").SourceHint);
    }

    [Theory]
    [InlineData("source\ninvalid")]
    [InlineData("source\tinvalid")]
    [InlineData("source\0invalid")]
    public void SourceHintRejectsEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            Create(sourceHint: value));
    }

    [Fact]
    public void CategoryIsNotRestrictedToSpecificProviderNames()
    {
        var first = Create(category: "configuration-snapshot");
        var second = Create(category: "custom:verification-proof");

        Assert.Equal("configuration-snapshot", first.Category);
        Assert.Equal("custom:verification-proof", second.Category);
    }

    [Fact]
    public void MandatoryAndSourceHintAreIndependent()
    {
        var requirement = Create(
            isMandatory: true,
            sourceHint: null);

        Assert.True(requirement.IsMandatory);
        Assert.Null(requirement.SourceHint);
    }
}

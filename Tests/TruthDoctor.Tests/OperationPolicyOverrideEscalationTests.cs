using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationPolicyOverrideEscalationTests
{
    [Fact]
    public void TypeIsPublicSealedAndProviderNeutral()
    {
        var type = typeof(OperationPolicyOverrideEscalation);

        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void PublicApiHasExactlyThreeGetterOnlyProperties()
    {
        var type = typeof(OperationPolicyOverrideEscalation);
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Equal(
            new[]
            {
                "EscalationRequestReason",
                "OverridePolicyId",
                "OverrideRequestReason"
            },
            properties.Select(p => p.Name).OrderBy(n => n));

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
            Assert.Equal(typeof(string), property.PropertyType);
        });
    }

    [Fact]
    public void ConstructorHasThreeOptionalNullableStrings()
    {
        var constructor = Assert.Single(
            typeof(OperationPolicyOverrideEscalation).GetConstructors());

        var parameters = constructor.GetParameters();

        Assert.Equal(
            new[]
            {
                "overridePolicyId",
                "overrideRequestReason",
                "escalationRequestReason"
            },
            parameters.Select(p => p.Name));

        Assert.All(parameters, parameter =>
        {
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.True(parameter.IsOptional);
            Assert.Null(parameter.DefaultValue);
        });
    }

    [Fact]
    public void DefaultDoesNotInventRequests()
    {
        var declaration = new OperationPolicyOverrideEscalation();

        Assert.Null(declaration.OverridePolicyId);
        Assert.Null(declaration.OverrideRequestReason);
        Assert.Null(declaration.EscalationRequestReason);
    }

    [Fact]
    public void RequestsRemainIndependentAndPreserveOriginalCase()
    {
        var overrideOnly = new OperationPolicyOverrideEscalation(
            overridePolicyId: "  Policy-ALPHA  ",
            overrideRequestReason: "  Manual EXCEPTION Requested  ");

        var escalationOnly = new OperationPolicyOverrideEscalation(
            escalationRequestReason: "  Escalate For REVIEW  ");

        var all = new OperationPolicyOverrideEscalation(
            "  Policy-BETA  ",
            "  Maintenance WINDOW  ",
            "  Security TEAM Review  ");

        Assert.Equal("Policy-ALPHA", overrideOnly.OverridePolicyId);
        Assert.Equal(
            "Manual EXCEPTION Requested",
            overrideOnly.OverrideRequestReason);
        Assert.Null(overrideOnly.EscalationRequestReason);

        Assert.Null(escalationOnly.OverridePolicyId);
        Assert.Null(escalationOnly.OverrideRequestReason);
        Assert.Equal(
            "Escalate For REVIEW",
            escalationOnly.EscalationRequestReason);

        Assert.Equal("Policy-BETA", all.OverridePolicyId);
        Assert.Equal("Maintenance WINDOW", all.OverrideRequestReason);
        Assert.Equal(
            "Security TEAM Review",
            all.EscalationRequestReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankPolicyIdsNormalizeToNull(string? value)
    {
        var declaration = new OperationPolicyOverrideEscalation(
            overridePolicyId: value);

        Assert.Null(declaration.OverridePolicyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankOverrideReasonsNormalizeToNull(string? value)
    {
        var declaration = new OperationPolicyOverrideEscalation(
            overrideRequestReason: value);

        Assert.Null(declaration.OverrideRequestReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void BlankEscalationReasonsNormalizeToNull(string? value)
    {
        var declaration = new OperationPolicyOverrideEscalation(
            escalationRequestReason: value);

        Assert.Null(declaration.EscalationRequestReason);
    }

    [Theory]
    [InlineData("policy\nid")]
    [InlineData("policy\rid")]
    [InlineData("policy\tid")]
    [InlineData("policy\0id")]
    public void PolicyIdsRejectEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPolicyOverrideEscalation(
                overridePolicyId: value));
    }

    [Theory]
    [InlineData("override\nreason")]
    [InlineData("override\rreason")]
    [InlineData("override\treason")]
    [InlineData("override\0reason")]
    public void OverrideReasonsRejectEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPolicyOverrideEscalation(
                overrideRequestReason: value));
    }

    [Theory]
    [InlineData("escalation\nreason")]
    [InlineData("escalation\rreason")]
    [InlineData("escalation\treason")]
    [InlineData("escalation\0reason")]
    public void EscalationReasonsRejectEmbeddedControls(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationPolicyOverrideEscalation(
                escalationRequestReason: value));
    }
}

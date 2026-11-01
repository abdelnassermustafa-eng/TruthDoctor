using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

/// <summary>
/// Test-first contract for caller-declared history target attribution.
/// A target declaration does not prove discovery, existence, execution,
/// modification, authorization, or verified historical truth.
/// </summary>
public sealed class OperationHistoryTargetAttributionTests
{
    private static OperationHistoryEvent Event(
        string operationId = "operation-target-42")
    {
        return new OperationHistoryEvent(
            "history-target-001",
            new OperationCorrelationId(operationId),
            "TargetReported");
    }

    private static OperationTarget Target(
        string providerId = "aws",
        string accountId = "account-001",
        string location = "us-east-1",
        string domainId = "domain-001",
        string resourceType = "ComputeInstance",
        string nativeId = "instance-001")
    {
        return new OperationTarget(
            providerId,
            accountId,
            location,
            domainId,
            resourceType,
            nativeId);
    }

    private static OperationHistoryTargetAttribution Create()
    {
        return new OperationHistoryTargetAttribution(
            Event(), Target());
    }

    [Fact]
    public void PreservesExactHistoryEventReference()
    {
        var historyEvent = Event();
        var result = new OperationHistoryTargetAttribution(
            historyEvent, Target());

        Assert.Same(historyEvent, result.Event);
        Assert.Equal("history-target-001", result.Event.EventId);
    }

    [Fact]
    public void PreservesExactOperationTargetReference()
    {
        var target = Target();
        var result = new OperationHistoryTargetAttribution(
            Event(), target);

        Assert.Same(target, result.Target);
    }

    [Fact]
    public void PreservesAllSixTargetIdentityFields()
    {
        var result = Create();

        Assert.Equal("aws", result.Target.ProviderId);
        Assert.Equal("account-001", result.Target.AccountId);
        Assert.Equal("us-east-1", result.Target.Location);
        Assert.Equal("domain-001", result.Target.DomainId);
        Assert.Equal("ComputeInstance", result.Target.ResourceType);
        Assert.Equal("instance-001", result.Target.NativeId);
    }

    [Fact]
    public void PreservesDistinctProviderAndAccountIdentities()
    {
        var target = Target(
            providerId: "Provider.CaseSensitive",
            accountId: "Account.MixedCase");
        var result = new OperationHistoryTargetAttribution(
            Event(), target);

        Assert.Same(target, result.Target);
        Assert.Equal("Provider.CaseSensitive", result.Target.ProviderId);
        Assert.Equal("Account.MixedCase", result.Target.AccountId);
    }

    [Fact]
    public void PreservesDistinctLocationAndDomainIdentities()
    {
        var target = Target(
            location: "Region-Custom-01",
            domainId: "Domain.Custom/Two");
        var result = new OperationHistoryTargetAttribution(
            Event(), target);

        Assert.Same(target, result.Target);
        Assert.Equal("Region-Custom-01", result.Target.Location);
        Assert.Equal("Domain.Custom/Two", result.Target.DomainId);
    }

    [Fact]
    public void PreservesDistinctResourceTypeAndNativeId()
    {
        var target = Target(
            resourceType: "VirtualNetwork",
            nativeId: "Network/MixedCase-001");
        var result = new OperationHistoryTargetAttribution(
            Event(), target);

        Assert.Same(target, result.Target);
        Assert.Equal("VirtualNetwork", result.Target.ResourceType);
        Assert.Equal("Network/MixedCase-001", result.Target.NativeId);
    }

    [Fact]
    public void RejectsNullHistoryEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryTargetAttribution(
                null!, Target()));
    }

    [Fact]
    public void RejectsNullOperationTarget()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationHistoryTargetAttribution(
                Event(), null!));
    }

    [Fact]
    public void DoesNotRequireMatchingOperationOrResourceScope()
    {
        var firstEvent = Event("operation-alpha");
        var secondEvent = Event("operation-beta");
        var target = Target();

        var first = new OperationHistoryTargetAttribution(
            firstEvent, target);
        var second = new OperationHistoryTargetAttribution(
            secondEvent, target);

        Assert.Same(firstEvent, first.Event);
        Assert.Same(secondEvent, second.Event);
        Assert.Same(target, first.Target);
        Assert.Same(target, second.Target);
    }

    [Fact]
    public void DoesNotInferDiscoveryOrResourceExistence()
    {
        var result = Create();

        var properties = result.GetType().GetProperties(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(
            properties,
            property => property.Name is
                "Discovered" or "Exists" or "Reachable" or
                "Affected" or "Modified" or "Executed" or
                "Succeeded" or "Authorized" or "Approved");
    }

    [Fact]
    public void IsPublicSealedProviderNeutralReferenceType()
    {
        var type = typeof(OperationHistoryTargetAttribution);

        Assert.True(type.IsPublic);
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed);
        Assert.Equal("TruthDoctor.Models.Platform", type.Namespace);
    }

    [Fact]
    public void ExposesExactlyTwoGetterOnlyProperties()
    {
        var properties = typeof(OperationHistoryTargetAttribution)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

        Assert.Equal(2, properties.Length);

        var expected = new (string Name, Type Type)[]
        {
            ("Event", typeof(OperationHistoryEvent)),
            ("Target", typeof(OperationTarget))
        };

        foreach (var (name, propertyType) in expected)
        {
            var property = Assert.Single(
                properties,
                item => item.Name == name);

            Assert.Equal(propertyType, property.PropertyType);
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void HasExactlyOneTwoArgumentConstructor()
    {
        var constructor = Assert.Single(
            typeof(OperationHistoryTargetAttribution)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance));

        var parameters = constructor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal("historyEvent", parameters[0].Name);
        Assert.Equal(
            typeof(OperationHistoryEvent),
            parameters[0].ParameterType);
        Assert.Equal("target", parameters[1].Name);
        Assert.Equal(typeof(OperationTarget), parameters[1].ParameterType);

        Assert.All(
            parameters,
            parameter => Assert.False(parameter.IsOptional));
    }

    [Fact]
    public void ExposesNoPublicOperationalMethods()
    {
        var methods = typeof(OperationHistoryTargetAttribution)
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Empty(methods);
    }

    [Fact]
    public void DoesNotDeclareIntegrationInterfaces()
    {
        Assert.Empty(
            typeof(OperationHistoryTargetAttribution)
                .GetInterfaces());
    }
}

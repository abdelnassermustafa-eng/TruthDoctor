using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationReachabilityVerificationTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 29, 16, 0, 0, TimeSpan.Zero);

    private static OperationTarget Source(int dimension = -1)
    {
        var fields = new[]
        {
            "provider-a", "account-a", "us-east-1",
            "network", "router", "router-01"
        };

        if (dimension >= 0)
        {
            fields[dimension] += "-different";
        }

        return new OperationTarget(
            fields[0], fields[1], fields[2],
            fields[3], fields[4], fields[5]);
    }

    private static OperationReachabilityContext Context(
        string? interfaceId = "eth0",
        string? vrf = "production",
        int? vlanId = 101,
        string? addressFamily = "ipv4") =>
        new(interfaceId, vrf, vlanId, addressFamily);

    private static OperationReachabilityCheckSpecification Specification(
        string destination = "192.0.2.20",
        string protocol = "tcp.connect",
        int? port = 443,
        OperationReachabilityContext? context = null) =>
        new(
            destination,
            new OperationReachabilityProtocol(protocol),
            context ?? Context(),
            port);

    private static OperationReachabilityObservation Observation(
        OperationReachabilityObservationDisposition disposition =
            OperationReachabilityObservationDisposition.Satisfied,
        OperationTarget? source = null,
        OperationReachabilityCheckSpecification? specification = null) =>
        new(
            new OperationCorrelationId("operation-reach-01"),
            source ?? Source(),
            specification ?? Specification(),
            Timestamp,
            disposition);

    private static OperationReachabilityComparison Compare(
        string key = "network.probe",
        string expected = "satisfied",
        OperationReachabilityObservationDisposition disposition =
            OperationReachabilityObservationDisposition.Satisfied,
        OperationTarget? observedSource = null,
        OperationReachabilityCheckSpecification? observedSpecification = null,
        OperationReachabilityCheckSpecification? declaredSpecification = null) =>
        OperationReachabilityComparison.Evaluate(
            new OperationOutcomeCondition(key, expected),
            new OperationResourceScope(Source()),
            declaredSpecification ?? Specification(),
            Observation(
                disposition,
                observedSource,
                observedSpecification));

    [Fact]
    public void ObservationDispositionHasExactValues()
    {
        Assert.Equal(0, (int)OperationReachabilityObservationDisposition.Unknown);
        Assert.Equal(1, (int)OperationReachabilityObservationDisposition.Satisfied);
        Assert.Equal(2, (int)OperationReachabilityObservationDisposition.Unsatisfied);
        Assert.Equal(3, (int)OperationReachabilityObservationDisposition.Unavailable);
        Assert.Equal(4, (int)OperationReachabilityObservationDisposition.NotAssessed);
        Assert.Equal(5, (int)OperationReachabilityObservationDisposition.UnsupportedCapability);
        Assert.Equal(6,
            Enum.GetValues<OperationReachabilityObservationDisposition>().Length);
    }

    [Fact]
    public void ComparisonDispositionHasExactValues()
    {
        Assert.Equal(0, (int)OperationReachabilityComparisonDisposition.Unknown);
        Assert.Equal(1, (int)OperationReachabilityComparisonDisposition.Matched);
        Assert.Equal(2, (int)OperationReachabilityComparisonDisposition.Mismatched);
        Assert.Equal(3, (int)OperationReachabilityComparisonDisposition.SourceMismatch);
        Assert.Equal(4, (int)OperationReachabilityComparisonDisposition.SpecificationMismatch);
        Assert.Equal(5, (int)OperationReachabilityComparisonDisposition.UnsupportedCondition);
        Assert.Equal(6, (int)OperationReachabilityComparisonDisposition.UnsupportedProtocol);
        Assert.Equal(7, (int)OperationReachabilityComparisonDisposition.UnsupportedCapability);
        Assert.Equal(8, (int)OperationReachabilityComparisonDisposition.NoObservation);
        Assert.Equal(9, (int)OperationReachabilityComparisonDisposition.Unavailable);
        Assert.Equal(10, (int)OperationReachabilityComparisonDisposition.NotAssessed);
        Assert.Equal(11,
            Enum.GetValues<OperationReachabilityComparisonDisposition>().Length);
    }

    [Fact]
    public void ContextPreservesAllFourFields()
    {
        var context = Context();

        Assert.Equal("eth0", context.InterfaceId);
        Assert.Equal("production", context.Vrf);
        Assert.Equal(101, context.VlanId);
        Assert.Equal("ipv4", context.AddressFamily);
    }

    [Fact]
    public void ContextAllowsExplicitlyAbsentFields()
    {
        var context = new OperationReachabilityContext();

        Assert.Null(context.InterfaceId);
        Assert.Null(context.Vrf);
        Assert.Null(context.VlanId);
        Assert.Null(context.AddressFamily);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4094)]
    public void ValidVlanBoundariesAreAccepted(int vlan)
    {
        Assert.Equal(vlan, Context(vlanId: vlan).VlanId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4095)]
    [InlineData(-1)]
    public void InvalidVlanIdsAreRejected(int vlan)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Context(vlanId: vlan));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void BlankOrControlContextValuesAreRejected(int which)
    {
        Assert.Throws<ArgumentException>(() =>
            which switch
            {
                0 => Context(interfaceId: " "),
                1 => Context(vrf: "\t"),
                _ => Context(addressFamily: "ipv4\ninvalid")
            });
    }

    [Fact]
    public void ContextNormalizesProvidedIdentifiers()
    {
        var context = Context(
            " ethernet0 ", " blue ", 200, " ipv6 ");

        Assert.Equal("ethernet0", context.InterfaceId);
        Assert.Equal("blue", context.Vrf);
        Assert.Equal(200, context.VlanId);
        Assert.Equal("ipv6", context.AddressFamily);
    }

    [Fact]
    public void ProtocolPreservesNormalizedIdentifier()
    {
        var protocol =
            new OperationReachabilityProtocol(" icmp.echo ");

        Assert.Equal("icmp.echo", protocol.Identifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n")]
    [InlineData("tcp\nconnect")]
    public void InvalidProtocolIdentifierIsRejected(string identifier)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationReachabilityProtocol(identifier));
    }

    [Fact]
    public void ProtocolIdentityIsIndependentOfVendor()
    {
        var protocol = new OperationReachabilityProtocol("tcp.connect");

        Assert.Equal("tcp.connect", protocol.Identifier);
        Assert.Null(
            typeof(OperationReachabilityProtocol).GetProperty("Vendor"));
        Assert.Null(
            typeof(OperationReachabilityProtocol).GetProperty("Provider"));
    }

    [Fact]
    public void IcmpEchoDoesNotRequirePort()
    {
        var specification = Specification(
            protocol: "icmp.echo",
            port: null);

        Assert.Equal("icmp.echo", specification.Protocol.Identifier);
        Assert.Null(specification.DestinationPort);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(443)]
    [InlineData(65535)]
    public void TcpConnectAcceptsValidPort(int port)
    {
        Assert.Equal(port, Specification(port: port).DestinationPort);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void TcpConnectRejectsInvalidPort(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Specification(port: port));
    }

    [Fact]
    public void TcpConnectRejectsMissingPort()
    {
        Assert.Throws<ArgumentException>(() =>
            Specification(port: null));
    }

    [Fact]
    public void IcmpEchoRejectsPort()
    {
        Assert.Throws<ArgumentException>(() =>
            Specification(protocol: "icmp.echo", port: 443));
    }

    [Fact]
    public void SpecificationRejectsNullProtocol()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationReachabilityCheckSpecification(
                "192.0.2.20", null!, Context(), 443));
    }

    [Fact]
    public void SpecificationRejectsNullContext()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationReachabilityCheckSpecification(
                "192.0.2.20",
                new OperationReachabilityProtocol("tcp.connect"),
                null!, 443));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n")]
    [InlineData("host\ninvalid")]
    public void SpecificationRejectsInvalidDestination(string destination)
    {
        Assert.Throws<ArgumentException>(() =>
            Specification(destination: destination));
    }

    [Fact]
    public void SpecificationPreservesExactIdentity()
    {
        var context = Context();
        var protocol = new OperationReachabilityProtocol("tcp.connect");
        var specification =
            new OperationReachabilityCheckSpecification(
                " 192.0.2.20 ", protocol, context, 443);

        Assert.Equal("192.0.2.20", specification.Destination);
        Assert.Same(protocol, specification.Protocol);
        Assert.Same(context, specification.Context);
        Assert.Equal(443, specification.DestinationPort);
    }

    [Theory]
    [InlineData("satisfied",
        OperationReachabilityObservationDisposition.Satisfied,
        OperationReachabilityComparisonDisposition.Matched)]
    [InlineData("satisfied",
        OperationReachabilityObservationDisposition.Unsatisfied,
        OperationReachabilityComparisonDisposition.Mismatched)]
    [InlineData("unsatisfied",
        OperationReachabilityObservationDisposition.Unsatisfied,
        OperationReachabilityComparisonDisposition.Matched)]
    [InlineData("unsatisfied",
        OperationReachabilityObservationDisposition.Satisfied,
        OperationReachabilityComparisonDisposition.Mismatched)]
    public void ComparisonIsExpectationAware(
        string expected,
        OperationReachabilityObservationDisposition observed,
        OperationReachabilityComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(expected: expected, disposition: observed)
                .Disposition);
    }

    [Theory]
    [InlineData(
        OperationReachabilityObservationDisposition.Unknown,
        OperationReachabilityComparisonDisposition.NoObservation)]
    [InlineData(
        OperationReachabilityObservationDisposition.Unavailable,
        OperationReachabilityComparisonDisposition.Unavailable)]
    [InlineData(
        OperationReachabilityObservationDisposition.NotAssessed,
        OperationReachabilityComparisonDisposition.NotAssessed)]
    [InlineData(
        OperationReachabilityObservationDisposition.UnsupportedCapability,
        OperationReachabilityComparisonDisposition.UnsupportedCapability)]
    public void NonAttemptStatesAreDistinctAndNeverMatch(
        OperationReachabilityObservationDisposition disposition,
        OperationReachabilityComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(expected: "unsatisfied", disposition: disposition)
                .Disposition);
    }

    [Fact]
    public void ExpectedConditionTokensIgnoreCase()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.Matched,
            Compare(key: "NETWORK.PROBE", expected: "SATISFIED")
                .Disposition);
    }

    [Theory]
    [InlineData("resource.health", "satisfied")]
    [InlineData("network.probe", "reachable")]
    [InlineData("network.probe", "healthy")]
    public void UnsupportedConditionsNeverMatch(
        string key,
        string expected)
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedCondition,
            Compare(key: key, expected: expected).Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EverySourceIdentityDimensionIsStrict(int dimension)
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SourceMismatch,
            Compare(observedSource: Source(dimension)).Disposition);
    }

    [Fact]
    public void DestinationMismatchDoesNotMatch()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                observedSpecification:
                    Specification(destination: "192.0.2.21"))
                .Disposition);
    }

    [Fact]
    public void DirectionReversalDoesNotMatch()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SourceMismatch,
            Compare(
                observedSource:
                    new OperationTarget(
                        "provider-a", "account-a", "us-east-1",
                        "network", "router", "192.0.2.20"),
                observedSpecification:
                    Specification(destination: "router-01"))
                .Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryNetworkContextDimensionIsStrict(int dimension)
    {
        var changed = dimension switch
        {
            0 => Context(interfaceId: "eth1"),
            1 => Context(vrf: "guest"),
            2 => Context(vlanId: 102),
            _ => Context(addressFamily: "ipv6")
        };

        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                observedSpecification:
                    Specification(context: changed)).Disposition);
    }

    [Fact]
    public void AbsentAndDeclaredContextCannotSilentlyMatch()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                observedSpecification:
                    Specification(context: new OperationReachabilityContext()))
                .Disposition);
    }

    [Fact]
    public void ProtocolMismatchDoesNotMatch()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                observedSpecification:
                    Specification(protocol: "icmp.echo", port: null))
                .Disposition);
    }

    [Fact]
    public void PortMismatchDoesNotMatch()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                observedSpecification: Specification(port: 80))
                .Disposition);
    }

    [Fact]
    public void EquivalentIndependentSpecificationsMatch()
    {
        var independentlyConstructed = Specification();

        Assert.Equal(
            OperationReachabilityComparisonDisposition.Matched,
            Compare(observedSpecification: independentlyConstructed)
                .Disposition);
    }

    [Fact]
    public void UnsupportedProtocolCannotProduceMatch()
    {
        var declared = Specification(
            protocol: "udp.probe", port: null);

        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedProtocol,
            Compare(
                declaredSpecification: declared,
                observedSpecification:
                    Specification(protocol: "udp.probe", port: null))
                .Disposition);
    }

    [Fact]
    public void UnsupportedProtocolPrecedesReportedSuccess()
    {
        var declared = Specification(
            protocol: "arp.resolve", port: null);

        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedProtocol,
            Compare(
                declaredSpecification: declared,
                observedSpecification:
                    Specification(protocol: "arp.resolve", port: null),
                disposition:
                    OperationReachabilityObservationDisposition.Satisfied)
                .Disposition);
    }

    [Fact]
    public void SourceMismatchPrecedesUnsupportedCondition()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SourceMismatch,
            Compare(
                key: "invalid",
                observedSource: Source(5))
                .Disposition);
    }

    [Fact]
    public void SpecificationMismatchPrecedesUnsupportedCondition()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.SpecificationMismatch,
            Compare(
                key: "invalid",
                observedSpecification: Specification(port: 80))
                .Disposition);
    }

    [Fact]
    public void UnsupportedConditionPrecedesUnavailable()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedCondition,
            Compare(
                expected: "invalid",
                disposition:
                    OperationReachabilityObservationDisposition.Unavailable)
                .Disposition);
    }

    [Fact]
    public void UnsupportedConditionPrecedesUnsupportedProtocol()
    {
        var unsupported = Specification(
            protocol: "udp.probe", port: null);

        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedCondition,
            Compare(
                expected: "invalid",
                declaredSpecification: unsupported,
                observedSpecification:
                    Specification(protocol: "udp.probe", port: null))
                .Disposition);
    }

    [Fact]
    public void UnsupportedCapabilityPrecedesUnavailableClassification()
    {
        Assert.Equal(
            OperationReachabilityComparisonDisposition.UnsupportedCapability,
            Compare(disposition:
                OperationReachabilityObservationDisposition.UnsupportedCapability)
                .Disposition);
    }

    [Fact]
    public void ObservationPreservesAllInputs()
    {
        var operation = new OperationCorrelationId("operation-reach-01");
        var source = Source();
        var specification = Specification();

        var observation = new OperationReachabilityObservation(
            operation, source, specification, Timestamp,
            OperationReachabilityObservationDisposition.Satisfied);

        Assert.Equal(operation, observation.Operation);
        Assert.Same(source, observation.Source);
        Assert.Same(specification, observation.Specification);
        Assert.Equal(Timestamp, observation.ObservedAt);
        Assert.Equal(
            OperationReachabilityObservationDisposition.Satisfied,
            observation.Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ObservationRejectsInvalidInputs(int which)
    {
        var operation = new OperationCorrelationId("operation-reach-01");
        var source = Source();
        var specification = Specification();

        Assert.ThrowsAny<ArgumentException>(() =>
            new OperationReachabilityObservation(
                which == 0 ? default : operation,
                which == 1 ? null! : source,
                which == 2 ? null! : specification,
                which == 3 ? default : Timestamp,
                OperationReachabilityObservationDisposition.Satisfied));
    }

    [Fact]
    public void ObservationRejectsUndefinedDisposition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationReachabilityObservation(
                new OperationCorrelationId("operation-reach-01"),
                Source(), Specification(), Timestamp,
                (OperationReachabilityObservationDisposition)99));
    }

    [Fact]
    public void ComparisonPreservesExactInputs()
    {
        var condition =
            new OperationOutcomeCondition("network.probe", "satisfied");
        var scope = new OperationResourceScope(Source());
        var specification = Specification();
        var observation = Observation();

        var comparison = OperationReachabilityComparison.Evaluate(
            condition, scope, specification, observation);

        Assert.Same(condition, comparison.Condition);
        Assert.Same(scope, comparison.SourceScope);
        Assert.Same(specification, comparison.Specification);
        Assert.Same(observation, comparison.Observation);
        Assert.Equal(
            OperationReachabilityComparisonDisposition.Matched,
            comparison.Disposition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ComparisonRejectsNullInputs(int which)
    {
        var condition =
            new OperationOutcomeCondition("network.probe", "satisfied");
        var scope = new OperationResourceScope(Source());
        var specification = Specification();
        var observation = Observation();

        Assert.Throws<ArgumentNullException>(() =>
            OperationReachabilityComparison.Evaluate(
                which == 0 ? null! : condition,
                which == 1 ? null! : scope,
                which == 2 ? null! : specification,
                which == 3 ? null! : observation));
    }

    [Theory]
    [InlineData(typeof(OperationReachabilityContext))]
    [InlineData(typeof(OperationReachabilityProtocol))]
    [InlineData(typeof(OperationReachabilityCheckSpecification))]
    [InlineData(typeof(OperationReachabilityObservation))]
    [InlineData(typeof(OperationReachabilityComparison))]
    public void ContractClassesAreSealedAndImmutable(Type type)
    {
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);

        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(properties);

        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Fact]
    public void ComparisonPublicApiHasOneEvaluateMethod()
    {
        var type = typeof(OperationReachabilityComparison);
        Assert.Empty(type.GetConstructors());

        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.DeclaredOnly |
            BindingFlags.Static | BindingFlags.Instance);

        var method = Assert.Single(
            methods, candidate => !candidate.IsSpecialName);

        Assert.Equal("Evaluate", method.Name);
        Assert.True(method.IsStatic);
        Assert.Equal(type, method.ReturnType);

        Assert.Equal(new[]
        {
            typeof(OperationOutcomeCondition),
            typeof(OperationResourceScope),
            typeof(OperationReachabilityCheckSpecification),
            typeof(OperationReachabilityObservation)
        }, method.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void ComparisonDoesNotAssertVerifiedSuccess()
    {
        var type = typeof(OperationReachabilityComparison);

        Assert.Null(type.GetProperty("IsVerified"));
        Assert.Null(type.GetProperty("IsSuccessful"));
        Assert.Null(type.GetProperty("VerifiedSuccess"));
        Assert.Null(type.GetProperty("ExecutionSucceeded"));
        Assert.Null(type.GetProperty("Evidence"));
    }
}

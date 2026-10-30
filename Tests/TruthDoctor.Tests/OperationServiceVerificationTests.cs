using System;
using System.Linq;
using System.Reflection;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationServiceVerificationTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    private static OperationTarget Source(int changed = -1)
    {
        var fields = new[]
        {
            "provider-a", "account-a", "us-east-1",
            "application", "service", "service-01"
        };

        if (changed >= 0)
            fields[changed] += "-different";

        return new OperationTarget(
            fields[0], fields[1], fields[2],
            fields[3], fields[4], fields[5]);
    }

    private static OperationServiceCheckSpecification Specification(
        string protocol = "http.response-status",
        string uri = "https://example.com:443/health?ready=1",
        string method = "GET",
        int expectedStatus = 200) =>
        new(
            new OperationServiceProtocol(protocol),
            new OperationServiceEndpoint(uri),
            method,
            new OperationServiceResponseCriteria(expectedStatus));

    private static OperationServiceObservation Observation(
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded,
        int? httpStatus = 200,
        OperationTarget? source = null,
        OperationServiceCheckSpecification? specification = null) =>
        new(
            new OperationCorrelationId("service-operation-01"),
            source ?? Source(),
            specification ?? Specification(),
            Timestamp,
            disposition,
            httpStatus);

    private static OperationServiceComparison Compare(
        string key = "service.http.status",
        string expected = "200",
        OperationServiceObservationDisposition disposition =
            OperationServiceObservationDisposition.Responded,
        int? observedStatus = 200,
        OperationTarget? source = null,
        OperationServiceCheckSpecification? declared = null,
        OperationServiceCheckSpecification? observed = null) =>
        OperationServiceComparison.Evaluate(
            new OperationOutcomeCondition(key, expected),
            new OperationResourceScope(Source()),
            declared ?? Specification(),
            Observation(disposition, observedStatus, source, observed));

    [Fact]
    public void ObservationDispositionValuesAreExplicit()
    {
        Assert.Equal(0, (int)OperationServiceObservationDisposition.Unknown);
        Assert.Equal(1, (int)OperationServiceObservationDisposition.Responded);
        Assert.Equal(2, (int)OperationServiceObservationDisposition.Unavailable);
        Assert.Equal(3, (int)OperationServiceObservationDisposition.NotAssessed);
        Assert.Equal(4, (int)OperationServiceObservationDisposition.UnsupportedCapability);
        Assert.Equal(5, Enum.GetValues<OperationServiceObservationDisposition>().Length);
    }

    [Fact]
    public void ComparisonDispositionValuesAreExplicit()
    {
        Assert.Equal(0, (int)OperationServiceComparisonDisposition.Unknown);
        Assert.Equal(1, (int)OperationServiceComparisonDisposition.Matched);
        Assert.Equal(2, (int)OperationServiceComparisonDisposition.Mismatched);
        Assert.Equal(3, (int)OperationServiceComparisonDisposition.SourceMismatch);
        Assert.Equal(4, (int)OperationServiceComparisonDisposition.SpecificationMismatch);
        Assert.Equal(5, (int)OperationServiceComparisonDisposition.UnsupportedCondition);
        Assert.Equal(6, (int)OperationServiceComparisonDisposition.UnsupportedProtocol);
        Assert.Equal(7, (int)OperationServiceComparisonDisposition.UnsupportedCapability);
        Assert.Equal(8, (int)OperationServiceComparisonDisposition.NoObservation);
        Assert.Equal(9, (int)OperationServiceComparisonDisposition.Unavailable);
        Assert.Equal(10, (int)OperationServiceComparisonDisposition.NotAssessed);
        Assert.Equal(11, Enum.GetValues<OperationServiceComparisonDisposition>().Length);
    }

    [Fact]
    public void ProtocolIdentifierIsNormalized()
    {
        var protocol = new OperationServiceProtocol(" http.response-status ");
        Assert.Equal("http.response-status", protocol.Identifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n")]
    [InlineData("http\nresponse")]
    public void InvalidProtocolIdentifiersAreRejected(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationServiceProtocol(value));
    }

    [Theory]
    [InlineData("https://example.com/health")]
    [InlineData("http://example.com/")]
    [InlineData("https://127.0.0.1:8443/status")]
    public void AbsoluteHttpEndpointsAreAccepted(string uri)
    {
        Assert.NotEmpty(new OperationServiceEndpoint(uri).Uri);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/relative")]
    [InlineData("ftp://example.com/")]
    [InlineData("file:///tmp/check")]
    [InlineData("https://user:secret@example.com/")]
    [InlineData("https://example.com/path#fragment")]
    [InlineData("http://")]
    public void InvalidOrUnsafeEndpointsAreRejected(string uri)
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationServiceEndpoint(uri));
    }

    [Fact]
    public void DefaultHttpsPortIsEquivalentToExplicit443()
    {
        var first = new OperationServiceEndpoint("https://EXAMPLE.com/health");
        var second = new OperationServiceEndpoint("https://example.com:443/health");
        Assert.Equal(first.Uri, second.Uri);
    }

    [Fact]
    public void DefaultHttpPortIsEquivalentToExplicit80()
    {
        var first = new OperationServiceEndpoint("http://EXAMPLE.com/health");
        var second = new OperationServiceEndpoint("http://example.com:80/health");
        Assert.Equal(first.Uri, second.Uri);
    }

    [Fact]
    public void PathAndQueryAreNotDiscarded()
    {
        var first = new OperationServiceEndpoint("https://example.com/a?x=1");
        var second = new OperationServiceEndpoint("https://example.com/b?x=1");
        var third = new OperationServiceEndpoint("https://example.com/a?x=2");
        Assert.NotEqual(first.Uri, second.Uri);
        Assert.NotEqual(first.Uri, third.Uri);
    }

    [Theory]
    [InlineData("GET", "GET")]
    [InlineData("get", "GET")]
    [InlineData("HEAD", "HEAD")]
    [InlineData("head", "HEAD")]
    public void SupportedMethodsAreNormalized(string input, string expected)
    {
        Assert.Equal(expected, Specification(method: input).Method);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("TRACE")]
    public void UnsupportedMethodsAreRejected(string method)
    {
        Assert.Throws<ArgumentException>(() =>
            Specification(method: method));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(599)]
    public void ExpectedHttpStatusIsExplicit(int status)
    {
        var criteria = new OperationServiceResponseCriteria(status);
        Assert.Equal(status, criteria.ExpectedHttpStatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    public void InvalidExpectedHttpStatusIsRejected(int status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationServiceResponseCriteria(status));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(599)]
    public void RespondedObservationPreservesStatus(int status)
    {
        var observation = Observation(httpStatus: status);
        Assert.Equal(status, observation.HttpStatusCode);
        Assert.Equal(OperationServiceObservationDisposition.Responded,
            observation.Disposition);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    public void InvalidObservedStatusIsRejected(int status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Observation(httpStatus: status));
    }

    [Fact]
    public void RespondedCannotSilentlyUseMissingStatus()
    {
        Assert.Throws<ArgumentException>(() =>
            Observation(httpStatus: null));
    }

    [Theory]
    [InlineData(OperationServiceObservationDisposition.Unknown)]
    [InlineData(OperationServiceObservationDisposition.Unavailable)]
    [InlineData(OperationServiceObservationDisposition.NotAssessed)]
    [InlineData(OperationServiceObservationDisposition.UnsupportedCapability)]
    public void NonresponseCannotCarryHttpStatus(
        OperationServiceObservationDisposition disposition)
    {
        Assert.Throws<ArgumentException>(() =>
            Observation(disposition, 200));
    }

    [Theory]
    [InlineData(OperationServiceObservationDisposition.Unknown)]
    [InlineData(OperationServiceObservationDisposition.Unavailable)]
    [InlineData(OperationServiceObservationDisposition.NotAssessed)]
    [InlineData(OperationServiceObservationDisposition.UnsupportedCapability)]
    public void NonresponseAllowsAbsentStatus(
        OperationServiceObservationDisposition disposition)
    {
        Assert.Null(Observation(disposition, null).HttpStatusCode);
    }

    [Fact]
    public void UndefinedObservationDispositionIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Observation(
                (OperationServiceObservationDisposition)99, null));
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
            OperationServiceComparisonDisposition.SourceMismatch,
            Compare(source: Source(dimension)).Disposition);
    }

    [Fact]
    public void EndpointMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed:
                Specification(uri: "https://example.com/other")).Disposition);
    }

    [Fact]
    public void SchemeMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed:
                Specification(uri: "http://example.com:443/health?ready=1"))
                .Disposition);
    }

    [Fact]
    public void EffectivePortMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed:
                Specification(uri: "https://example.com:8443/health?ready=1"))
                .Disposition);
    }

    [Fact]
    public void QueryMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed:
                Specification(uri: "https://example.com/health?ready=0"))
                .Disposition);
    }

    [Fact]
    public void MethodMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed: Specification(method: "HEAD")).Disposition);
    }

    [Fact]
    public void ExpectedCriteriaMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed: Specification(expectedStatus: 204)).Disposition);
    }

    [Fact]
    public void ProtocolMismatchIsNotAMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(observed:
                Specification(protocol: "grpc.health")).Disposition);
    }

    [Fact]
    public void EquivalentIndependentSpecificationsMatch()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.Matched,
            Compare(observed: Specification()).Disposition);
    }

    [Theory]
    [InlineData(200, 200, OperationServiceComparisonDisposition.Matched)]
    [InlineData(200, 201, OperationServiceComparisonDisposition.Mismatched)]
    [InlineData(404, 404, OperationServiceComparisonDisposition.Matched)]
    [InlineData(500, 500, OperationServiceComparisonDisposition.Matched)]
    [InlineData(200, 500, OperationServiceComparisonDisposition.Mismatched)]
    public void HttpStatusComparisonRequiresExactValue(
        int expected, int observed,
        OperationServiceComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(
                expected: expected.ToString(),
                declared: Specification(expectedStatus: expected),
                observed: Specification(expectedStatus: expected),
                observedStatus: observed).Disposition);
    }

    [Theory]
    [InlineData("service.health", "200")]
    [InlineData("service.http.status", "2xx")]
    [InlineData("service.http.status", "reachable")]
    [InlineData("service.http.status", "healthy")]
    [InlineData("service.http.status", "099")]
    public void UnsupportedConditionsNeverMatch(string key, string expected)
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.UnsupportedCondition,
            Compare(key: key, expected: expected).Disposition);
    }

    [Fact]
    public void ExpectedConditionMustAgreeWithDeclaredCriteria()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.UnsupportedCondition,
            Compare(expected: "201").Disposition);
    }

    [Fact]
    public void ConditionTokensIgnoreCase()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.Matched,
            Compare(key: "SERVICE.HTTP.STATUS").Disposition);
    }

    [Theory]
    [InlineData("grpc.health")]
    [InlineData("websocket.handshake")]
    [InlineData("smtp.banner")]
    [InlineData("database.query")]
    public void UnsupportedMechanismsCannotMatch(string mechanism)
    {
        var specification = Specification(protocol: mechanism);
        Assert.Equal(
            OperationServiceComparisonDisposition.UnsupportedProtocol,
            Compare(
                declared: specification,
                observed: Specification(protocol: mechanism)).Disposition);
    }

    [Theory]
    [InlineData(
        OperationServiceObservationDisposition.Unknown,
        OperationServiceComparisonDisposition.NoObservation)]
    [InlineData(
        OperationServiceObservationDisposition.Unavailable,
        OperationServiceComparisonDisposition.Unavailable)]
    [InlineData(
        OperationServiceObservationDisposition.NotAssessed,
        OperationServiceComparisonDisposition.NotAssessed)]
    [InlineData(
        OperationServiceObservationDisposition.UnsupportedCapability,
        OperationServiceComparisonDisposition.UnsupportedCapability)]
    public void NonresponseStatesAreNeverMatched(
        OperationServiceObservationDisposition state,
        OperationServiceComparisonDisposition result)
    {
        Assert.Equal(result,
            Compare(disposition: state,
                observedStatus: null).Disposition);
    }

    [Fact]
    public void SourceMismatchPrecedesInvalidCondition()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SourceMismatch,
            Compare(key: "invalid", source: Source(5)).Disposition);
    }

    [Fact]
    public void SpecificationMismatchPrecedesInvalidCondition()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SpecificationMismatch,
            Compare(key: "invalid",
                observed: Specification(method: "HEAD")).Disposition);
    }

    [Fact]
    public void InvalidConditionPrecedesUnsupportedProtocol()
    {
        var specification = Specification(protocol: "grpc.health");
        Assert.Equal(
            OperationServiceComparisonDisposition.UnsupportedCondition,
            Compare(key: "invalid", declared: specification,
                observed: Specification(protocol: "grpc.health"))
                .Disposition);
    }

    [Fact]
    public void UnsupportedProtocolPrecedesUnavailable()
    {
        var specification = Specification(protocol: "grpc.health");
        Assert.Equal(
            OperationServiceComparisonDisposition.UnsupportedProtocol,
            Compare(declared: specification,
                observed: Specification(protocol: "grpc.health"),
                disposition: OperationServiceObservationDisposition.Unavailable,
                observedStatus: null).Disposition);
    }

    [Fact]
    public void SourceCannotBeReversedIntoDestination()
    {
        Assert.Equal(
            OperationServiceComparisonDisposition.SourceMismatch,
            Compare(source: new OperationTarget(
                "provider-a", "account-a", "us-east-1",
                "application", "service", "example.com"))
                .Disposition);
    }

    [Fact]
    public void ObservationPreservesCorrelationAndInputs()
    {
        var operation = new OperationCorrelationId("service-operation-01");
        var source = Source();
        var specification = Specification();
        var observation = new OperationServiceObservation(
            operation, source, specification, Timestamp,
            OperationServiceObservationDisposition.Responded, 200);

        Assert.Equal(operation, observation.Operation);
        Assert.Same(source, observation.Source);
        Assert.Same(specification, observation.Specification);
        Assert.Equal(Timestamp, observation.ObservedAt);
        Assert.Equal(200, observation.HttpStatusCode);
    }

    [Fact]
    public void ObservationRejectsDefaultOperation()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationServiceObservation(
                default, Source(), Specification(), Timestamp,
                OperationServiceObservationDisposition.Responded, 200));
    }

    [Fact]
    public void ObservationRejectsDefaultTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationServiceObservation(
                new OperationCorrelationId("service-operation-01"),
                Source(), Specification(), default,
                OperationServiceObservationDisposition.Responded, 200));
    }

    [Fact]
    public void ObservationRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationServiceObservation(
                new OperationCorrelationId("service-operation-01"),
                null!, Specification(), Timestamp,
                OperationServiceObservationDisposition.Responded, 200));
    }

    [Fact]
    public void ObservationRejectsNullSpecification()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationServiceObservation(
                new OperationCorrelationId("service-operation-01"),
                Source(), null!, Timestamp,
                OperationServiceObservationDisposition.Responded, 200));
    }

    [Fact]
    public void SpecificationRejectsNullDependencies()
    {
        var endpoint = new OperationServiceEndpoint("https://example.com/");
        var protocol = new OperationServiceProtocol("http.response-status");
        var criteria = new OperationServiceResponseCriteria(200);

        Assert.Throws<ArgumentNullException>(() =>
            new OperationServiceCheckSpecification(
                null!, endpoint, "GET", criteria));
        Assert.Throws<ArgumentNullException>(() =>
            new OperationServiceCheckSpecification(
                protocol, null!, "GET", criteria));
        Assert.Throws<ArgumentNullException>(() =>
            new OperationServiceCheckSpecification(
                protocol, endpoint, "GET", null!));
    }

    [Fact]
    public void ComparisonPreservesExactInputs()
    {
        var condition =
            new OperationOutcomeCondition("service.http.status", "200");
        var scope = new OperationResourceScope(Source());
        var specification = Specification();
        var observation = Observation();

        var comparison = OperationServiceComparison.Evaluate(
            condition, scope, specification, observation);

        Assert.Same(condition, comparison.Condition);
        Assert.Same(scope, comparison.SourceScope);
        Assert.Same(specification, comparison.Specification);
        Assert.Same(observation, comparison.Observation);
        Assert.Equal(
            OperationServiceComparisonDisposition.Matched,
            comparison.Disposition);
    }

    [Fact]
    public void ComparisonRejectsNullInputs()
    {
        var condition =
            new OperationOutcomeCondition("service.http.status", "200");
        var scope = new OperationResourceScope(Source());
        var specification = Specification();
        var observation = Observation();

        Assert.Throws<ArgumentNullException>(() =>
            OperationServiceComparison.Evaluate(
                null!, scope, specification, observation));
        Assert.Throws<ArgumentNullException>(() =>
            OperationServiceComparison.Evaluate(
                condition, null!, specification, observation));
        Assert.Throws<ArgumentNullException>(() =>
            OperationServiceComparison.Evaluate(
                condition, scope, null!, observation));
        Assert.Throws<ArgumentNullException>(() =>
            OperationServiceComparison.Evaluate(
                condition, scope, specification, null!));
    }

    [Theory]
    [InlineData(typeof(OperationServiceProtocol))]
    [InlineData(typeof(OperationServiceEndpoint))]
    [InlineData(typeof(OperationServiceResponseCriteria))]
    [InlineData(typeof(OperationServiceCheckSpecification))]
    [InlineData(typeof(OperationServiceObservation))]
    [InlineData(typeof(OperationServiceComparison))]
    public void PublicContractClassesAreSealedAndImmutable(Type type)
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
    public void ComparisonHasOnlyOnePublicEvaluationMethod()
    {
        var type = typeof(OperationServiceComparison);
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
            typeof(OperationServiceCheckSpecification),
            typeof(OperationServiceObservation)
        }, method.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void ComparisonDoesNotClaimVerifiedOperationSuccess()
    {
        var type = typeof(OperationServiceComparison);

        Assert.Null(type.GetProperty("IsVerified"));
        Assert.Null(type.GetProperty("IsSuccessful"));
        Assert.Null(type.GetProperty("VerifiedSuccess"));
        Assert.Null(type.GetProperty("ExecutionSucceeded"));
        Assert.Null(type.GetProperty("Evidence"));
    }

    [Fact]
    public void ProtocolIdentityHasNoVendorDependency()
    {
        var type = typeof(OperationServiceProtocol);
        Assert.Null(type.GetProperty("Vendor"));
        Assert.Null(type.GetProperty("Provider"));
    }
}

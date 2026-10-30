using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Models.Platform;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class OperationVerificationEvidenceSetTests
{
    private static readonly DateTimeOffset Observed =
        new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Recorded =
        new(2026, 10, 30, 9, 1, 0, TimeSpan.Zero);

    private static OperationTarget Target(string id = "service-01")
        => new(
            "provider-01", "account-01", "region-01",
            "domain-01", "service", id);

    private static OperationResourceScope Scope(string id = "service-01")
        => new(Target(id));

    private static OperationVerificationEvidenceRecord Evidence(
        string evidenceId = "evidence-01",
        string operationId = "operation-01",
        string resourceId = "service-01",
        string checkId = "check-01")
    {
        var operation = new OperationCorrelationId(operationId);
        var target = Target(resourceId);
        var scope = new OperationResourceScope(target);
        var condition = new OperationOutcomeCondition(
            "service.http.status", "200");
        var specification = new OperationServiceCheckSpecification(
            new OperationServiceProtocol("http.response-status"),
            new OperationServiceEndpoint("https://example.com/health"),
            "GET",
            new OperationServiceResponseCriteria(200));
        var observation = new OperationServiceObservation(
            operation, target, specification, Observed,
            OperationServiceObservationDisposition.Responded, 200);
        var comparison = OperationServiceComparison.Evaluate(
            condition, scope, specification, observation);
        var result = OperationVerificationResult.FromService(
            checkId, comparison);

        return new OperationVerificationEvidenceRecord(
            evidenceId, "source-01", Recorded, result);
    }

    private static OperationVerificationEvidenceSet Create(
        IEnumerable<OperationVerificationEvidenceRecord> records,
        string operationId = "operation-01",
        string resourceId = "service-01")
        => new(
            new OperationCorrelationId(operationId),
            Scope(resourceId),
            records);

    [Fact]
    public void EvidenceSetTypeMustBePublic()
        => Assert.True(typeof(OperationVerificationEvidenceSet).IsPublic);

    [Fact]
    public void EvidenceSetTypeMustBeSealed()
        => Assert.True(typeof(OperationVerificationEvidenceSet).IsSealed);

    [Fact]
    public void ExactPublicConstructorSignature()
    {
        Assert.Single(
            typeof(OperationVerificationEvidenceSet).GetConstructors());

        Assert.NotNull(
            typeof(OperationVerificationEvidenceSet).GetConstructor(
                new[]
                {
                    typeof(OperationCorrelationId),
                    typeof(OperationResourceScope),
                    typeof(IEnumerable<OperationVerificationEvidenceRecord>)
                }));
    }

    [Fact]
    public void EmptyCollectionIsAllowed()
    {
        var set = Create(
            Array.Empty<OperationVerificationEvidenceRecord>());

        Assert.Empty(set.Records);
        Assert.Equal("operation-01", set.Operation.Value);
        Assert.Equal("service-01", set.Scope.Target.NativeId);
    }

    [Fact]
    public void SingleEvidenceRecordIsPreserved()
    {
        var record = Evidence();
        var set = Create(new[] { record });

        Assert.Single(set.Records);
        Assert.Same(record, set.Records[0]);
    }

    [Fact]
    public void DeclarationOrderIsPreserved()
    {
        var first = Evidence("evidence-01");
        var second = Evidence("evidence-02");
        var set = Create(new[] { first, second });

        Assert.Same(first, set.Records[0]);
        Assert.Same(second, set.Records[1]);
    }

    [Fact]
    public void InputListMutationDoesNotChangeSnapshot()
    {
        var source = new List<OperationVerificationEvidenceRecord>
        {
            Evidence("evidence-01")
        };
        var set = Create(source);

        source.Add(Evidence("evidence-02"));

        Assert.Single(set.Records);
    }

    [Fact]
    public void OutputCollectionRejectsMutation()
    {
        var set = Create(new[] { Evidence() });
        var list = Assert.IsAssignableFrom<
            IList<OperationVerificationEvidenceRecord>>(set.Records);

        Assert.Throws<NotSupportedException>(() =>
            list.Add(Evidence("evidence-02")));
    }

    [Fact]
    public void NullCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Create(null!));
    }

    [Fact]
    public void NullEntryIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(new OperationVerificationEvidenceRecord[] { null! }));
    }

    [Fact]
    public void DefaultOperationIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationVerificationEvidenceSet(
                default, Scope(),
                Array.Empty<OperationVerificationEvidenceRecord>()));
    }

    [Fact]
    public void NullScopeIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OperationVerificationEvidenceSet(
                new OperationCorrelationId("operation-01"),
                null!,
                Array.Empty<OperationVerificationEvidenceRecord>()));
    }

    [Fact]
    public void DuplicateEvidenceIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(new[]
            {
                Evidence("evidence-01"),
                Evidence("evidence-01")
            }));
    }

    [Fact]
    public void CrossOperationEvidenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(new[]
            {
                Evidence(
                    evidenceId: "evidence-02",
                    operationId: "other-operation")
            }));
    }

    [Fact]
    public void CrossResourceEvidenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Create(new[]
            {
                Evidence(
                    evidenceId: "evidence-02",
                    resourceId: "other-service")
            }));
    }

    [Fact]
    public void MultipleEvidenceRecordsMayShareOneCheckId()
    {
        var set = Create(new[]
        {
            Evidence("evidence-01", checkId: "check-01"),
            Evidence("evidence-02", checkId: "check-01")
        });

        Assert.Equal(2, set.Records.Count);
    }

    [Fact]
    public void DistinctEvidenceIdsRemainIndependent()
    {
        var set = Create(new[]
        {
            Evidence("evidence-01", checkId: "check-01"),
            Evidence("evidence-02", checkId: "check-02")
        });

        Assert.Equal(
            new[] { "evidence-01", "evidence-02" },
            set.Records.Select(x => x.EvidenceId).ToArray());
    }

    [Fact]
    public void NoPublicPropertyClaimsVerifiedSuccess()
    {
        var names = typeof(OperationVerificationEvidenceSet)
            .GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("VerifiedSuccess", names);
        Assert.DoesNotContain("ExecutionSucceeded", names);
        Assert.DoesNotContain("AuthenticatedProvenance", names);
    }
}

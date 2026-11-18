using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class
    G327A116B22C557V22PrincipalContinuityTests
{
    private const string Account = "123456789012";

    private static string Sts(
        string arn,
        string account = Account)
        => JsonSerializer.Serialize(
            new { Account = account, Arn = arn });

    private static string Role(
        string name,
        string session = "Session")
        => $"arn:aws:sts::{Account}:assumed-role/{name}/{session}";

    private static AwsCloudTrailPrincipalIdentity Parse(
        string json)
    {
        Assert.True(
            AwsCloudTrailPrincipalIdentity.TryParse(
                json, out var identity));

        return Assert.IsType<
            AwsCloudTrailPrincipalIdentity>(identity);
    }

    [Fact]
    public void SameRoleSessionRenewalPreservesPrincipal()
    {
        var original = Parse(
            Sts(Role("Engineering", "Initial")));

        var renewed = Parse(
            Sts(Role("Engineering", "Renewed")));

        Assert.True(original.SamePrincipal(renewed));
        Assert.Equal("assumed-role", original.PrincipalKind);
    }

    [Fact]
    public void DifferentRoleWithinAccountIsRejected()
    {
        var original = Parse(
            Sts(Role("Engineering")));

        var different = Parse(
            Sts(Role("Administration")));

        Assert.False(original.SamePrincipal(different));
    }

    [Fact]
    public void AccountMismatchFailsParsing()
    {
        string invalid = Sts(
            Role("Engineering"),
            "999988887777");

        Assert.False(
            AwsCloudTrailPrincipalIdentity.TryParse(
                invalid, out _));
    }

    [Fact]
    public void MalformedAndUnsupportedArnsFailClosed()
    {
        Assert.False(
            AwsCloudTrailPrincipalIdentity.TryParse(
                "{", out _));

        Assert.False(
            AwsCloudTrailPrincipalIdentity.TryParse(
                Sts(
                    $"arn:aws:sts::{Account}:federated-user/Test"),
                out _));

        Assert.False(
            AwsCloudTrailPrincipalIdentity.TryParse(
                Sts(
                    $"arn:aws:sts::{Account}:assumed-role/MissingSession"),
                out _));
    }

    [Fact]
    public void SameIamUserIsStable()
    {
        string identity = Sts(
            $"arn:aws:iam::{Account}:user/team/Alice");

        var first = Parse(identity);
        var second = Parse(identity);

        Assert.True(first.SamePrincipal(second));
        Assert.Equal("iam-user", first.PrincipalKind);
    }

    private static async Task<(
        AwsCloudTrailEnrichmentResult Result,
        int Lookups,
        int StsCalls)> Run(
            string beforeArn,
            string afterArn,
            AwsCloudTrailPrincipalIdentity expected)
    {
        int lookups = 0;
        int stsCalls = 0;

        Task<string> Runner(
            IReadOnlyList<string> arguments,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string command = string.Join(
                " ", arguments);

            if (command.Contains(
                "get-caller-identity",
                StringComparison.Ordinal))
            {
                int number =
                    Interlocked.Increment(ref stsCalls);

                return Task.FromResult(
                    Sts(number == 1
                        ? beforeArn
                        : afterArn));
            }

            if (command.Contains(
                "lookup-events",
                StringComparison.Ordinal))
            {
                Interlocked.Increment(ref lookups);

                return Task.FromResult(
                    """{"Events":[]}""");
            }

            throw new InvalidOperationException(
                "Unexpected synthetic AWS command.");
        }

        var orchestrator =
            new AwsCloudTrailAuthenticatedOrchestrator(
                Runner);

        var result = await orchestrator.FindCreationAsync(
            "test-profile",
            Account,
            "us-east-1",
            "virtual-network",
            "vpc-123",
            DateTimeOffset.UtcNow,
            CancellationToken.None,
            expectedPrincipal: expected);

        return (result, lookups, stsCalls);
    }

    [Fact]
    public async Task DifferentPreLookupPrincipalBlocksLookup()
    {
        var expected = Parse(
            Sts(Role("Engineering")));

        var actual = await Run(
            Role("Administration"),
            Role("Administration"),
            expected);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Equal(0, actual.Lookups);
        Assert.Equal(1, actual.StsCalls);
    }

    [Fact]
    public async Task PostLookupPrincipalDriftIsRejected()
    {
        var expected = Parse(
            Sts(Role("Engineering")));

        var actual = await Run(
            Role("Engineering"),
            Role("Administration"),
            expected);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.Unavailable,
            actual.Result.Status);

        Assert.Equal(1, actual.Lookups);
        Assert.Equal(2, actual.StsCalls);
    }

    [Fact]
    public async Task SameRoleRenewalAllowsCompletedLookup()
    {
        var expected = Parse(
            Sts(Role("Engineering", "Initial")));

        var actual = await Run(
            Role("Engineering", "Renewed"),
            Role("Engineering", "AnotherSession"),
            expected);

        Assert.Equal(
            AwsCloudTrailEnrichmentStatus.NoMatchingEvidence,
            actual.Result.Status);

        Assert.Equal(1, actual.Lookups);
        Assert.Equal(2, actual.StsCalls);
    }
}

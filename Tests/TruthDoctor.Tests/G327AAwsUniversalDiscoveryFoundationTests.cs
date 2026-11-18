using System;
using System.Collections.Generic;
using System.Linq;
using TruthDoctor.Services.Standalone;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class G327AAwsUniversalDiscoveryFoundationTests
{
    private static readonly AwsDiscoveryScope Ec2 =
        new("123456789012", "us-east-1", "ec2");

    private static readonly AwsDiscoveryScope S3 =
        new("123456789012", "global", "s3");

    private static AwsDiscoveredResource Resource(
        AwsDiscoveryScope scope,
        string id,
        string name,
        string type = "resource") =>
        new(
            new AwsDiscoveryResourceKey(scope, type, id),
            name);

    private static AwsDiscoveryScopeScan Success(
        AwsDiscoveryScope scope,
        params AwsDiscoveredResource[] resources) =>
        new(
            scope,
            AwsDiscoveryScanStatus.Success,
            resources);

    private static AwsDiscoveryScopeScan Failure(
        AwsDiscoveryScope scope,
        AwsDiscoveryScanStatus status) =>
        new(
            scope,
            status,
            Array.Empty<AwsDiscoveredResource>(),
            "SCAN_UNAVAILABLE");

    private static AwsDiscoveryInventoryTransition Apply(
        AwsDiscoveryInventorySnapshot previous,
        params AwsDiscoveryScopeScan[] scans) =>
        AwsDiscoveryInventoryReconciler.Reconcile(
            previous, scans);

    [Fact]
    public void FirstSuccessfulScanDiscoversResourcesAcrossServices()
    {
        var result = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(Ec2, Resource(Ec2, "vpc-001", "Network")),
            Success(S3, Resource(S3, "bucket-001", "Storage")));

        Assert.Equal(2, result.Added.Count);
        Assert.Empty(result.Removed);
        Assert.Equal(2, result.Snapshot.Resources.Count);
        Assert.Equal(2, result.Snapshot.Coverage.Count);
        Assert.Equal(1, result.Snapshot.Revision);
    }

    [Fact]
    public void SuccessfulRescanRemovesDeletedResources()
    {
        var first = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(Ec2, Resource(Ec2, "vpc-old", "Network")));

        var second = Apply(first.Snapshot, Success(Ec2));

        Assert.Empty(second.Snapshot.Resources);
        Assert.Empty(second.Added);
        Assert.Single(second.Removed);
        Assert.Equal("vpc-old", second.Removed[0].Key.ResourceId);
    }

    [Fact]
    public void RebuildWithSameNameDetectsDifferentResourceIdentity()
    {
        var first = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(Ec2, Resource(Ec2, "vpc-old", "Same Name")));

        var second = Apply(
            first.Snapshot,
            Success(Ec2, Resource(Ec2, "vpc-new", "Same Name")));

        Assert.Single(second.Added);
        Assert.Single(second.Removed);
        Assert.Equal("vpc-old", second.Removed[0].Key.ResourceId);
        Assert.Equal("vpc-new", second.Added[0].Key.ResourceId);
        Assert.Single(second.Snapshot.Resources);
    }

    [Fact]
    public void FailedScanPreservesPreviouslyDiscoveredResources()
    {
        var first = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(Ec2, Resource(Ec2, "vpc-001", "Network")));

        var second = Apply(
            first.Snapshot,
            Failure(Ec2, AwsDiscoveryScanStatus.Failed));

        Assert.Single(second.Snapshot.Resources);
        Assert.Empty(second.Added);
        Assert.Empty(second.Removed);
        Assert.Equal(
            AwsDiscoveryScanStatus.Failed,
            Assert.Single(second.Snapshot.Coverage).Status);
    }

    [Fact]
    public void AccessDeniedPreservesPreviouslyDiscoveredResources()
    {
        var first = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(S3, Resource(S3, "bucket-001", "Storage")));

        var second = Apply(
            first.Snapshot,
            Failure(S3, AwsDiscoveryScanStatus.AccessDenied));

        Assert.Single(second.Snapshot.Resources);
        Assert.Empty(second.Removed);
        Assert.Equal(
            AwsDiscoveryScanStatus.AccessDenied,
            Assert.Single(second.Snapshot.Coverage).Status);
    }

    [Fact]
    public void UnscannedServicesRemainUnchanged()
    {
        var first = Apply(
            AwsDiscoveryInventorySnapshot.Empty,
            Success(Ec2, Resource(Ec2, "vpc-001", "Network")),
            Success(S3, Resource(S3, "bucket-001", "Storage")));

        var second = Apply(
            first.Snapshot,
            Success(Ec2));

        Assert.Single(second.Removed);
        Assert.Single(second.Snapshot.Resources);
        Assert.Equal(
            "bucket-001",
            second.Snapshot.Resources[0].Key.ResourceId);
    }

    [Fact]
    public void DuplicateDiscoveryScopesAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Apply(
                AwsDiscoveryInventorySnapshot.Empty,
                Success(Ec2),
                Success(Ec2)));
    }

    [Fact]
    public void CrossScopeResourcesAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Apply(
                AwsDiscoveryInventorySnapshot.Empty,
                Success(Ec2, Resource(S3, "bucket-001", "Storage"))));
    }

    [Fact]
    public void FailedScansCannotPublishPartialResourceCollections()
    {
        var invalid = new AwsDiscoveryScopeScan(
            Ec2,
            AwsDiscoveryScanStatus.Failed,
            new List<AwsDiscoveredResource>
            {
                Resource(Ec2, "vpc-partial", "Partial")
            });

        Assert.Throws<InvalidOperationException>(() =>
            Apply(AwsDiscoveryInventorySnapshot.Empty, invalid));
    }
}

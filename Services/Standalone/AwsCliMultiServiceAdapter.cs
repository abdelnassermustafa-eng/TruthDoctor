using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Indicates a read-only AWS discovery command was denied.
/// The diagnostic never contains raw CLI stderr.
/// </summary>
public sealed class AwsDiscoveryAccessDeniedException : Exception
{
    public AwsDiscoveryAccessDeniedException()
        : base("AWS discovery access denied.")
    {
    }
}

/// <summary>
/// First operational AWS adapter increment. Uses an existing AWS CLI
/// profile and explicit regions. Does not acquire or store credentials.
/// Its command transport is injectable so tests never contact AWS.
/// </summary>
/// <summary>
/// Completed optional AWS CloudTrail enrichment diagnostics.
/// A completed report may still have NotExamined resources.
/// This is an adapter result, not workbench publication proof.
/// </summary>
public sealed record AwsCloudTrailCoverageReport(
    IReadOnlyList<AwsCloudTrailCoverageEntry> Entries,
    AwsCloudTrailCoverageSummary Summary);

public sealed class AwsCliMultiServiceAdapter :
    IStandaloneProviderAdapter
{
    private sealed record DiscoveryOperation(
        string Service,
        string Command,
        string Collection,
        string IdentityProperty,
        string ResourceType,
        string ScopeName,
        bool Global = false);

    private static readonly DiscoveryOperation[] Operations =
    [
        new(
            "ec2", "describe-vpcs", "Vpcs", "VpcId",
            "virtual-network", "ec2:vpc"),
        new(
            "ec2", "describe-subnets", "Subnets", "SubnetId",
            "subnet", "ec2:subnet"),
        new(
            "ec2", "describe-instances", "Reservations",
            "InstanceId", "compute-instance", "ec2:instance"),
        new(
            "rds", "describe-db-instances", "DBInstances",
            "DBInstanceIdentifier", "database-instance",
            "rds:db-instance"),
        new(
            "lambda", "list-functions", "Functions",
            "FunctionName", "function", "lambda:function"),
        new(
            "s3api", "list-buckets", "Buckets", "Name",
            "storage-bucket", "s3:bucket", Global: true)
    ];


    private static readonly DiscoveryOperation[]
        ExtendedNetworkingOperations =
    [
        new(
            "ec2", "describe-route-tables",
            "RouteTables", "RouteTableId",
            "route-table", "ec2:route-table"),
        new(
            "ec2", "describe-internet-gateways",
            "InternetGateways", "InternetGatewayId",
            "internet-gateway", "ec2:internet-gateway"),
        new(
            "ec2", "describe-nat-gateways",
            "NatGateways", "NatGatewayId",
            "nat-gateway", "ec2:nat-gateway"),
        new(
            "ec2", "describe-security-groups",
            "SecurityGroups", "GroupId",
            "security-group", "ec2:security-group"),
        new(
            "ec2", "describe-network-acls",
            "NetworkAcls", "NetworkAclId",
            "network-acl", "ec2:network-acl"),
        new(
            "ec2", "describe-network-interfaces",
            "NetworkInterfaces", "NetworkInterfaceId",
            "network-interface", "ec2:network-interface"),
        new(
            "ec2", "describe-volumes",
            "Volumes", "VolumeId",
            "volume", "ec2:volume")
    ];

    private readonly string _connectionId;
    private readonly string _profile;
    private readonly string[] _regions;
    private readonly bool _includeNetworking;
    private readonly bool _enableCloudTrailCreationEvidence;
    private const int MaxCloudTrailEnrichmentResources = 12;
    private readonly Func<IReadOnlyList<string>,
        CancellationToken, Task<string>> _runner;

    private string? _accountId;
    private AwsCloudTrailPrincipalIdentity?
        _authenticatedCloudTrailPrincipal;
    // Synchronizes identity transitions, discovery generation,
    // and completed CloudTrail report publication.
    private readonly object _cloudTrailReportSync = new();
    private long _cloudTrailGeneration;
    private AwsCloudTrailCoverageReport? _lastCloudTrailCoverageReport;

    public AwsCloudTrailCoverageReport? LastCloudTrailCoverageReport
    {
        get
        {
            lock (_cloudTrailReportSync)
            {
                return _lastCloudTrailCoverageReport;
            }
        }
    }

    private void RequireCurrentCloudTrailGeneration(
        long generation,
        string account)
    {
        lock (_cloudTrailReportSync)
        {
            if (_cloudTrailGeneration != generation ||
                !StringComparer.Ordinal.Equals(
                    _accountId, account))
            {
                throw new InvalidOperationException(
                    "AWS enrichment belongs to a superseded session.");
            }
        }
    }

    public string ProviderId => "aws";

    /// <summary>
    /// Account verified by AWS STS, never a guessed identifier.
    /// </summary>
    public string? AuthenticatedAccountId => _accountId;

    public AwsDiscoveryInventoryTransition? LastTransition
    {
        get;
        private set;
    }

    public AwsCliMultiServiceAdapter(
        string connectionId,
        string profile,
        IEnumerable<string> regions,
        Func<IReadOnlyList<string>,
            CancellationToken, Task<string>>? runner = null,
        bool includeNetworking = false,
        bool enableCloudTrailCreationEvidence = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile);
        ArgumentNullException.ThrowIfNull(regions);

        if (!SafeSelector(profile))
        {
            throw new ArgumentException(
                "AWS profile selector is invalid.", nameof(profile));
        }

        _regions = regions.ToArray();

        if (_regions.Length == 0
            || _regions.Any(region => !SafeSelector(region))
            || _regions.Distinct(StringComparer.Ordinal).Count()
                != _regions.Length)
        {
            throw new ArgumentException(
                "AWS regions must be explicit, valid and unique.",
                nameof(regions));
        }

        _connectionId = connectionId;
        _profile = profile;
        _runner = runner ?? RunAwsCliAsync;
        _includeNetworking = includeNetworking;
        _enableCloudTrailCreationEvidence =
            enableCloudTrailCreationEvidence;
    }

    private static bool SafeSelector(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 128
        && value.All(c =>
            char.IsAsciiLetterOrDigit(c)
            || c is '_' or '-' or '.' or '/');

    private void VerifyConnection(string connectionId)
    {
        if (!StringComparer.Ordinal.Equals(
            connectionId, _connectionId))
        {
            throw new InvalidOperationException(
                "AWS provider connection identity mismatch.");
        }
    }

    private async Task<string> CommandAsync(
        string region,
        string service,
        string operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // No shell or interpolated command string is involved.
        IReadOnlyList<string> arguments =
        [
            "--profile", _profile,
            "--region", region,
            "--no-cli-pager",
            service, operation,
            "--output", "json"
        ];

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(90));

        return await _runner(
            arguments, timeout.Token).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>>
        ListAvailableRegionsAsync(
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string json = await CommandAsync(
            _regions[0],
            "ec2",
            "describe-regions",
            cancellationToken).ConfigureAwait(false);

        return AwsRegionCatalog.Parse(json);
    }

    public async Task AuthenticateAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        VerifyConnection(connectionId);

        long authenticationGeneration;

        lock (_cloudTrailReportSync)
        {
            authenticationGeneration = ++_cloudTrailGeneration;
            _lastCloudTrailCoverageReport = null;
            _accountId = null;
            _authenticatedCloudTrailPrincipal = null;
        }
        LastTransition = null;

        string output = await CommandAsync(
            _regions[0],
            "sts",
            "get-caller-identity",
            cancellationToken).ConfigureAwait(false);

        if (!AwsCloudTrailPrincipalIdentity.TryParse(
                output, out var principal) ||
            principal is null)
        {
            throw new InvalidOperationException(
                "AWS identity response is invalid.");
        }

        string account = principal.AccountId;

        lock (_cloudTrailReportSync)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_cloudTrailGeneration != authenticationGeneration)
            {
                throw new InvalidOperationException(
                    "AWS authentication was superseded.");
            }

            _accountId = account;
            _authenticatedCloudTrailPrincipal = principal;
        }
    }

    public Task DisconnectAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        VerifyConnection(connectionId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cloudTrailReportSync)
        {
            ++_cloudTrailGeneration;
            _lastCloudTrailCoverageReport = null;
            _accountId = null;
            _authenticatedCloudTrailPrincipal = null;
        }
        LastTransition = null;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Produces a scope-aware inventory transition.
    /// Failed scopes retain prior resources; coverage records the
    /// failure rather than incorrectly reporting resource deletion.
    /// </summary>
    public async Task<AwsDiscoveryInventoryTransition>
        DiscoverInventoryAsync(
            string connectionId,
            AwsDiscoveryInventorySnapshot previous,
            CancellationToken cancellationToken = default)
    {
        VerifyConnection(connectionId);
        ArgumentNullException.ThrowIfNull(previous);
        cancellationToken.ThrowIfCancellationRequested();

        string account = _accountId ??
            throw new InvalidOperationException(
                "AWS authentication is required before discovery.");

        var scans = new List<AwsDiscoveryScopeScan>();

        var regionalOperations = _includeNetworking
            ? Operations.Where(x => !x.Global)
                .Concat(ExtendedNetworkingOperations)
            : Operations.Where(x => !x.Global);

        foreach (string region in _regions)
        {
            foreach (DiscoveryOperation operation
                in regionalOperations)
            {
                scans.Add(
                    await ScanAsync(
                        account,
                        region,
                        operation,
                        cancellationToken).ConfigureAwait(false));
            }
        }

        // Global services are scanned once, not once per region.
        foreach (DiscoveryOperation operation
            in Operations.Where(x => x.Global))
        {
            scans.Add(
                await ScanAsync(
                    account,
                    "global",
                    operation,
                    cancellationToken).ConfigureAwait(false));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var transition =
            AwsDiscoveryInventoryReconciler.Reconcile(
                previous, scans);

        LastTransition = transition;
        return transition;
    }

    public async Task<IReadOnlyList<ProviderResourceRecord>>
        DiscoverAsync(
            string connectionId,
            CancellationToken cancellationToken)
    {
        long discoveryGeneration;

        lock (_cloudTrailReportSync)
        {
            discoveryGeneration = ++_cloudTrailGeneration;
            _lastCloudTrailCoverageReport = null;
        }

        var transition = await DiscoverInventoryAsync(
            connectionId,
            AwsDiscoveryInventorySnapshot.Empty,
            cancellationToken).ConfigureAwait(false);

        // The existing provider-neutral contract cannot communicate
        // partial scan coverage. Fail closed until the desktop
        // coverage-aware publication path is integrated.
        if (transition.Snapshot.Coverage.Any(
            c => c.Status != AwsDiscoveryScanStatus.Success))
        {
            throw new InvalidOperationException(
                "AWS discovery was incomplete; inspect scope coverage.");
        }

        ProviderResourceRecord[] records = transition.Snapshot.Resources
            .Select(resource => new ProviderResourceRecord(
                "aws",
                resource.Key.ResourceId,
                resource.Key.ResourceType,
                resource.DisplayName,
                resource.State,
                resource.AvailabilityZone,
                resource.Arn,
                NormalizeSnapshotNativeLifecycle(
                    resource.Properties),
                resource.Tags))
            .ToArray();

        // Existing discovery remains unchanged by default.
        if (!_enableCloudTrailCreationEvidence)
            return records;

        string account;
        AwsCloudTrailPrincipalIdentity principal;

        lock (_cloudTrailReportSync)
        {
            account = _accountId ??
                throw new InvalidOperationException(
                    "AWS authentication is required for enrichment.");

            principal = _authenticatedCloudTrailPrincipal ??
                throw new InvalidOperationException(
                    "AWS principal authentication is required.");
        }

        // Discovery coverage is already validated above.
        // The ledger owns the budget and selection policy.
        var ledger = new AwsCloudTrailEnrichmentCoverageLedger(
            transition.Snapshot,
            account,
            AwsCloudTrailEnrichmentCoverageLedger
                .DefaultResourceBudget);

        var scheduled = new HashSet<AwsDiscoveryResourceKey>(
            ledger.ScheduledKeys);

        var enrichment =
            new AwsCloudTrailAuthenticatedOrchestrator(_runner);

        for (int index = 0; index < records.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AwsDiscoveredResource resource =
                transition.Snapshot.Resources[index];

            AwsDiscoveryResourceKey key = resource.Key;

            if (!scheduled.Contains(key))
                continue;

            RequireCurrentCloudTrailGeneration(discoveryGeneration, account);

            AwsCloudTrailEnrichmentStatus outcome =
                AwsCloudTrailEnrichmentStatus.Unavailable;

            try
            {
                AwsCloudTrailEnrichmentResult result =
                    await enrichment.FindCreationAsync(
                        _profile,
                        account,
                        key.Scope.Region,
                        key.ResourceType,
                        key.ResourceId,
                        DateTimeOffset.UtcNow,
                        cancellationToken,
                        expectedPrincipal: principal)
                        .ConfigureAwait(false);

                if (result.Status ==
                        AwsCloudTrailEnrichmentStatus.EvidenceFound &&
                    result.Evidence is not null)
                {
                    var binding = new AwsCreationEvidenceBinding(
                        key, result.Evidence);

                    // Associate first, report EvidenceFound only
                    // after association validation succeeds.
                    records[index] =
                        AwsCreationEvidenceAssociation.Apply(
                            records[index],
                            key,
                            binding);

                    outcome =
                        AwsCloudTrailEnrichmentStatus.EvidenceFound;
                }
                else if (result.Status ==
                    AwsCloudTrailEnrichmentStatus.NoMatchingEvidence)
                {
                    outcome =
                        AwsCloudTrailEnrichmentStatus.NoMatchingEvidence;
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Optional enrichment cannot erase valid discovery.
                // Never report unassociated evidence as verified.
                outcome = AwsCloudTrailEnrichmentStatus.Unavailable;
            }

            cancellationToken.ThrowIfCancellationRequested();

            RequireCurrentCloudTrailGeneration(discoveryGeneration, account);

            ledger.RecordOutcome(key, outcome);
        }

        cancellationToken.ThrowIfCancellationRequested();

        RequireCurrentCloudTrailGeneration(discoveryGeneration, account);

        // Publish diagnostics only after all scheduled lookups
        // have terminal outcomes. NotExamined remains explicit.
        if (ledger.Summary.Pending != 0)
            throw new InvalidOperationException(
                "CloudTrail coverage accounting is incomplete.");

        var completedReport = new AwsCloudTrailCoverageReport(
            ledger.Entries,
            ledger.Summary);

        // This property describes the completed adapter result.
        // The workbench retains its own publication authorization.
        // Authorize and publish the completed report inside
        // the same session-transition synchronization boundary.
        lock (_cloudTrailReportSync)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_cloudTrailGeneration != discoveryGeneration ||
                !StringComparer.Ordinal.Equals(
                    _accountId, account))
            {
                throw new InvalidOperationException(
                    "AWS enrichment belongs to a superseded session.");
            }

            _lastCloudTrailCoverageReport = completedReport;
        }

        return records;
    }

    private async Task<AwsDiscoveryScopeScan> ScanAsync(
        string account,
        string region,
        DiscoveryOperation operation,
        CancellationToken cancellationToken)
    {
        var scope = new AwsDiscoveryScope(
            account, region, operation.ScopeName);

        try
        {
            // AWS CLI performs normal automatic pagination for
            // supported paginated operations. No --no-paginate,
            // --max-items, or --no-cli-auto-prompt is used here.
            string output = await CommandAsync(
                region == "global" ? _regions[0] : region,
                operation.Service,
                operation.Command,
                cancellationToken).ConfigureAwait(false);

            var resources = ParseResources(
                output, scope, operation);

            return new AwsDiscoveryScopeScan(
                scope,
                AwsDiscoveryScanStatus.Success,
                resources);
        }
        catch (AwsDiscoveryAccessDeniedException)
        {
            return new AwsDiscoveryScopeScan(
                scope,
                AwsDiscoveryScanStatus.AccessDenied,
                Array.Empty<AwsDiscoveredResource>(),
                "ACCESS_DENIED");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Do not expose raw CLI stderr or sensitive provider
            // diagnostic data in inventory or user-visible errors.
            return new AwsDiscoveryScopeScan(
                scope,
                AwsDiscoveryScanStatus.Failed,
                Array.Empty<AwsDiscoveredResource>(),
                "SCAN_FAILED");
        }
    }

    private static IReadOnlyList<AwsDiscoveredResource>
        ParseResources(
            string json,
            AwsDiscoveryScope scope,
            DiscoveryOperation operation)
    {
        using var document = JsonDocument.Parse(json);

        JsonElement collection =
            document.RootElement.GetProperty(
                operation.Collection);

        if (collection.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "AWS response collection is invalid.");
        }

        var resources = new List<AwsDiscoveredResource>();

        foreach (JsonElement item in collection.EnumerateArray())
        {
            // EC2 describe-instances nests instances in Reservations.
            if (operation.Command == "describe-instances")
            {
                JsonElement instances =
                    item.GetProperty("Instances");

                foreach (JsonElement instance
                    in instances.EnumerateArray())
                {
                    resources.Add(
                        MakeResource(
                            instance, scope, operation));
                }

                continue;
            }

            resources.Add(
                MakeResource(item, scope, operation));
        }

        return resources;
    }

    private static AwsDiscoveredResource MakeResource(
        JsonElement item,
        AwsDiscoveryScope scope,
        DiscoveryOperation operation)
    {
        string id = item.GetProperty(
            operation.IdentityProperty).GetString() ?? "";

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                "AWS resource identifier is missing.");
        }

        string displayName = id;

        if (item.TryGetProperty("Tags", out var tags)
            && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement tag in tags.EnumerateArray())
            {
                if (tag.TryGetProperty("Key", out var key)
                    && key.GetString() == "Name"
                    && tag.TryGetProperty("Value", out var value)
                    && !string.IsNullOrWhiteSpace(
                        value.GetString()))
                {
                    displayName = value.GetString()!;
                    break;
                }
            }
        }

        // Normalize every discovered AWS resource through
        // the same service-independent metadata pipeline.
        var metadata =
            AwsUniversalMetadataNormalizer.Normalize(item);

        return new AwsDiscoveredResource(
            new AwsDiscoveryResourceKey(
                scope,
                operation.ResourceType,
                id),
            displayName,
            ReadResourceState(item),
            ReadAvailabilityZone(item),
            ReadArn(item),
            metadata.Properties,
            metadata.Tags);
    }


    private static string ReadString(
        JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value))
            return "";

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => ""
        };
    }

    private static string ReadNestedString(
        JsonElement item, string parent, string child)
    {
        if (!item.TryGetProperty(parent, out var nested)
            || nested.ValueKind != JsonValueKind.Object)
            return "";

        return ReadString(nested, child);
    }


    // Normalize metadata on the actual discovery snapshot
    // projection path, without altering audit evidence.
    private static IReadOnlyDictionary<string, string>
        NormalizeSnapshotNativeLifecycle(
            IReadOnlyDictionary<string, string> original)
    {
        var properties =
            new Dictionary<string, string>(
                original,
                StringComparer.Ordinal);

        string? rawTimestamp = null;
        string? label = null;
        string? source = null;

        if (properties.TryGetValue(
                "LaunchTime", out var launch))
        {
            rawTimestamp = launch;
            label = "Launched";
            source = "AWS LaunchTime";
        }
        else if (properties.TryGetValue(
                     "CreateTime", out var created))
        {
            rawTimestamp = created;
            label = "Created";
            source = "AWS CreateTime";
        }

        if (rawTimestamp is not null &&
            DateTimeOffset.TryParse(
                rawTimestamp,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out _))
        {
            properties["NativeLifecycle.CreationTimestamp"] =
                rawTimestamp;
            properties["NativeLifecycle.CreationLabel"] =
                label!;
            properties["NativeLifecycle.CreationSource"] =
                source!;
        }

        return properties;
    }

    private static IReadOnlyDictionary<string, string>
        ExtractSafeProperties(JsonElement item)
    {
        // Explicit allowlist. Do not copy arbitrary AWS JSON
        // fields into the workbench or diagnostic logs.
        string[] allowed =
        [
            "VpcId", "SubnetId", "CidrBlock",
            "InstanceType", "Architecture", "ImageId",
            "PrivateIpAddress", "PublicIpAddress",
            "LaunchTime", "CreateTime", "RootDeviceType",
            "AvailabilityZone", "DBInstanceClass",
            "DBInstanceStatus", "Engine", "EngineVersion",
            "AllocatedStorage", "Runtime", "Handler",
            "LastModified", "FunctionArn", "DBInstanceArn"
        ];

        var properties = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (string key in allowed)
        {
            string value = ReadString(item, key);
            if (!string.IsNullOrWhiteSpace(value))
                properties[key] = value;
        }


        // Provider-reported creation/launch metadata.
        // Do not convert native timestamps into verified
        // audit evidence.
        if (properties.TryGetValue(
                "LaunchTime", out var launchTime))
        {
            properties["NativeLifecycle.CreationTimestamp"] =
                launchTime;
            properties["NativeLifecycle.CreationLabel"] =
                "Launched";
            properties["NativeLifecycle.CreationSource"] =
                "AWS LaunchTime";
        }
        else if (properties.TryGetValue(
                     "CreateTime", out var createTime))
        {
            properties["NativeLifecycle.CreationTimestamp"] =
                createTime;
            properties["NativeLifecycle.CreationLabel"] =
                "Created";
            properties["NativeLifecycle.CreationSource"] =
                "AWS CreateTime";
        }

return properties;
    }

    private static IReadOnlyDictionary<string, string>
        ExtractSafeTags(JsonElement item)
    {
        var result = new Dictionary<string, string>(
            StringComparer.Ordinal);

        if (!item.TryGetProperty("Tags", out var tags)
            || tags.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var tag in tags.EnumerateArray())
        {
            if (tag.ValueKind != JsonValueKind.Object)
                continue;

            string key = ReadString(tag, "Key");
            string value = ReadString(tag, "Value");

            // Keys and values may contain sensitive data.
            // Accept only explicitly designated display tags.
            if (key is "Name" or "Environment"
                or "Project" or "Owner"
                or "CostCenter")
            {
                if (value.Length <= 256)
                    result[key] = value;
            }
        }

        return result;
    }

    private static string ReadResourceState(JsonElement item)
    {
        string state = ReadString(item, "State");

        if (string.IsNullOrWhiteSpace(state))
            state = ReadNestedString(item, "State", "Name");

        if (string.IsNullOrWhiteSpace(state))
            state = ReadString(item, "DBInstanceStatus");

        if (string.IsNullOrWhiteSpace(state))
            state = ReadString(item, "Status");

        return state;
    }

    private static string ReadAvailabilityZone(JsonElement item)
    {
        string zone = ReadString(item, "AvailabilityZone");

        if (string.IsNullOrWhiteSpace(zone))
            zone = ReadNestedString(
                item, "Placement", "AvailabilityZone");

        return zone;
    }

    private static string ReadArn(JsonElement item)
    {
        foreach (string key in new[]
        {
            "FunctionArn", "DBInstanceArn"
        })
        {
            string value = ReadString(item, key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return "";
    }

    private static async Task<string> RunAwsCliAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("aws")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        start.Environment["AWS_PAGER"] = "";

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = start
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "AWS CLI could not be started.");
        }

        try
        {
            Task<string> stdout =
                process.StandardOutput.ReadToEndAsync(
                    cancellationToken);

            Task<string> stderr =
                process.StandardError.ReadToEndAsync(
                    cancellationToken);

            await process.WaitForExitAsync(
                cancellationToken).ConfigureAwait(false);

            string output =
                await stdout.ConfigureAwait(false);

            string diagnostic =
                await stderr.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                // Classify failures before exposing exceptions to
                // discovery or CloudTrail orchestration.
                //
                // STS/CloudTrail errors use sanitized transport.
                // Other discovery behavior remains unchanged.
                throw AwsCliFailureRouter.CreateException(
                    arguments, diagnostic);
            }

            return output;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            throw;
        }
    }
}

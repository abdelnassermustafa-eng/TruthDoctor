using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TruthDoctor.Services;

namespace TruthDoctor;

public partial class ValidationWindow : Window
{
    private readonly ApiService? _apiService;

    private System.Timers.Timer? _autoRefreshTimer;
    private bool _refreshInProgress = false;
    private const int AutoRefreshIntervalMs = 20000;

    // Required by Avalonia runtime loader
    public ValidationWindow()
    {
        InitializeComponent();
    }

    // Used by your application logic
    public ValidationWindow(string json, ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        LoadValidation(json);
        StartAutoRefresh();
    }


    private async void OnRefreshClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var scoreText = this.FindControl<TextBlock>("ScoreText");

        if (_apiService == null)
        {
            if (scoreText != null)
                scoreText.Text = "Refresh unavailable (no API connection)";
            return;
        }

        try
        {
            var json = await _apiService.GetValidationAsync();
            LoadValidation(json);
        }
        catch (Exception ex)
        {
            if (scoreText != null)
                scoreText.Text = "Refresh failed: " + ex.Message;
        }
    }


    private void LoadValidation(string json)
    {
        var scoreText = this.FindControl<TextBlock>("ScoreText");
        var passText = this.FindControl<TextBlock>("PassText");
        var failText = this.FindControl<TextBlock>("FailText");
        var timestampText = this.FindControl<TextBlock>("TimestampText");
        var statusText = this.FindControl<TextBlock>("ConnectionStatusText");
        var resultsGrid = this.FindControl<DataGrid>("ResultsGrid");
        var vpcGrid = this.FindControl<DataGrid>("VpcGrid");
        var subnetGrid = this.FindControl<DataGrid>("SubnetGrid");
        var routeGrid = this.FindControl<DataGrid>("RouteTableGrid");
        var instanceGrid = this.FindControl<DataGrid>("InstanceGrid");
        var natGatewayGrid = this.FindControl<DataGrid>("NatGatewayGrid");

        var igwGrid = this.FindControl<DataGrid>("IgwGrid");
if (scoreText == null || passText == null ||
            failText == null || resultsGrid == null || statusText == null)
            return;

        try
        {
            if (json.StartsWith("Error"))
            {
                statusText.Text = "🔴 API unreachable";
                return;
            }

            var doc = JsonDocument.Parse(json);

            statusText.Text = "🟢 Connected";

            // Extract data object
            var data = doc.RootElement.GetProperty("data");

            // Timestamp
            if (timestampText != null &&
                doc.RootElement.TryGetProperty("timestamp", out var ts))
            {
                var rawTs = ts.GetString() ?? "";
                if (DateTime.TryParse(rawTs, out var parsed))
                    timestampText.Text = "Last checked: " +
                        parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                else
                    timestampText.Text = "Last checked: " + rawTs;
            }

            var summary = data.GetProperty("summary");
            var results = data.GetProperty("results");

            int score = summary.GetProperty("score").GetInt32();
            int pass = summary.GetProperty("pass").GetInt32();
            int fail = summary.GetProperty("fail").GetInt32();

            if (scoreText != null)
                scoreText.Text = $"Score: {score}%";

            if (passText != null)
                passText.Text = $"Pass: {pass}";

            if (failText != null)
                failText.Text = $"Fail: {fail}";

            var rows = new List<ValidationRow>();

            // ==============================
            // Validation results
            // ==============================
            foreach (var r in results.EnumerateArray())
            {
                rows.Add(new ValidationRow
                {
                    Category = r.GetProperty("category").GetString() ?? "",
                    Rule = r.GetProperty("rule").GetString() ?? "",
                    ResourceType = r.GetProperty("resourceType").GetString() ?? "",
                    ResourceId = r.GetProperty("resourceId").GetString() ?? "",
                    Region = r.GetProperty("region").GetString() ?? "",
                    CidrOrDestination = r.GetProperty("cidrOrDestination").GetString() ?? "",
                    RouteTableId = r.GetProperty("routeTableId").GetString() ?? "",
                    Status = r.GetProperty("status").GetString() ?? "",
                    Severity = r.GetProperty("severity").GetString() ?? "",
                    Message = r.GetProperty("message").GetString() ?? ""
                });
            }

            // ==============================
            // VPC inventory
            // ==============================
            if (data.TryGetProperty("vpcs", out var vpcs))
            {
                foreach (var v in vpcs.EnumerateArray())
                {
                    rows.Add(new ValidationRow
                    {
                        Category = "Inventory",
                        Rule = "VPC",
                        ResourceType = "VPC",
                        ResourceId = v.GetProperty("vpcId").GetString() ?? "",
                        Region = v.GetProperty("region").GetString() ?? "",
                        CidrOrDestination = v.GetProperty("cidr").GetString() ?? "",
                        RouteTableId = "",
                        Status = v.GetProperty("state").GetString() ?? "",
                        Severity = "INFO",
                        Message = "VPC resource"
                    });
                }
            }

            // ==============================
            // Subnet inventory
            // ==============================
            if (data.TryGetProperty("subnets", out var subnets))
            {
                foreach (var s in subnets.EnumerateArray())
                {
                    rows.Add(new ValidationRow
                    {
                        Category = "Inventory",
                        Rule = "Subnet",
                        ResourceType = "Subnet",
                        ResourceId = s.GetProperty("subnetId").GetString() ?? "",
                        Region = s.GetProperty("region").GetString() ?? "",
                        CidrOrDestination = s.GetProperty("cidr").GetString() ?? "",
                        RouteTableId = s.GetProperty("vpcId").GetString() ?? "",
                        Status = s.GetProperty("state").GetString() ?? "",
                        Severity = "INFO",
                        Message = "Subnet in AZ: " +
                                  (s.GetProperty("availabilityZone").GetString() ?? "")
                    });
                }
            }

            // ==============================
            // Route table inventory
            // ==============================
            if (data.TryGetProperty("routeTables", out var routeTables))
            {
                foreach (var rt in routeTables.EnumerateArray())
                {
                    rows.Add(new ValidationRow
                    {
                        Category = "Inventory",
                        Rule = "RouteTable",
                        ResourceType = "RouteTable",
                        ResourceId = rt.GetProperty("routeTableId").GetString() ?? "",
                        Region = rt.GetProperty("region").GetString() ?? "",
                        CidrOrDestination = rt.GetProperty("destinationCidr").GetString() ?? "",
                        RouteTableId = rt.GetProperty("vpcId").GetString() ?? "",
                        Status = rt.GetProperty("associationType").GetString() ?? "",
                        Severity = "INFO",
                        Message = rt.GetProperty("isMain").GetBoolean()
                            ? "Main route table"
                            : "Non-main route table"
                    });
                }
            }

            resultsGrid.ItemsSource = rows;


            // ==============================
            // Strongly-typed inventory binding
            // ==============================

            if (data.TryGetProperty("vpcs", out var vpcData) && vpcGrid != null)
            {
                var vpcRows = new List<VpcInventoryRow>();
                foreach (var v in vpcData.EnumerateArray())
                {
                    vpcRows.Add(new VpcInventoryRow
                    {
                        VpcId = v.GetProperty("vpcId").GetString() ?? "",
                        Cidr = v.GetProperty("cidr").GetString() ?? "",
                        State = v.GetProperty("state").GetString() ?? "",
                        Region = v.GetProperty("region").GetString() ?? ""
                    });
                }
                vpcGrid.ItemsSource = vpcRows;
            }

            if (data.TryGetProperty("subnets", out var subnetData) && subnetGrid != null)
            {
                var subnetRows = new List<SubnetInventoryRow>();
                foreach (var s in subnetData.EnumerateArray())
                {
                    subnetRows.Add(new SubnetInventoryRow
                    {
                        SubnetId = s.GetProperty("subnetId").GetString() ?? "",
                        VpcId = s.GetProperty("vpcId").GetString() ?? "",
                        Cidr = s.GetProperty("cidr").GetString() ?? "",
                        AvailabilityZone = s.GetProperty("availabilityZone").GetString() ?? "",
                        State = s.GetProperty("state").GetString() ?? "",
                        Action = "",
                        Region = s.GetProperty("region").GetString() ?? ""
                    });
                }
                subnetGrid.ItemsSource = subnetRows;
            }

            if (data.TryGetProperty("routeTables", out var rtData) && routeGrid != null)
            {
                var rtRows = new List<RouteTableInventoryRow>();
                foreach (var rt in rtData.EnumerateArray())
                {
                    rtRows.Add(new RouteTableInventoryRow
                    {
                        RouteTableId = rt.GetProperty("routeTableId").GetString() ?? "",
                        VpcId = rt.GetProperty("vpcId").GetString() ?? "",
                        IsMain = rt.GetProperty("isMain").GetBoolean(),
                        AssociationType = rt.GetProperty("associationType").GetString() ?? "",
                        AssociatedSubnetId = rt.GetProperty("associatedSubnetId").GetString() ?? "",
                        DestinationCidr = rt.GetProperty("destinationCidr").GetString() ?? "",
                        Region = rt.GetProperty("region").GetString() ?? ""
                    });
                }
                routeGrid.ItemsSource = rtRows;
            }
            // ==============================
            // EC2 instance inventory
            // ==============================
            if (data.TryGetProperty("instances", out var instanceData) && instanceGrid != null)
            {
                var instanceRows = new List<object>();
                foreach (var i in instanceData.EnumerateArray())
                {
                    instanceRows.Add(new
                    {
                        InstanceId = i.GetProperty("instanceId").GetString() ?? "",
                        InstanceType = i.GetProperty("instanceType").GetString() ?? "",
                        State = i.GetProperty("state").GetString() ?? "",
                        SubnetId = i.GetProperty("subnetId").GetString() ?? "",
                        VpcId = i.GetProperty("vpcId").GetString() ?? "",
                        AvailabilityZone = i.GetProperty("availabilityZone").GetString() ?? ""
                    });
                }
                instanceGrid.ItemsSource = instanceRows;
            // ==============================
            // NAT Gateway inventory
            // ==============================
            if (data.TryGetProperty("natGateways", out var natData) && natGatewayGrid != null)
            {
                var natRows = new List<object>();
                foreach (var n in natData.EnumerateArray())
                {
                    natRows.Add(new
                    {
                        NatGatewayId = n.GetProperty("natGatewayId").GetString() ?? "",
                        State = n.GetProperty("state").GetString() ?? "",
                        SubnetId = n.GetProperty("subnetId").GetString() ?? "",
                        VpcId = n.GetProperty("vpcId").GetString() ?? "",
                        PublicIp = n.GetProperty("publicIp").GetString() ?? "",
                        Region = n.GetProperty("region").GetString() ?? ""
                    });
                }
                natGatewayGrid.ItemsSource = natRows;
            }
            // ==============================
            // Internet Gateway inventory
            // ==============================
            if (data.TryGetProperty("internetGateways", out var igwData) && igwGrid != null)
            {
                var igwRows = new List<object>();
                foreach (var igw in igwData.EnumerateArray())
                {
                    igwRows.Add(new
                    {
                        IgwId = igw.GetProperty("igwId").GetString() ?? "",
                        AttachedVpcIds = igw.GetProperty("attachedVpcIds").GetString() ?? "",
                        AttachmentState = igw.GetProperty("attachmentState").GetString() ?? "",
                        Region = igw.GetProperty("region").GetString() ?? ""
                    });
                }
                igwGrid.ItemsSource = igwRows;
            }

            }




        }
        catch
        {
            statusText.Text = "🟡 Session expired or invalid response";
            scoreText.Text = "Validation error";
        }
    }


    private void StartAutoRefresh()
    {
        _autoRefreshTimer = new System.Timers.Timer(AutoRefreshIntervalMs);
        _autoRefreshTimer.Elapsed += async (s, e) =>
        {
            if (_refreshInProgress || _apiService == null)
                return;

            _refreshInProgress = true;

            try
            {
                var json = await _apiService.GetValidationAsync();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoadValidation(json);
                });
            }
            catch
            {
                // Ignore background refresh errors
            }
            finally
            {
                _refreshInProgress = false;
            }
        };

        _autoRefreshTimer.AutoReset = true;
        _autoRefreshTimer.Start();
    }


    private void OnAutoRefreshToggled(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var checkBox = this.FindControl<CheckBox>("AutoRefreshCheckBox");
        if (checkBox == null)
            return;

        if (checkBox.IsChecked == true)
        {
            StartAutoRefresh();
        }
        else
        {
            StopAutoRefresh();
        }
    }

    private void StopAutoRefresh()
    {
        if (_autoRefreshTimer != null)
        {
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Dispose();
            _autoRefreshTimer = null;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoRefreshTimer?.Stop();
        _autoRefreshTimer?.Dispose();
        base.OnClosed(e);
    }

}

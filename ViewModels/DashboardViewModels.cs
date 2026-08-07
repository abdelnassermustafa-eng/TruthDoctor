using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.ViewModels;

public sealed class DashboardViewModel
{
    public string ProviderName { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string IdentityArn { get; set; } = "";
    public string SelectedLocation { get; set; } = "";
    public string DiscoveryStatus { get; set; } = "Not started";
    public string LastRefresh { get; set; } = "Not yet";
    public int TotalResourceCount { get; set; }

    public ObservableCollection<string> Locations { get; } = [];
    public ObservableCollection<DashboardDomainViewModel> Domains { get; } = [];
    public ObservableCollection<DashboardResourceViewModel> Resources { get; } = [];

    public static DashboardViewModel FromState(
        PlatformState state)
    {
        var viewModel = new DashboardViewModel
        {
            ProviderName = state.Context.ProviderName,
            ProviderId = state.Context.ProviderId,
            AccountId = state.Context.AccountId,
            AccountName = string.IsNullOrWhiteSpace(
                state.Context.AccountName)
                ? state.Context.AccountId
                : state.Context.AccountName,
            IdentityArn = state.Context.IdentityArn,
            SelectedLocation = state.Context.DefaultLocation,
            DiscoveryStatus = state.Warnings.Count == 0
                ? "Completed"
                : $"Completed with {state.Warnings.Count} warning(s)",
            LastRefresh = state.DiscoveredAt.LocalDateTime
                .ToString("g"),
            TotalResourceCount = state.TotalResourceCount
        };

        foreach (var location in state.Context.Locations)
        {
            viewModel.Locations.Add(location);
        }

        foreach (var domain in state.Domains)
        {
            viewModel.Domains.Add(
                new DashboardDomainViewModel
                {
                    Id = domain.Id,
                    DisplayName = domain.DisplayName,
                    Icon = RenderRegistry.ResolveIcon(
                        domain.IconKey),
                    AccentKey = domain.AccentKey,
                    Background = RenderRegistry.ResolveBackground(
                        domain.AccentKey),
                    Border = RenderRegistry.ResolveBorder(
                        domain.AccentKey),
                    Foreground = RenderRegistry.ResolveForeground(
                        domain.AccentKey),
                    ResourceCount = domain.ResourceCount,
                    ResourceTypes = string.Join(
                        ", ",
                        domain.ResourceTypes)
                });
        }

        foreach (var resource in state.Resources)
        {
            viewModel.Resources.Add(
                new DashboardResourceViewModel
                {
                    ProviderId = resource.ProviderId,
                    AccountId = resource.AccountId,
                    DomainId = resource.DomainId,
                    ResourceType = resource.ResourceType,
                    ResourceId = resource.ResourceId,
                    NativeId = resource.NativeId,
                    DisplayName = resource.DisplayName,
                    State = resource.State,
                    Location = resource.Location,
                    AvailabilityZone = resource.AvailabilityZone,
                    Arn = resource.Arn,
                    Icon = RenderRegistry.ResolveIcon(
                        resource.IconKey),
                    AccentKey = resource.AccentKey,
                    Foreground = RenderRegistry.ResolveForeground(
                        resource.AccentKey),
                    Properties = resource.Properties,
                    Tags = resource.Tags,
                    Capabilities = resource.Capabilities
                });
        }

        return viewModel;
    }
}

public sealed class DashboardDomainViewModel
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "◇";
    public string AccentKey { get; set; } = "";
    public string Background { get; set; } = "#162033";
    public string Border { get; set; } = "#334155";
    public string Foreground { get; set; } = "#E2E8F0";
    public int ResourceCount { get; set; }
    public string ResourceTypes { get; set; } = "";
}

public sealed class DashboardResourceViewModel
{
    public string ProviderId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string DomainId { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string ResourceId { get; set; } = "";
    public string NativeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string State { get; set; } = "";
    public string Location { get; set; } = "";
    public string AvailabilityZone { get; set; } = "";
    public string Arn { get; set; } = "";
    public string Icon { get; set; } = "◇";
    public string AccentKey { get; set; } = "";
    public string Foreground { get; set; } = "#E2E8F0";
    public IReadOnlyDictionary<string, string> Properties { get; set; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Tags { get; set; } =
        new Dictionary<string, string>();
    public IReadOnlyList<string> Capabilities { get; set; } = [];
}

public static class RenderRegistry
{
    public static string ResolveIcon(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "aws" => "AWS",
            "network" or "networking" => "⌘",
            "compute" or "instance" or "ec2-instance" => "▣",
            "storage" or "volume" or "snapshot" => "▤",
            "database" => "◉",
            "containers" => "⬡",
            "scale" or "auto-scaling" => "↗",
            "load-balancer" or "load-balancing" => "⇆",
            "identity" => "♙",
            "security" or "security-group" => "◆",
            "observability" => "◈",
            "ai" => "✦",
            "vpc" => "☁",
            "subnet" => "▦",
            "route-table" => "↔",
            "internet-gateway" => "◎",
            "nat-gateway" => "⇥",
            "network-acl" => "▥",
            "key-pair" => "⚿",
            "ami" => "◫",
            _ => "◇"
        };
    }

    public static string ResolveBackground(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "orange" => "#633D16",
            "amber" => "#5B4317",
            "purple" => "#382D72",
            "blue" => "#164778",
            "green" => "#19583F",
            "cyan" => "#0D5667",
            "teal" => "#14564F",
            "emerald" => "#14563C",
            "indigo" => "#303C76",
            "red" => "#682C35",
            "yellow" => "#5F501B",
            "violet" => "#503071",
            _ => "#162033"
        };
    }

    public static string ResolveBorder(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "orange" => "#B9782C",
            "amber" => "#A77B2D",
            "purple" => "#725FD1",
            "blue" => "#2D72B6",
            "green" => "#27835B",
            "cyan" => "#16899E",
            "teal" => "#268B80",
            "emerald" => "#27835B",
            "indigo" => "#5867C5",
            "red" => "#B84D5E",
            "yellow" => "#A9902E",
            "violet" => "#8151B2",
            _ => "#334155"
        };
    }

    public static string ResolveForeground(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "orange" => "#FDBA74",
            "amber" => "#FCD34D",
            "purple" => "#D8B4FE",
            "blue" => "#BFDBFE",
            "green" => "#BBF7D0",
            "cyan" => "#A5F3FC",
            "teal" => "#99F6E4",
            "emerald" => "#A7F3D0",
            "indigo" => "#C7D2FE",
            "red" => "#FECDD3",
            "yellow" => "#FEF08A",
            "violet" => "#E9D5FF",
            _ => "#E2E8F0"
        };
    }
}

namespace TruthDoctor.Services.Providers;

public static class ProviderVisualFactory
{
    public static ResourceRenderDescriptor Create(
        string iconKey,
        string accentKey)
    {
        return new ResourceRenderDescriptor
        {
            Icon = ResolveIcon(iconKey),
            AccentKey = accentKey,
            Background = ResolveBackground(accentKey),
            Border = ResolveBorder(accentKey),
            Foreground = ResolveForeground(accentKey)
        };
    }

    public static string AccentForDomain(string domainId)
    {
        return domainId.ToLowerInvariant() switch
        {
            "identity" => "amber",
            "networking" => "purple",
            "compute" => "blue",
            "auto-scaling" => "green",
            "load-balancing" => "cyan",
            "storage" => "teal",
            "database" => "emerald",
            "containers" => "indigo",
            "security" => "red",
            "observability" => "yellow",
            "ai" => "violet",
            _ => "default"
        };
    }

    public static string ResolveIcon(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "aws" => "AWS",
            "azure" => "AZ",
            "oci" => "OCI",
            "gcp" => "GCP",
            "vmware" => "VM",
            "kubernetes" => "K8S",
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

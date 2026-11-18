using System;
using System.Linq;
using System.Text.Json;

namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Normalized identity of an AWS principal.
///
/// The identity is stable across assumed-role session-name
/// renewal for the same role and AWS account.
///
/// This contract does not guarantee that independently
/// executed AWS CLI commands used identical credentials.
/// </summary>
public sealed record AwsCloudTrailPrincipalIdentity(
    string Partition,
    string AccountId,
    string PrincipalKind,
    string PrincipalName)
{
    public bool SamePrincipal(
        AwsCloudTrailPrincipalIdentity? other)
        => other is not null &&
           StringComparer.Ordinal.Equals(
               Partition, other.Partition) &&
           StringComparer.Ordinal.Equals(
               AccountId, other.AccountId) &&
           StringComparer.Ordinal.Equals(
               PrincipalKind, other.PrincipalKind) &&
           StringComparer.Ordinal.Equals(
               PrincipalName, other.PrincipalName);

    public static bool TryParse(
        string? json,
        out AwsCloudTrailPrincipalIdentity? identity)
    {
        identity = null;

        if (string.IsNullOrWhiteSpace(json) ||
            json.Length > 100_000)
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(
                    "Account", out var accountElement) ||
                accountElement.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty(
                    "Arn", out var arnElement) ||
                arnElement.ValueKind != JsonValueKind.String)
                return false;

            string account =
                accountElement.GetString() ?? "";

            string arn =
                arnElement.GetString() ?? "";

            if (account.Length != 12 ||
                !account.All(char.IsAsciiDigit) ||
                arn.Length > 2048)
                return false;

            string[] parts = arn.Split(':', 6);

            // arn:partition:service:region:account:resource
            if (parts.Length != 6 ||
                parts[0] != "arn" ||
                parts[3].Length != 0 ||
                !StringComparer.Ordinal.Equals(
                    parts[4], account) ||
                parts[1] is not (
                    "aws" or "aws-us-gov" or "aws-cn"))
                return false;

            string service = parts[2];
            string resource = parts[5];

            string kind;
            string name;

            if (service == "sts" &&
                resource.StartsWith(
                    "assumed-role/",
                    StringComparison.Ordinal))
            {
                string suffix =
                    resource["assumed-role/".Length..];

                int separator = suffix.LastIndexOf('/');

                if (separator <= 0 ||
                    separator == suffix.Length - 1)
                    return false;

                kind = "assumed-role";
                name = suffix[..separator];
            }
            else if (service == "iam" &&
                     resource.StartsWith(
                         "user/",
                         StringComparison.Ordinal))
            {
                kind = "iam-user";
                name = resource["user/".Length..];
            }
            else if (service == "iam" &&
                     resource.StartsWith(
                         "role/",
                         StringComparison.Ordinal))
            {
                kind = "iam-role";
                name = resource["role/".Length..];
            }
            else if (service == "iam" &&
                     resource == "root")
            {
                kind = "root";
                name = "root";
            }
            else
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(name) ||
                name.StartsWith(
                    "/", StringComparison.Ordinal) ||
                name.EndsWith(
                    "/", StringComparison.Ordinal) ||
                name.Contains(
                    "//", StringComparison.Ordinal))
                return false;

            identity = new AwsCloudTrailPrincipalIdentity(
                parts[1],
                account,
                kind,
                name);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

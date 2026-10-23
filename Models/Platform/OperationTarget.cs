using System;

namespace TruthDoctor.Models.Platform;

/// <summary>
/// Immutable, provider-neutral identity of one operation target.
/// Does not discover, authorize, plan, or execute operations.
/// </summary>
public sealed class OperationTarget
{
    public string ProviderId { get; }

    public string AccountId { get; }

    public string Location { get; }

    public string DomainId { get; }

    public string ResourceType { get; }

    public string NativeId { get; }

    public OperationTarget(
        string providerId,
        string accountId,
        string location,
        string domainId,
        string resourceType,
        string nativeId)
    {
        ProviderId = ValidateRequired(
            providerId, nameof(providerId));
        AccountId = ValidateRequired(
            accountId, nameof(accountId));
        Location = ValidateRequired(
            location, nameof(location));
        DomainId = ValidateRequired(
            domainId, nameof(domainId));
        ResourceType = ValidateRequired(
            resourceType, nameof(resourceType));
        NativeId = ValidateRequired(
            nativeId, nameof(nativeId));
    }

    private static string ValidateRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Operation target identity fields must not be blank.",
                parameterName);
        }

        var normalized = value.Trim();

        foreach (var character in normalized)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Operation target identity fields cannot " +
                    "contain control characters.",
                    parameterName);
            }
        }

        return normalized;
    }
}

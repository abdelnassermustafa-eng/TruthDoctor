using System;
using System.Collections.Generic;

namespace TruthDoctor.Graph;

public sealed class RelationshipSemanticsRegistry
{
    private readonly Dictionary<string, RelationshipSemantic>
        _semantics =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly RelationshipSemantic _unknown =
        new()
        {
            Kind = RelationshipKind.Unknown,
            CanonicalName = "related-to",
            ReverseName = "related-from"
        };

    public RelationshipSemanticsRegistry()
    {
        RegisterDefaults();
    }

    public RelationshipSemantic Resolve(
        string? relationship)
    {
        if (string.IsNullOrWhiteSpace(relationship))
        {
            return _unknown;
        }

        var normalized =
            Normalize(relationship);

        return _semantics.TryGetValue(
            normalized,
            out var semantic)
            ? semantic
            : _unknown;
    }

    public RelationshipKind ResolveKind(
        string? relationship)
    {
        return Resolve(relationship).Kind;
    }

    public void Register(
        string alias,
        RelationshipSemantic semantic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentNullException.ThrowIfNull(semantic);

        _semantics[Normalize(alias)] = semantic;
    }

    private void RegisterDefaults()
    {
        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.Contains,
                CanonicalName = "contains",
                ReverseName = "contained-by",
                IsContainment = true
            },
            "contains",
            "contain",
            "parent-of");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.MemberOf,
                CanonicalName = "member-of",
                ReverseName = "has-member",
                IsContainment = true
            },
            "member-of",
            "belongs-to",
            "contained-by");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.AttachedTo,
                CanonicalName = "attached-to",
                ReverseName = "has-attached",
                IsDependency = true
            },
            "attached-to",
            "attachment",
            "attached");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.HostedOn,
                CanonicalName = "hosted-on",
                ReverseName = "hosts",
                IsDependency = true
            },
            "hosted-on",
            "runs-on",
            "located-on");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.DependsOn,
                CanonicalName = "depends-on",
                ReverseName = "depended-on-by",
                IsDependency = true
            },
            "depends-on",
            "dependency",
            "requires");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.Uses,
                CanonicalName = "uses",
                ReverseName = "used-by",
                IsDependency = true
            },
            "uses",
            "references",
            "consumes");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.ConnectedTo,
                CanonicalName = "connected-to",
                ReverseName = "connected-from",
                IsConnectivity = true
            },
            "connected-to",
            "connected",
            "connects-to");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.RoutesThrough,
                CanonicalName = "routes-through",
                ReverseName = "routes-for",
                IsConnectivity = true,
                IsTrafficFlow = true
            },
            "routes-through",
            "routes-via",
            "next-hop");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.SecuredBy,
                CanonicalName = "secured-by",
                ReverseName = "secures",
                IsSecurity = true,
                IsDependency = true
            },
            "secured-by",
            "protected-by",
            "security-group",
            "authorized-by");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.Serves,
                CanonicalName = "serves",
                ReverseName = "served-by",
                IsTrafficFlow = true
            },
            "serves",
            "serves-traffic-for",
            "fronts");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.Targets,
                CanonicalName = "targets",
                ReverseName = "targeted-by",
                IsTrafficFlow = true,
                IsDependency = true
            },
            "targets",
            "target-of",
            "forwards-to");

        RegisterAliases(
            new RelationshipSemantic
            {
                Kind = RelationshipKind.AssociatedWith,
                CanonicalName = "associated-with",
                ReverseName = "associated-with"
            },
            "associated-with",
            "associated",
            "related-to");
    }

    private void RegisterAliases(
        RelationshipSemantic semantic,
        params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            Register(alias, semantic);
        }
    }

    private static string Normalize(
        string relationship)
    {
        return relationship
            .Trim()
            .Replace("_", "-", StringComparison.Ordinal)
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}

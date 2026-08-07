using System;
using System.Collections.Generic;

namespace TruthDoctor.Services.Visuals;

public sealed class RenderRegistry
{
    private readonly List<IRenderContributor>
        _contributors = [];

    private readonly IRenderContributor
        _fallback = new DefaultRenderContributor();

    public RenderRegistry(
        IEnumerable<IRenderContributor>? contributors = null)
    {
        if (contributors is not null)
        {
            _contributors.AddRange(contributors);
        }
    }

    public void Register(
        IRenderContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);

        _contributors.Insert(0, contributor);
    }

    public RenderDescriptor Resolve(
        string? providerId,
        string? domainId,
        string? resourceType,
        string? iconKey = null,
        string? accentKey = null)
    {
        var provider =
            providerId?.Trim() ?? "";

        var domain =
            domainId?.Trim() ?? "";

        var type =
            resourceType?.Trim() ?? "";

        var icon =
            iconKey?.Trim() ?? "";

        var accent =
            accentKey?.Trim() ?? "";

        foreach (var contributor in _contributors)
        {
            if (contributor.TryResolve(
                    provider,
                    domain,
                    type,
                    icon,
                    accent,
                    out var visual))
            {
                return visual;
            }
        }

        _fallback.TryResolve(
            provider,
            domain,
            type,
            icon,
            accent,
            out var fallback);

        return fallback;
    }
}

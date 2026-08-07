using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Services.Providers;

public sealed class ProviderRegistry
{
    private readonly Dictionary<string, IProviderPlugin> _plugins;

    private readonly IProviderPlugin _defaultPlugin;

    public ProviderRegistry(
        IEnumerable<IProviderPlugin>? plugins = null)
    {
        _defaultPlugin = new DefaultProviderPlugin();

        var registeredPlugins =
            plugins?.ToList() ??
            [
                new AwsProviderPlugin()
            ];

        _plugins = registeredPlugins
            .GroupBy(
                plugin => plugin.ProviderId,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IProviderPlugin Resolve(string? providerId)
    {
        if (!string.IsNullOrWhiteSpace(providerId) &&
            _plugins.TryGetValue(
                providerId,
                out var plugin))
        {
            return plugin;
        }

        return _defaultPlugin;
    }

    public IReadOnlyCollection<IProviderPlugin> GetAll()
    {
        return _plugins.Values
            .OrderBy(plugin =>
                plugin.ProviderVisual.DisplayName)
            .ToList();
    }

    public void Register(IProviderPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            plugin.ProviderId);

        _plugins[plugin.ProviderId] = plugin;
    }
}

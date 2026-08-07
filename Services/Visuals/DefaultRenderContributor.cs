using TruthDoctor.Services.Providers;

namespace TruthDoctor.Services.Visuals;

public sealed class DefaultRenderContributor :
    IRenderContributor
{
    public bool TryResolve(
        string providerId,
        string domainId,
        string resourceType,
        string iconKey,
        string accentKey,
        out RenderDescriptor visual)
    {
        var resolvedAccent =
            string.IsNullOrWhiteSpace(accentKey) ||
            accentKey == "default"
                ? ProviderVisualFactory.AccentForDomain(
                    domainId)
                : accentKey;

        var resolvedIcon =
            string.IsNullOrWhiteSpace(iconKey)
                ? resourceType
                : iconKey;

        visual = new RenderDescriptor
        {
            Icon =
                ProviderVisualFactory.ResolveIcon(
                    resolvedIcon),

            AccentKey = resolvedAccent,

            Background =
                ProviderVisualFactory.ResolveBackground(
                    resolvedAccent),

            Border =
                ProviderVisualFactory.ResolveBorder(
                    resolvedAccent),

            Foreground =
                ProviderVisualFactory.ResolveForeground(
                    resolvedAccent)
        };

        return true;
    }
}

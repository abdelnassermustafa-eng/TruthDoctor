namespace TruthDoctor.Services.Providers;

public sealed class DefaultProviderPlugin : IProviderPlugin
{
    public string ProviderId => "default";

    public ProviderRenderDescriptor ProviderVisual { get; } =
        new()
        {
            ProviderId = "default",
            DisplayName = "Infrastructure Provider",
            IconKey = "cloud",
            AccentKey = "default"
        };
}

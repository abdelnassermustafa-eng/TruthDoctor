namespace TruthDoctor.Services.Providers;

public sealed class ProviderRenderDescriptor
{
    public string ProviderId { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public string IconKey { get; init; } = "cloud";

    public string AccentKey { get; init; } = "default";

}

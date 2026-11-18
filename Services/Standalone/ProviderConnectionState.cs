namespace TruthDoctor.Services.Standalone;

/// <summary>
/// Connection lifecycle states for an installed provider.
/// These states do not establish successful discovery.
/// </summary>
public enum ProviderConnectionState
{
    Available,
    Installed,
    Configured,
    Authenticating,
    Authenticated,
    Connecting,
    Connected,
    Disconnected,
    Expired,
    Failed
}

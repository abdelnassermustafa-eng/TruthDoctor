using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using TruthDoctor.Models.Platform;

namespace TruthDoctor.Services;

public sealed class PlatformStateClient
{
    private readonly HttpClient _client;

    public PlatformStateClient(
        string baseUrl = "http://localhost:5029")
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                username,
                password
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var login = await response.Content.ReadFromJsonAsync<
            ApiResponse<LoginResult>>(
            cancellationToken: cancellationToken);

        var token = login?.Data?.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return true;
    }

    public async Task<PlatformState> GetStateAsync(
        string? region = null,
        CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(region)
            ? "/api/v2/platform/state"
            : $"/api/v2/platform/state?region={Uri.EscapeDataString(region)}";

        using var response = await _client.GetAsync(
            path,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<
            ApiResponse<PlatformState>>(
            cancellationToken: cancellationToken);

        if (result?.Success != true || result.Data is null)
        {
            throw new InvalidOperationException(
                "The platform state response did not contain valid data.");
        }

        return result.Data;
    }

    private sealed class LoginResult
    {
        public string Token { get; set; } = "";
    }
}

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TruthDoctor.Services;

public sealed class ApiService
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new System.Uri("http://localhost:5029")
    };

    public async Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username, password },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                cancellationToken));

        var root = document.RootElement;

        if (!root.TryGetProperty("success", out var success) ||
            !success.GetBoolean() ||
            !root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("token", out var tokenProperty))
        {
            return false;
        }

        var token = tokenProperty.GetString();

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return true;
    }

    public async Task<JsonDocument> GetJsonAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync(
            path,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        return JsonDocument.Parse(json);
    }

    public async Task<string> GetValidationAsync()
    {
        var response = await _client.PostAsync(
            "/api/v1/validate/all",
            null);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> ValidateAsync(object payload)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/validate",
            payload);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}

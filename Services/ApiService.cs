using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace TruthDoctor.Services;

public class ApiService
{
    private readonly HttpClient _client = new();
    private string? _token;

    private const string BaseUrl = "http://localhost:5029";

    public ApiService()
    {
        _client.BaseAddress = new System.Uri(BaseUrl);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var payload = new
        {
            username = username,
            password = password
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            payload
        );

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("success", out var successProp) ||
                !successProp.GetBoolean())
            {
                return false;
            }

            if (root.TryGetProperty("data", out var dataProp) &&
                dataProp.ValueKind == JsonValueKind.Object &&
                dataProp.TryGetProperty("token", out var tokenProp))
            {
                _token = tokenProp.GetString();

                if (!string.IsNullOrEmpty(_token))
                {
                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _token);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetValidationAsync()
    {
        var response = await _client.PostAsync(
            "/api/v1/validate/all",
            null
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> ValidateAsync(object payload)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/validate",
            payload
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}

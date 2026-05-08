using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LabelStudio.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient = new();
    private readonly ISettingsService _settingsService;

    public AuthService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var baseUrl = _settingsService.BackendUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return false;

            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            var loginUrl = $"{baseUrl}authorization/login/";
            var payload = new { email, password };

            var response = await _httpClient.PostAsJsonAsync(loginUrl, payload);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("status", out var statusEl) && statusEl.GetInt32() == 0)
                {
                    if (root.TryGetProperty("access_token", out var tokenEl))
                    {
                        _settingsService.AuthToken = tokenEl.GetString() ?? string.Empty;
                        _settingsService.Save();
                    }
                    return true;
                }
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Logout()
    {
        _settingsService.AuthToken = string.Empty;
        _settingsService.Save();
    }
}

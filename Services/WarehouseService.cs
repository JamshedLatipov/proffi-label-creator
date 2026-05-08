using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LabelStudio.Services;

public sealed class WarehouseService : IWarehouseService
{
    private readonly HttpClient        _http           = new();
    private readonly ISettingsService  _settingsService;

    public event EventHandler? SessionExpired;

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public WarehouseService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync()
    {
        var baseUrl = _settingsService.BackendUrl.TrimEnd('/');
        var token   = _settingsService.AuthToken;

        Debug.WriteLine($"[WarehouseService] BaseUrl='{baseUrl}'");
        Debug.WriteLine($"[WarehouseService] Token='{(string.IsNullOrWhiteSpace(token) ? "(empty)" : token[..Math.Min(12, token.Length)] + "…")}'");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.WriteLine("[WarehouseService] Aborted: BackendUrl is empty.");
            return [];
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.WriteLine("[WarehouseService] Aborted: AuthToken is empty.");
            return [];
        }

        try
        {
            var url = $"{baseUrl}/warehouses/warehouse/";
            Debug.WriteLine($"[WarehouseService] GET {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            Debug.WriteLine($"[WarehouseService] Response: {(int)response.StatusCode} {response.ReasonPhrase}");

            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[WarehouseService] Body (first 500 chars): {json[..Math.Min(500, json.Length)]}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Debug.WriteLine($"[WarehouseService] {(int)response.StatusCode} — session invalid or forbidden. Redirecting to login.");
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                }
                Debug.WriteLine("[WarehouseService] Non-success status — returning empty.");
                return [];
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Debug.WriteLine($"[WarehouseService] Root kind: {root.ValueKind}");

            // proffi: {"page_count":..., "body": [...]}
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("body", out var body))
            {
                var list = JsonSerializer.Deserialize<List<WarehouseDto>>(body.GetRawText(), _opts) ?? [];
                Debug.WriteLine($"[WarehouseService] Parsed via 'body': {list.Count} items");
                return list;
            }

            // DRF paginated: {"count": N, "results": [...]}
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("results", out var results))
            {
                var list = JsonSerializer.Deserialize<List<WarehouseDto>>(results.GetRawText(), _opts) ?? [];
                Debug.WriteLine($"[WarehouseService] Parsed via 'results': {list.Count} items");
                return list;
            }

            // proffi wrapper: {"status": 0, "data": [...]}
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("data", out var data))
            {
                var list = JsonSerializer.Deserialize<List<WarehouseDto>>(data.GetRawText(), _opts) ?? [];
                Debug.WriteLine($"[WarehouseService] Parsed via 'data': {list.Count} items");
                return list;
            }

            // Direct array
            if (root.ValueKind == JsonValueKind.Array)
            {
                var list = JsonSerializer.Deserialize<List<WarehouseDto>>(json, _opts) ?? [];
                Debug.WriteLine($"[WarehouseService] Parsed as direct array: {list.Count} items");
                return list;
            }

            Debug.WriteLine($"[WarehouseService] Unrecognized JSON structure. Full root keys: {string.Join(", ", EnumerateKeys(root))}");
            return [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WarehouseService] Exception: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    public async Task<IReadOnlyList<ZoneDto>> GetZonesAsync(string warehouseId)
    {
        var baseUrl = _settingsService.BackendUrl.TrimEnd('/');
        var token   = _settingsService.AuthToken;

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
            return [];

        try
        {
            var url = $"{baseUrl}/warehouses/zone/?warehouse_id={Uri.EscapeDataString(warehouseId)}&limit=200";
            Debug.WriteLine($"[WarehouseService] GET {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            Debug.WriteLine($"[WarehouseService] Zones response: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            List<ZoneDto>? list = null;

            // paginated: { "body": [...] }  or { "response": { "list": [...] } }
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("body", out var body))
                    list = JsonSerializer.Deserialize<List<ZoneDto>>(body.GetRawText(), _opts);
                else if (root.TryGetProperty("results", out var results))
                    list = JsonSerializer.Deserialize<List<ZoneDto>>(results.GetRawText(), _opts);
                else if (root.TryGetProperty("data", out var data))
                    list = JsonSerializer.Deserialize<List<ZoneDto>>(data.GetRawText(), _opts);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                list = JsonSerializer.Deserialize<List<ZoneDto>>(json, _opts);
            }

            Debug.WriteLine($"[WarehouseService] Zones parsed: {list?.Count ?? 0}");
            return list ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WarehouseService] GetZonesAsync exception: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    public async Task<IReadOnlyList<ShelfDto>> GetShelvesAsync(string zoneId)
    {
        var baseUrl = _settingsService.BackendUrl.TrimEnd('/');
        var token   = _settingsService.AuthToken;

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
            return [];

        try
        {
            var url = $"{baseUrl}/warehouses/shelf/?warehouse_zone_id={Uri.EscapeDataString(zoneId)}&limit=200";
            Debug.WriteLine($"[WarehouseService] GET {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            Debug.WriteLine($"[WarehouseService] Shelves response: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            List<ShelfDto>? list = null;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("body", out var body))
                    list = JsonSerializer.Deserialize<List<ShelfDto>>(body.GetRawText(), _opts);
                else if (root.TryGetProperty("results", out var results))
                    list = JsonSerializer.Deserialize<List<ShelfDto>>(results.GetRawText(), _opts);
                else if (root.TryGetProperty("data", out var data))
                    list = JsonSerializer.Deserialize<List<ShelfDto>>(data.GetRawText(), _opts);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                list = JsonSerializer.Deserialize<List<ShelfDto>>(json, _opts);
            }

            Debug.WriteLine($"[WarehouseService] Shelves parsed: {list?.Count ?? 0}");
            return list ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WarehouseService] GetShelvesAsync exception: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    public async Task<IReadOnlyList<BoxDto>> GetBoxesAsync(string shelfId)
    {
        var baseUrl = _settingsService.BackendUrl.TrimEnd('/');
        var token   = _settingsService.AuthToken;

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
            return [];

        try
        {
            var url = $"{baseUrl}/warehouses/box/?warehouse_shelf_id={Uri.EscapeDataString(shelfId)}&limit=200";
            Debug.WriteLine($"[WarehouseService] GET {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            Debug.WriteLine($"[WarehouseService] Boxes response: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            List<BoxDto>? list = null;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("body", out var body))
                    list = JsonSerializer.Deserialize<List<BoxDto>>(body.GetRawText(), _opts);
                else if (root.TryGetProperty("results", out var results))
                    list = JsonSerializer.Deserialize<List<BoxDto>>(results.GetRawText(), _opts);
                else if (root.TryGetProperty("data", out var data))
                    list = JsonSerializer.Deserialize<List<BoxDto>>(data.GetRawText(), _opts);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                list = JsonSerializer.Deserialize<List<BoxDto>>(json, _opts);
            }

            Debug.WriteLine($"[WarehouseService] Boxes parsed: {list?.Count ?? 0}");
            return list ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WarehouseService] GetBoxesAsync exception: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    private static IEnumerable<string> EnumerateKeys(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) yield break;
        foreach (var prop in element.EnumerateObject())
            yield return prop.Name;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LabelStudio.Services;

public interface IProductService
{
    /// <summary>
    /// Searches products by barcode.
    /// Returns (results, null) on success, ([], errorMessage) on failure.
    /// </summary>
    Task<(IReadOnlyList<ProductSuggestion> Results, string? Error)> SearchByBarcodeAsync(string barcode);
}

public sealed class ProductSuggestion
{
    public string Id           { get; }
    public string Name         { get; }
    public string Barcode      { get; }
    public string Article      { get; }
    public string CategoryName { get; }
    public string CategoryCode { get; }
    public string Description  { get; }
    public string SizeDisplay  { get; }
    public string SizeCode     { get; }
    public string StateName    { get; }
    public string Origin       { get; }

    public ProductSuggestion(string id, string name, string barcode = "", string article = "",
        string categoryName = "", string categoryCode = "", string description = "",
        string sizeDisplay = "", string sizeCode = "", string stateName = "", string origin = "")
    {
        Id           = id;   Name         = name;   Barcode      = barcode;
        Article      = article;
        CategoryName = categoryName; CategoryCode = categoryCode;
        Description  = description;
        SizeDisplay  = sizeDisplay;  SizeCode     = sizeCode;
        StateName    = stateName;    Origin       = origin;
    }

    public override string ToString() => Name;
}

public sealed class ProductService : IProductService
{
    // Shared static client: ProductService is instantiated once per PrintPreviewViewModel,
    // so a per-instance HttpClient is acceptable in a desktop app.
    private readonly HttpClient       _http = new();
    private readonly ISettingsService _settings;

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<(IReadOnlyList<ProductSuggestion> Results, string? Error)> SearchByBarcodeAsync(string barcode)
    {
        var baseUrl = _settings.BackendUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return ([], "Backend URL not configured. Open Connection Settings.");

        try
        {
            var url = $"{baseUrl}/products/product/?barcode={Uri.EscapeDataString(barcode.Trim())}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(_settings.AuthToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AuthToken);

            var response = await _http.SendAsync(request);
            var rawBody  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return ([], $"Server returned {(int)response.StatusCode}");

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("body", out var bodyEl) || bodyEl.ValueKind != JsonValueKind.Array)
                return ([], null);  // empty body — not an error, just no results

            var results = bodyEl.EnumerateArray()
                .Select(ParseProduct)
                .Where(p => !string.IsNullOrEmpty(p.Id))
                .ToList();

            return (results, null);
        }
        catch (HttpRequestException ex)
        {
            return ([], $"Network error: {ex.Message}");
        }
        catch (JsonException)
        {
            return ([], "Failed to parse server response");
        }
        catch (Exception ex)
        {
            return ([], $"Error: {ex.Message}");
        }
    }

    private static ProductSuggestion ParseProduct(JsonElement el)
    {
        static string S(JsonElement e, string key) =>
            e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? string.Empty : string.Empty;

        var id          = S(el, "id");
        var name        = S(el, "name");
        var barcode     = S(el, "barcode");
        var article     = S(el, "article");
        var description = S(el, "description");
        var origin      = S(el, "origin");

        string categoryName = string.Empty, categoryCode = string.Empty;
        if (el.TryGetProperty("category", out var cat) && cat.ValueKind == JsonValueKind.Object)
        {
            categoryName = S(cat, "name");
            categoryCode = S(cat, "code");
        }

        string sizeDisplay = string.Empty, sizeCode = string.Empty;
        if (el.TryGetProperty("size", out var sz) && sz.ValueKind == JsonValueKind.Object)
        {
            sizeDisplay = S(sz, "size_display");
            sizeCode    = S(sz, "code");
        }

        string stateName = string.Empty;
        if (el.TryGetProperty("state", out var st) && st.ValueKind == JsonValueKind.Object)
            stateName = S(st, "name");

        return new ProductSuggestion(id, name, barcode, article,
            categoryName, categoryCode, description, sizeDisplay, sizeCode, stateName, origin);
    }
}

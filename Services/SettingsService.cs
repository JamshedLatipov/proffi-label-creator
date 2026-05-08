using System;
using System.IO;
using System.Text.Json;

namespace LabelStudio.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LabelStudio", "appsettings.json");

    private readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };
    private SettingsData _data;

    public SettingsService()
    {
        _data = Load();
    }

    public string BackendUrl
    {
        get => _data.BackendUrl;
        set => _data.BackendUrl = value;
    }

    public string AuthToken
    {
        get => _data.AuthToken;
        set => _data.AuthToken = value;
    }

    public string DefaultWarehouseId
    {
        get => _data.DefaultWarehouseId;
        set => _data.DefaultWarehouseId = value;
    }

    public string UserEmail
    {
        get => _data.UserEmail;
        set => _data.UserEmail = value;
    }

    public string DefaultWarehouseName
    {
        get => _data.DefaultWarehouseName;
        set => _data.DefaultWarehouseName = value;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_data, _jsonOpts));
    }

    private static SettingsData Load()
    {
        if (!File.Exists(SettingsPath)) return new SettingsData();
        try
        {
            var text = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<SettingsData>(text) ?? new SettingsData();
        }
        catch { return new SettingsData(); }
    }

    private sealed class SettingsData
    {
        public string BackendUrl            { get; set; } = string.Empty;
        public string AuthToken             { get; set; } = string.Empty;
        public string UserEmail             { get; set; } = string.Empty;
        public string DefaultWarehouseId    { get; set; } = string.Empty;
        public string DefaultWarehouseName  { get; set; } = string.Empty;
    }
}

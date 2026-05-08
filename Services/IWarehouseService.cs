using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LabelStudio.Services;

public sealed class WarehouseDto
{
    public string Id   { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

public sealed class ZoneDto
{
    public string Id          { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public string Code        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} — {Name}";
}

public sealed class ShelfDto
{
    public string Id          { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public string Code        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} — {Name}";
}

public sealed class BoxDto
{
    public string Id           { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public string Code         { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("location_path")]
    public string LocationPath { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} — {Name}";
}

public interface IWarehouseService
{
    event EventHandler? SessionExpired;
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync();
    Task<IReadOnlyList<ZoneDto>>     GetZonesAsync(string warehouseId);
    Task<IReadOnlyList<ShelfDto>>    GetShelvesAsync(string zoneId);
    Task<IReadOnlyList<BoxDto>>      GetBoxesAsync(string shelfId);
}

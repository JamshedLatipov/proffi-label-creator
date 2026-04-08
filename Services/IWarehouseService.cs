using System.Collections.Generic;
using System.Threading.Tasks;

namespace LabelStudio.Services;

public sealed class WarehouseDto
{
    public string Id   { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync();
}

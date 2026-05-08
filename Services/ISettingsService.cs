namespace LabelStudio.Services;

public interface ISettingsService
{
    string BackendUrl { get; set; }
    string AuthToken { get; set; }
    string UserEmail { get; set; }
    string DefaultWarehouseId   { get; set; }
    string DefaultWarehouseName { get; set; }
    void Save();
}

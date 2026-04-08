namespace LabelStudio.Services;

public interface ISettingsService
{
    string BackendUrl { get; set; }
    string AuthToken { get; set; }
    void Save();
}

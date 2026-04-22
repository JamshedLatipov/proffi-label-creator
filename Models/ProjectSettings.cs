namespace LabelStudio.Models;

public record ProjectSettings(
    string ProjectName,
    string Description,
    double LabelWidth,
    double LabelHeight,
    bool IsPortrait,
    string PrinterProfileName,
    string PrinterName,
    string Dpi,
    string Material,
    bool IsMonochrome
);

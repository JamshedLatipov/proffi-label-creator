namespace LabelStudio.Models;

public record PrinterProfile(
    string Name,
    string PrinterName,
    string Dpi,
    string Material,
    bool IsMonochrome
)
{
    /// <summary>Human-readable summary shown in the combo/card.</summary>
    public string Summary =>
        $"{PrinterName} · {Dpi} · {Material} · {(IsMonochrome ? "Mono" : "Color")}";
}

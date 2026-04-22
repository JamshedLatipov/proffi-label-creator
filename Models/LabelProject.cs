using System;
using System.Collections.Generic;

namespace LabelStudio.Models;

public class LabelProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public double LabelWidth { get; set; }
    public double LabelHeight { get; set; }
    public bool IsPortrait { get; set; }
    public string PrinterProfileName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public string Dpi { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public bool IsMonochrome { get; set; }
    public List<LabelElement> Elements { get; set; } = [];

    public static LabelProject FromSettings(ProjectSettings s) => new()
    {
        Name               = s.ProjectName,
        Description        = s.Description,
        LabelWidth         = s.LabelWidth,
        LabelHeight        = s.LabelHeight,
        IsPortrait         = s.IsPortrait,
        PrinterProfileName = s.PrinterProfileName,
        PrinterName        = s.PrinterName,
        Dpi                = s.Dpi,
        Material           = s.Material,
        IsMonochrome       = s.IsMonochrome,
    };
}

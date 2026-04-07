using System;

namespace LabelStudio.Models;

public enum ElementKind { Text, DynamicField, Barcode, QrCode, Rectangle, Image }

public class LabelElement
{
    public string Id       { get; set; } = Guid.NewGuid().ToString("N");
    public ElementKind Kind { get; set; }

    // Position and size in mm
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Width  { get; set; } = 30;
    public double Height { get; set; } = 8;

    // Text / Dynamic
    public string Content    { get; set; } = string.Empty;   // static text or field key
    public string FontFamily { get; set; } = "Inter";
    public double FontSize   { get; set; } = 10;
    public bool   Bold       { get; set; }
    public bool   Italic     { get; set; }
    public string Color      { get; set; } = "#000000";

    // Barcode / QR only
    public string BarcodeValue { get; set; } = "0000000000000";

    // Image only
    public string ImagePath { get; set; } = string.Empty;
}

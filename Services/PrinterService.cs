using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LabelStudio.Services;

/// <summary>Cross-platform helper for enumerating installed system printers.</summary>
public static class PrinterService
{
    /// <summary>
    /// Returns the list of printer names registered in the OS.
    /// Returns an empty list on non-Windows platforms instead of throwing.
    /// </summary>
    public static IReadOnlyList<string> GetSystemPrinters()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [];

        using var key = Registry.LocalMachine?.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers");
        return key?.GetSubKeyNames() ?? [];
    }
}

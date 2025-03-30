using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Terminal.Gui;

/// <summary>
/// Attempts to determine information about the terminal and what features it
/// does/not support based on runtime operations e.g. registry etc
/// </summary>
internal class ConsoleFeatureFinder
{
    public ConsoleFeatureFinderResults GetResults ()
    {
        var results = new ConsoleFeatureFinderResults ();

        PlatformID p = Environment.OSVersion.Platform;
        results.IsWindows = p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows;

        if (results.IsWindows)
        {
            DetectWindowsSpecificFeatures (results.Windows);
        }

        return results;
    }

    private void DetectWindowsSpecificFeatures (WindowsFeatureSet windowsFeatures)
    {
        windowsFeatures.ConHostLegacyMode = IsLegacyConsoleEnabled ();
    }

    bool IsLegacyConsoleEnabled ()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey (@"Console"))
            {
                if (key != null)
                {
                    object value = key.GetValue ("ForceV2");
                    if (value is int intValue)
                    {
                        return intValue == 0; // Legacy Mode enabled if ForceV2 is 0
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ("Error reading registry: " + ex.Message);
        }

        return false; // Assume new console mode if check fails
    }
}
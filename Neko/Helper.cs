using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using TextCopy;

namespace Neko;

public static class Helper
{
    private static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    public static string SizeSuffix(long value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException(nameof(decimalPlaces)); }
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        var mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        var adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
    public static IEnumerable<Enum> GetFlags(Enum input)
    {
        return from Enum value in Enum.GetValues(input.GetType())
               where input.HasFlag(value) && Convert.ToInt32(value) > 0
               select value;
    }

    public static void CopyToClipboard(string text)
    {
        if (text == "")
        {
            Gui.Common.Notification("Unable to copy to Clipboard");
            return;
        }

        ClipboardService.SetText(text);
        Gui.Common.Notification($"Copied \"{text}\" to Clipboard");
    }

    public static void OpenInBrowser(string url)
    {
        if (url == "")
        {
            Gui.Common.Notification("Unable to open in a Browser");
            return;
        }

        // Safty check url
        try
        {
            var uri = new Uri(url);
            var scheme = uri.GetLeftPart(UriPartial.Scheme).ToString();
            if (scheme is not "https://" and not "http://")
                throw new Exception("Invald scheme");
        }
        catch (Exception ex)
        {
            Gui.Common.Notification("Unable to open in a Browser, Invalid URL");
            PluginLog.Error(ex, "URL unsafe");
            return;
        }

        // Execute url as process
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                Gui.Common.Notification("Unable to open in a Browser");
            }
        }
    }
}


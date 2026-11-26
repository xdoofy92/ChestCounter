using System.Drawing;
using System.Runtime.Versioning;

namespace ChestCounter;

/// <summary>Icono de la ventana: el mismo icono de cofre que lleva el exe (ApplicationIcon).</summary>
[SupportedOSPlatform("windows")]
internal static class AppIcon
{
    public static Icon? Load()
    {
        try
        {
            // ProcessPath can be dotnet.exe or a test host. Always use our own chest resource.
            using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("ChestCounter.app.ico");
            if (stream == null) return null;
            using var icon = new Icon(stream, new Size(32, 32));
            return (Icon)icon.Clone();
        }
        catch
        {
            return null; // sin icono: Windows usa el genérico
        }
    }
}

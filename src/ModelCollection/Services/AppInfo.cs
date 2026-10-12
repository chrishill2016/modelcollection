using System.Reflection;

namespace ModelCollection.Services;

public static class AppInfo
{
    /// <summary>The project's &lt;Version&gt;, stamped with the build number by CI.</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var info = typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = info.IndexOf('+');
        return plus >= 0 ? info[..plus] : info;
    }
}

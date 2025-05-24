using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CBRE.Editor;

static class VersionUtil {
    public static string Version { get; } = FileVersionInfo.GetVersionInfo(typeof(VersionUtil).Assembly.Location).FileVersion;
    public static string GitHash { get; } = typeof(VersionUtil).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
}

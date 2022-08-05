using System.Diagnostics;
using System.IO;

namespace CBRE.Editor;

static class VersionUtil {
    public static string Version { get; } =
        FileVersionInfo.GetVersionInfo(typeof(VersionUtil).Assembly.Location).FileVersion;
    
    public static string GitInfoPath { get; } = Path.Combine(typeof(VersionUtil).Assembly.Location, "..", "gitinfo.txt");

    public static string GitHash { get; } = File.Exists(GitInfoPath) ? File.ReadAllText(GitInfoPath) : null;
}

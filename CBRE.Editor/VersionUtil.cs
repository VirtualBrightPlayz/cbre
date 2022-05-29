using System.Diagnostics;

namespace CBRE.Editor;

static class VersionUtil {
    public static string Version { get; } =
        FileVersionInfo.GetVersionInfo(typeof(VersionUtil).Assembly.Location).FileVersion;
}

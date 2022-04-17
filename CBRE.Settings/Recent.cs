using System;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Settings;

public static class Recent {
    public static List<string> RecentFiles { get => RecentFilesAccessors.Getter(); set => RecentFilesAccessors.Setter(value); }


    private static List<string> _recentFiles = new();

    public record struct Accessors<T>(Func<T> Getter, Action<T> Setter);

    [NonSerialized] // Intended to be overriden by the proper application-side getter.
    public static Accessors<List<string>> RecentFilesAccessors = new(
        Getter: () => _recentFiles,
        Setter: files => _recentFiles = files.ToList()
    );
}

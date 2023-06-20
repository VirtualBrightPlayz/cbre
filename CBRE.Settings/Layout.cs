using System.Collections.Generic;

namespace CBRE.Settings {
    public static class Layout {
        public static int SidebarWidth { get; set; }
        public static string SidebarLayout { get; set; }
        public static List<string> OpenWindows { get; set; }

        static Layout() {
            SidebarWidth = 250;
            SidebarLayout = "";
            OpenWindows = new List<string>();
        }
    }
}
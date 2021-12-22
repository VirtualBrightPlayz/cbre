using ImGuiNET;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CBRE.Editor.Popup {
    public class AboutPopup : PopupUI
    {
        private const string virtualRepo = "https://github.com/VirtualBrightPlayz/cbre";
        private const string cbrePages = "https://juanjp600.github.io/cbre/";
        private const string juanRepo = "https://github.com/juanjp600/cbre";
        private const string gplV2 = "https://www.gnu.org/licenses/gpl-2.0.html";
        private const string logicAndTrickWebsite = "https://logic-and-trick.com/";

        public AboutPopup() : base("About") { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OpenUrl(string url) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
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
                throw new System.NotImplementedException("Platform cannot launch Link");
            }
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {

            ImGui.Text("Source:");
            ImGui.SameLine();

            if (ImGui.Button("Github")) {
                try {
                    Process.Start(virtualRepo);
                } catch {
                    OpenUrl(virtualRepo);
                }
            }

            ImGui.Text("juanjp600:");
            ImGui.SameLine();
            if (ImGui.Button("GitHub Pages CBRE")) {
                try {
                    Process.Start(cbrePages);
                } catch {
                    OpenUrl(cbrePages);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("CBRE GitHub")) {
                try {
                    Process.Start(juanRepo);
                } catch {
                    OpenUrl(juanRepo);
                }
            }

            ImGui.Text("License:");
            ImGui.SameLine();

            if (ImGui.Button("GPLv2")) {
                try {
                    Process.Start(gplV2);
                } catch {
                    OpenUrl(gplV2);
                }
            }

            ImGui.Text("LogicAndTrick:");
            ImGui.SameLine();

            if (ImGui.Button("L&T")) {
                try {
                    Process.Start(logicAndTrickWebsite);
                } catch {
                    OpenUrl(logicAndTrickWebsite);
                }
            }

            shouldBeOpen = true;
            if (ImGui.Button("Close")) {
                shouldBeOpen = false;
            }
        }
    }
}

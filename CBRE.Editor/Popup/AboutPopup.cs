using ImGuiNET;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CBRE.Editor.Popup {
    public class AboutPopup : PopupUI
    {
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
            shouldBeOpen = true;

            ImGui.Text("CBRE:");
            ImGui.SameLine();

            void Button(string name, string url) {
                if (ImGui.Button(name)) {
                    try {
                        Process.Start(url);
                    } catch {
                        OpenUrl(url);
                    }
                }
            }

            Button("Guide", "https://SCP-CBN.github.io/cbre/");
            ImGui.SameLine();
            Button("Source", "https://github.com/VirtualBrightPlayz/cbre");

            ImGui.Text("License:");
            ImGui.SameLine();

            Button("GPLv2", "https://www.gnu.org/licenses/gpl-2.0.html");

            ImGui.Text("LogicAndTrick:");
            ImGui.SameLine();

            Button("L&T", "https://logic-and-trick.com/");

            ImGui.Text("Made by:");
            ImGui.SameLine();
            Button("juanjp600", "https://github.com/juanjp600");
            ImGui.SameLine();
            Button("VirtualBrightPlayz", "https://github.com/VirtualBrightPlayz");
            ImGui.SameLine();
            Button("Salvage", "https://github.com/Saalvage");
            Button("TheMoogle", "https://github.com/TheMoogle");
            ImGui.SameLine();
            Button("AestheticalZ", "https://github.com/AestheticalZ");
            ImGui.SameLine();
            Button("ced777ric", "https://github.com/ced777ric");
            ImGui.SameLine();
            Button("Bananaman043", "https://github.com/Bananaman043");
        }
    }
}

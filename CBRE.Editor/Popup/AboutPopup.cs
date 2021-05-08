using ImGuiNET;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CBRE.Editor.Popup {
    public class AboutPopup : PopupUI
    {
        private const string githubURL = "https://github.com/VirtualBrightPlayz/cbre";
        private const string GPLv2URL = "https://www.gnu.org/licenses/gpl-2.0.html";
        private const string LogicAndTrick = "https://logic-and-trick.com/";

        public AboutPopup() : base("About")
        {
            GameMain.Instance.PopupSelected = true;
        }

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

        protected override bool ImGuiLayout() {

            ImGui.Text("Source:");
            ImGui.SameLine();

            if (ImGui.Button("Github")) {
                try {
                    Process.Start(githubURL);
                } catch {
                    OpenUrl(githubURL);
                }
            }

            ImGui.Text("License:");
            ImGui.SameLine();

            if (ImGui.Button("GPLv2")) {
                try {
                    Process.Start(GPLv2URL);
                } catch {
                    OpenUrl(GPLv2URL);
                }
            }

            ImGui.Text("LogicAndTrick:");
            ImGui.SameLine();

            if (ImGui.Button("L&T")) {
                try {
                    Process.Start(LogicAndTrick);
                } catch {
                    OpenUrl(LogicAndTrick);
                }
            }

            if (ImGui.Button("Close")) {
                return false;
            }

            return true;
        }
    }
}
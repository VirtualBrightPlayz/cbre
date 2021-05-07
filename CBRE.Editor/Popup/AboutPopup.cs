using ImGuiNET;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace CBRE.Editor.Popup {
    public class AboutPopup : PopupUI
    {
        private const string githubURL = "https://github.com/VirtualBrightPlayz/cbre";
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

            ImGui.Text("Links:");
            ImGui.SameLine();
            if (ImGui.Button("Github")) {
                try {
                    Process.Start(githubURL);
                } catch {
                    OpenUrl(githubURL);
                }
            }

            if (ImGui.Button("Close")) {
                return false;
            }

            return true;
        }
    }
}
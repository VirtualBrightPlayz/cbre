using CBRE.Graphics;
using ImGuiNET;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CBRE.Editor.Popup {
    public class AboutPopup : PopupUI
    {
        public readonly bool LoadingBox;
        private bool doneLoading = false;
        protected override bool canBeClosed => !LoadingBox;
        protected override bool canBeDefocused => !LoadingBox;
        protected override bool hasOkButton => !LoadingBox || doneLoading;

        public AboutPopup(bool loading) : base(loading ? "Loading" : "About") {
            LoadingBox = loading;
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

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            ImGui.SetWindowSize(new Vector2(400, 300), ImGuiCond.Once);

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

            ImGui.Text("Version: ");
            ImGui.SameLine();
            using (new ColorPush(ImGuiCol.Text, new Vector4(0f, 1f, 0f, 1f))) {
                ImGui.Text(VersionUtil.Version);
            }

            ImGui.Text("Git Hash: ");
            ImGui.SameLine();
            if (string.IsNullOrEmpty(VersionUtil.GitHash)) {
                using (new ColorPush(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f))) {
                    ImGui.Text("N/A");
                }
            } else {
                using (new ColorPush(ImGuiCol.Text, new Vector4(0f, 1f, 0f, 1f))) {
                    ImGui.Text(VersionUtil.GitHash);
                }
            }

            if (LoadingBox) {
                if (AsyncTexture.AllTextures.Any(x => x.MonoGameTexture == null)) {
                    ImGui.Text("Starting...");
                    ImGui.ProgressBar(1f - (float)AsyncTexture.AllTextures.Count(x => x.MonoGameTexture == null) / AsyncTexture.AllTextures.Count, new Vector2(250, 0));
                } else if (!ImGui.IsWindowHovered()) {
                    shouldBeOpen = false;
                } else {
                    doneLoading = true;
                }
            }
        }
    }
}

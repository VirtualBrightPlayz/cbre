using System.IO;
using System.Text;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SettingsPopup : PopupUI {

        public SettingsPopup(string title) : base(title) {
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text("Texture Directories");
            if (ImGui.BeginChild("TextureDirs", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5))) {
                ImGui.Text("Click listing to remove it");
                ImGui.SameLine();
                if (ImGui.Button("+")) {
                    new CallbackFolderSelect("Select Texture Directory", "", Directories.TextureDirs.Add);
                    // Directories.TextureDirs.Add();
                }
                for (int i = 0; i < Directories.TextureDirs.Count; i++) {
                    var dir = Directories.TextureDirs[i];
                    if (ImGui.Selectable(dir, false)) {
                        Directories.TextureDirs.RemoveAt(i);
                        break;
                    }
                }
            }
            ImGui.EndChild();
            if (ImGui.Button("Close")) {
                SettingsManager.Write();
                return false;
            }
            return true;
        }
    }
}
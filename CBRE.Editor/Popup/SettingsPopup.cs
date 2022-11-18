using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CBRE.Settings;
using ImGuiNET;
using NativeFileDialog;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public sealed class SettingsPopup : PopupUI {
        protected override bool canBeDefocused => false;

        private int fixedHeight = 0;
        
        public SettingsPopup() : base("Options") { }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            ImGui.BeginTabBar("SettingsTabber");

            if (ImGui.BeginTabItem("Camera")) {
                CameraGui();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Directories")) {
                TextureDirGui();
                ModelDirGui();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Hotkeys")) {
                HotkeysGui();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Misc")) {
                MiscGui();
                ImGui.EndTabItem();
            }

            if (fixedHeight <= 0) {
                fixedHeight = (int)ImGui.GetCursorPosY();
            }
        }

        private const int minNonFixedHeight = 24;
        private int GetNonFixedHeight()
            => fixedHeight > 0 ? Math.Max((int)ImGui.GetWindowHeight() - fixedHeight, minNonFixedHeight) : minNonFixedHeight;

        private void CameraGui() {
            ImGui.Text("Camera Settings");
            ImGui.Separator();
            int fov = View.CameraFOV;
            ImGui.SliderInt("FOV", ref fov, v_min: 60, v_max: 110, format: "%d°");
            View.CameraFOV = fov;
        }
        
        private void TextureDirGui() {
            ImGui.Text("Texture Directories");
            ImGui.Separator();
            bool addNew = ImGui.Button("+");
            ImGui.SameLine();
            addNew |= ImGui.Selectable("Click to add a new texture directory", false);
            if (addNew) {
                var result =
                    PickFolderDialog.Open(Directory.GetCurrentDirectory(), out string path);
                if (result == Result.Okay) {
                    Directories.TextureDirs.Add(path.Replace('\\', '/'));
                }
            }
            if (ImGui.BeginChild("TextureDirs", new Num.Vector2(0, GetNonFixedHeight() * 0.5f))) {
                for (int i = 0; i < Directories.TextureDirs.Count; i++) {
                    var dir = Directories.TextureDirs[i];

                    using (ColorPush.RedButton()) {
                        if (ImGui.Button($"X##textureDirs{i}")) {
                            Directories.TextureDirs.RemoveAt(i);
                            break;
                        }
                    }
                    
                    ImGui.SameLine();

                    if (ImGui.Selectable(dir, false)) {
                        var result =
                            PickFolderDialog.Open(Directory.GetCurrentDirectory(), out string path);
                        if (result == Result.Okay) {
                            Directories.TextureDirs[i] = path.Replace('\\', '/');
                        }
                        break;
                    }
                }
            }
            ImGui.EndChild();
            ImGui.Separator();
        }
        
        private void ModelDirGui() {
            ImGui.Text("Model Directories");
            ImGui.Separator();
            bool addNew = ImGui.Button("+");
            ImGui.SameLine();
            addNew |= ImGui.Selectable("Click to add a new model directory", false);
            if (addNew) {
                var result =
                    PickFolderDialog.Open(Directory.GetCurrentDirectory(), out string path);
                if (result == Result.Okay) {
                    Directories.ModelDirs.Add(path.Replace('\\', '/'));
                }
            }
            if (ImGui.BeginChild("ModelDirs", new Num.Vector2(0, GetNonFixedHeight() * 0.5f))) {
                for (int i = 0; i < Directories.ModelDirs.Count; i++) {
                    var dir = Directories.ModelDirs[i];

                    using (ColorPush.RedButton()) {
                        if (ImGui.Button($"X##modelDirs{i}")) {
                            Directories.ModelDirs.RemoveAt(i);
                            break;
                        }
                    }
                    
                    ImGui.SameLine();

                    if (ImGui.Selectable(dir, false)) {
                        var result =
                            PickFolderDialog.Open(Directory.GetCurrentDirectory(), out string path);
                        if (result == Result.Okay) {
                            Directories.ModelDirs[i] = path.Replace('\\', '/');
                        }
                        break;
                    }
                }
            }
            ImGui.EndChild();
            ImGui.Separator();
        }

        public static string GetActionName(string action)
            => Hotkeys.GetHotkeyDefinition(action)?.Name ?? action;

        private void HotkeysGui() {
            ImGui.Text("Hotkeys");
            ImGui.Separator();
            bool addNew = ImGui.Button("+");
            ImGui.SameLine();
            addNew |= ImGui.Selectable("Click to add new bind");
            if (addNew) {
                GameMain.Instance.Popups.Add(new HotkeyListenerPopup(hotkeyIndex: null));
            }
            if (ImGui.BeginChild("Hotkeys", new Num.Vector2(0, GetNonFixedHeight() * 0.5f))) {
                for (int i = 0; i < SettingsManager.Hotkeys.Count; i++) {
                    var hotkey = SettingsManager.Hotkeys[i];
                    
                    using (ColorPush.RedButton()) {
                        if (ImGui.Button($"X##hotkeys{i}")) {
                            SettingsManager.Hotkeys.RemoveAt(i);
                            break;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{GetActionName(hotkey.ID)} - {hotkey.HotkeyString}", false)) {
                        GameMain.Instance.Popups.Add(new HotkeyListenerPopup(hotkeyIndex: i) {
                            SelectedAction = Hotkeys.GetHotkeyDefinition(hotkey.ID),
                            Combo = hotkey.HotkeyString
                        });
                        break;
                    }
                }
            }
            ImGui.EndChild();
        }

        private void MiscGui() {
            ImGui.Text("Misc");
            ImGui.Separator();
            bool dc = Misc.DiscordIntegration;
            ImGui.Checkbox("Enable Discord integration", ref dc);
            if (dc != Misc.DiscordIntegration) {
                Misc.DiscordIntegration = dc;
                GameMain.Instance.SetDiscord(dc);
            }
        }

        public override void Dispose() {
            base.Dispose();
            
            Hotkeys.SetupHotkeys(SettingsManager.Hotkeys);
            SettingsManager.Write();
        }
    }
}

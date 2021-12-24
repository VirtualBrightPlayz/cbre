using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Settings.Models;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using NativeFileDialog;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SettingsPopup : PopupUI {
        protected override bool canBeDefocused => false;

        public SettingsPopup() : base("Options") { }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            TextureDirGui();
            HotkeysGui();
            if (ImGui.Button("Close")) {
                Hotkeys.SetupHotkeys(SettingsManager.Hotkeys);
                SettingsManager.Write();
                shouldBeOpen = false;
            }
        }

        protected virtual void TextureDirGui() {
            ImGui.Text("Texture Directories");
            ImGui.Separator();
            if (ImGui.BeginChild("TextureDirs", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5))) {
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

        private HotkeyDefinition definition;

        protected virtual void HotkeysGui() {
            ImGui.Text("Hotkeys");
            ImGui.Text("Click a listing to remove it");
            if (ImGui.BeginCombo("Hotkey", definition?.ID)) {
                var hks = Hotkeys.GetHotkeyDefinitions().ToArray();
                for (int i = 0; i < hks.Length; i++) {
                    if (ImGui.Selectable(hks[i].ID, hks[i].ID == definition?.ID)) {
                        definition = hks[i];
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.Button("+")) {
                
            }
            ImGui.NewLine();
            if (ImGui.BeginChild("Hotkeys", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 10))) {
                for (int i = 0; i < SettingsManager.Hotkeys.Count; i++) {
                    var dir = SettingsManager.Hotkeys[i];
                    if (ImGui.Selectable($"{dir.ID} {dir.HotkeyString}", false)) {
                        SettingsManager.Hotkeys.RemoveAt(i);
                        break;
                    }
                }
            }
            ImGui.EndChild();
        }
    }
}

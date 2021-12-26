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

        private int fixedHeight = 0;
        
        public SettingsPopup() : base("Options") { }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            TextureDirGui();
            HotkeysGui();
            if (fixedHeight <= 0) {
                fixedHeight = (int)ImGui.GetCursorPosY();
            }
        }

        private const int minNonFixedHeight = 24;
        private int GetNonFixedHeight()
            => fixedHeight > 0 ? Math.Max((int)ImGui.GetWindowHeight() - fixedHeight, minNonFixedHeight) : minNonFixedHeight;
        
        protected virtual void TextureDirGui() {
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

        public static string GetActionName(string action)
            => Hotkeys.GetHotkeyDefinition(action)?.Name ?? action;

        protected virtual void HotkeysGui() {
            ImGui.Text("Hotkeys");
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

        public override void Dispose() {
            base.Dispose();
            
            Hotkeys.SetupHotkeys(SettingsManager.Hotkeys);
            SettingsManager.Write();
        }
    }
}

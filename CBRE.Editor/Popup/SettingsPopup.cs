using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Settings.Models;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SettingsPopup : PopupUI {

        public SettingsPopup() : base("Options") {
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            TextureDirGui();
            HotkeysGui();
            if (ImGui.Button("Close")) {
                Hotkeys.SetupHotkeys(SettingsManager.Hotkeys);
                SettingsManager.Write();
                return false;
            }
            return true;
        }

        protected virtual void TextureDirGui() {
            ImGui.Text("Texture Directories");
            if (ImGui.BeginChild("TextureDirs", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 5))) {
                ImGui.Text("Click a listing to remove it");
                ImGui.SameLine();
                if (ImGui.Button("+")) {
                    new CallbackFolderSelect("Select Texture Directory", "", Directories.TextureDirs.Add);
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
        }

        private HotkeyDefinition definition;
        private Keys key = Keys.None;
        private bool ctrl = false;
        private bool shift = false;
        private bool alt = false;

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
            ImGui.Checkbox("Control Key", ref ctrl);
            ImGui.Checkbox("Shift Key", ref shift);
            ImGui.Checkbox("Alt Key", ref alt);
            if (ImGui.BeginCombo("Key", key.ToString())) {
                var hks = Enum.GetValues<Keys>();
                for (int i = 0; i < hks.Length; i++) {
                    if (ImGui.Selectable(hks[i].ToString(), hks[i] == key)) {
                        key = hks[i];
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.Button("+")) {
                if (key != Keys.None) {
                    List<string> str = new List<string>();
                    if (ctrl)
                        str.Add("Ctrl");
                    if (shift)
                        str.Add("Shift");
                    if (alt)
                        str.Add("Alt");
                    str.Add(key.ToString());
                    Hotkey hkey = new Hotkey() { ID = definition.ID, HotkeyString = string.Join("+", str) };
                    SettingsManager.Hotkeys.Add(hkey);
                }
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

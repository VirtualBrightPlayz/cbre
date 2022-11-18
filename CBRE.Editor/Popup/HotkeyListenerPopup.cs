using System;
using System.Linq;
using CBRE.Settings;
using CBRE.Settings.Models;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace CBRE.Editor.Popup {
    public class HotkeyListenerPopup : PopupUI {
        protected override bool hasOkButton => !string.IsNullOrEmpty(Combo) && SelectedAction != null;
        protected override bool canBeDefocused => false;

        public readonly int? HotkeyIndex;
        public string Combo { get; set; } = "";
        public HotkeyDefinition SelectedAction { get; set; }

        public HotkeyListenerPopup(int? hotkeyIndex) : base("HotkeyListener") {
            HotkeyIndex = hotkeyIndex;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            ImGui.Columns(2);
            ImGui.Text("Action");
            ImGui.Separator();
            if (ImGui.BeginChild("ActionCombo",
                new Vector2(ImGui.GetColumnWidth() - 10, 
                    ImGui.GetWindowHeight() - ImGui.GetTextLineHeight() * 4))) {
                foreach (var action in Hotkeys.Definitions) {
                    if (ImGui.Selectable(
                            SettingsPopup.GetActionName(action.ID),
                            action == SelectedAction)) {
                        SelectedAction = action;
                    }
                }
            }
            ImGui.EndChild();
            
            ImGui.NextColumn();
            
            shouldBeOpen = true;
            
            var keys = Keyboard.GetState().GetPressedKeys().ToHashSet();

            bool keyDown(params Keys[] keysToCheck) {
                bool contains = false;
                foreach (var k in keysToCheck) {
                    contains |= keys.Remove(k);
                }
                return contains;
            }
            string tempCombo =
                ((keyDown(Keys.LeftControl, Keys.RightControl)) ? "Ctrl+" : "")
                + ((keyDown(Keys.LeftShift, Keys.RightShift)) ? "Shift+" : "")
                + ((keyDown(Keys.LeftAlt, Keys.RightAlt)) ? "Alt+" : "")
                + (keys.Any() ? keys.First() : "");
            if (keys.Any()) {
                Combo = tempCombo;
            }

            ImGui.Text("Input a key combination");
            ImGui.NewLine();
            
            using (new ColorPush(ImGuiCol.Text, string.IsNullOrEmpty(Combo) ? Color.DarkGray : Color.White)) {
                ImGui.Text(string.IsNullOrEmpty(Combo) ? tempCombo : Combo);
            }
            ImGui.NewLine();
        }

        public override void OnOkHit() {
            base.OnOkHit();

            var newHotkey = new Hotkey { ID = SelectedAction.ID, HotkeyString = Combo };
            if (HotkeyIndex is { } hotkeyIndex) {
                SettingsManager.Hotkeys[hotkeyIndex] = newHotkey;
            } else {
                SettingsManager.Hotkeys.Add(newHotkey);
            }
        }
    }
}

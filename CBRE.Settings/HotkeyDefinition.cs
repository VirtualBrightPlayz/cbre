using System;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Settings {
    public class HotkeyDefinition {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HotkeysMediator Action { get; set; }
        public object Parameter { get; set; }
        public string[] DefaultHotkeys { get; set; }
        public Keys ShortcutKey { get; set; }
        public bool Ctrl { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Alt { get; set; } = false;

        public HotkeyDefinition(string name, string description, HotkeysMediator action, params string[] defaultHotkeys) {
            ID = action.ToString();
            Name = name;
            Description = description;
            Action = action;
            DefaultHotkeys = defaultHotkeys;
            SetupKey();
        }

        public HotkeyDefinition(string name, string description, HotkeysMediator action, object parameter, params string[] defaultHotkeys) {
            ID = action + (parameter != null ? "." + parameter : "");
            Name = name;
            Description = description;
            Action = action;
            DefaultHotkeys = defaultHotkeys;
            Parameter = parameter;
            SetupKey();
        }

        private void SetupKey() {
            string[] keys = string.Join("+", DefaultHotkeys).Split("+");
            for (int i = 0; i < keys.Length; i++) {
                if (keys[i].ToLower() == "ctrl") {
                    Ctrl = true;
                }
                else if (keys[i].ToLower() == "shift") {
                    Shift = true;
                }
                else if (keys[i].ToLower() == "alt") {
                    Alt = true;
                }
                else {
                    ShortcutKey = Enum.TryParse<Keys>(keys[i], true, out Keys res) ? res : Keys.None;
                }
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Settings {
    public class HotkeyImplementation {
        public HotkeyDefinition Definition { get; private set; }
        public string Hotkey { get; private set; }
        public bool Ctrl { get; private set; } = false;
        public bool Shift { get; private set; } = false;
        public bool Alt { get; private set; } = false;
        public List<Keys> ShortcutKeys { get; private set; } = new List<Keys>();

        public HotkeyImplementation(HotkeyDefinition definition, string hotkey) {
            Definition = definition;
            Hotkey = hotkey;
            SetupKeys();
        }

        public void SetupKeys() {
            ShortcutKeys = new List<Keys>();
            string[] keys = Hotkey.Split("+");
            for (int i = 0; i < keys.Length; i++) {
                string keyLower = keys[i].ToLowerInvariant();
                if (keyLower == "ctrl") {
                    Ctrl = true;
                } else if (keyLower == "shift") {
                    Shift = true;
                } else if (keyLower == "alt") {
                    Alt = true;
                } else if (keyLower == "[") {
                    ShortcutKeys.Add(Keys.OemOpenBrackets);
                } else if (keyLower == "]") {
                    ShortcutKeys.Add(Keys.OemCloseBrackets);
                } else if (keyLower == "del") {
                    ShortcutKeys.Add(Keys.Delete);
                } else {
                    ShortcutKeys.Add(Enum.TryParse<Keys>(keys[i], true, out Keys res) ? res : Keys.None);
                }
            }
        }
    }
}

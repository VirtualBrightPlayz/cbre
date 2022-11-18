using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Settings {
    public class HotkeyImplementation {
        public HotkeyDefinition Definition { get; private set; }
        public string Hotkey { get; private set; }
        public bool Ctrl { get; private set; } = false;
        public bool Shift { get; private set; } = false;
        public bool Alt { get; private set; } = false;
        public ImmutableHashSet<Keys> ShortcutKeys { get; private set; }

        public HotkeyImplementation(HotkeyDefinition definition, string hotkey) {
            Definition = definition;
            Hotkey = hotkey;
            SetupKeys();
        }

        public void SetupKeys() {
            var shortcutKeys = new HashSet<Keys>();
            if (Hotkey == "+") {
                shortcutKeys.Add(Keys.Add);
                shortcutKeys.Add(Keys.OemPlus);
            }
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
                    shortcutKeys.Add(Keys.OemOpenBrackets);
                } else if (keyLower == "]") {
                    shortcutKeys.Add(Keys.OemCloseBrackets);
                } else if (keyLower == "del") {
                    shortcutKeys.Add(Keys.Delete);
                } else if (keyLower == "-") {
                    shortcutKeys.Add(Keys.Subtract);
                    shortcutKeys.Add(Keys.OemMinus);
                } else {
                    shortcutKeys.Add(Enum.TryParse<Keys>(keys[i], true, out Keys res) ? res : Keys.None);
                }
            }

            ShortcutKeys = shortcutKeys.ToImmutableHashSet();
        }
    }
}

using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class PopupGame
    {
        private string _title;
        private string _message;
        private string _icon;

        public enum PopupButtonType {
            Ok,
            YesNo,
        }

        public PopupGame(string title, string message, string icon = "")
        {
            _title = title;
            _message = message;
            _icon = icon;
            GameMain.Instance.Popups.Add(this);
        }

        public bool Draw()
        {
            // Draw our UI
            return ImGuiLayout();
        }

        public virtual void Close() {
            GameMain.Instance.Popups.Remove(this);
        }

        protected virtual bool ImGuiLayout() {
            if (ImGui.Begin(_title)) {
                ImGui.Text(_message);
                if (ImGui.Button("OK")) {
                    return false;
                    // Close();
                }
                ImGui.End();
            }
            return true;
        }
	}
}

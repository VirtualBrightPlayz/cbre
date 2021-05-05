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

namespace CBRE.Editor.Popup {
    public class PopupGame
    {
        private string _title;

        public PopupGame(string title)
        {
            _title = title;
            GameMain.Instance.Popups.Add(this);
        }

        public bool Draw()
        {
            if (ImGui.Begin(_title, ImGuiWindowFlags.NoCollapse)) {
                bool shouldBeOpen = ImGuiLayout();
                ImGui.End();
                return shouldBeOpen;
            }
            return false;
        }

        public virtual void Close() {
            GameMain.Instance.Popups.Remove(this);
        }

        protected virtual bool ImGuiLayout() {
            if (ImGui.Button("OK")) {
                return false;
            }
            return true;
        }
	}
}

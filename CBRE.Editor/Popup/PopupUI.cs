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
    public class PopupUI : IDisposable
    {
        private string _title;
        private bool _hasColor = false;
        private ImColor _color;

        public PopupUI(string title)
        {
            _title = title;
            GameMain.Instance.Popups.Add(this);
        }

        public PopupUI(string title, ImColor color) : this(title)
        {
            _color = color;
            _hasColor = true;
        }

        public virtual bool Draw()
        {
            bool shouldBeOpen = true;
            if (_hasColor)
                ImGui.PushStyleColor(ImGuiCol.WindowBg, _color.Value);
            if (ImGui.Begin(_title, ImGuiWindowFlags.NoCollapse)) {
                shouldBeOpen = ImGuiLayout();
            }
            ImGui.End();
            if (_hasColor)
                ImGui.PopStyleColor();
            return shouldBeOpen;
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

        public virtual void Dispose() {
            Close();
        }
    }
}

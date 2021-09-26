using ImGuiNET;
using System;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class PopupUI : IDisposable
    {
        private string _title;
        private bool _hasColor = false;
        private ImColor _color;
        public bool DrawAlways { get; protected set; }

        public PopupUI(string title)
        {
            _title = title;
            DrawAlways = false;
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
            if (_hasColor) { ImGui.PushStyleColor(ImGuiCol.WindowBg, _color.Value); }
            if (ImGui.Begin(_title, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking)) {
                shouldBeOpen = ImGuiLayout();
                ImGui.End();
            }
            if (_hasColor) { ImGui.PopStyleColor(); }
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

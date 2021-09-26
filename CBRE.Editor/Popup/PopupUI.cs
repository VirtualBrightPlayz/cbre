using ImGuiNET;
using System;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class PopupUI : IDisposable {
        private string _title;
        private bool _hasColor = false;
        private ImColor _color;
        public bool DrawAlways { get; protected set; }
        
        protected virtual bool CanBeClosed => true;
        protected virtual bool CanBeDefocused => true; //TODO: implement

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
            bool closeButtonWasntHit = true;
            if (_hasColor) { ImGui.PushStyleColor(ImGuiCol.WindowBg, _color.Value); }

            string titleAndIndex = $"{_title}##popup{GameMain.Instance.Popups.IndexOf(this)}";
            bool windowWasInitialized = false;
            if (CanBeClosed) {
                windowWasInitialized = ImGui.Begin(titleAndIndex, ref closeButtonWasntHit,
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking);
            } else {
                windowWasInitialized = ImGui.Begin(titleAndIndex, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking);
            }
            if (windowWasInitialized) {
                if (!closeButtonWasntHit) { OnCloseButtonHit(ref shouldBeOpen); }
                if (shouldBeOpen) { shouldBeOpen = ImGuiLayout(); }
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

        protected virtual void OnCloseButtonHit(ref bool shouldBeOpen) { shouldBeOpen = false; }

        public virtual void Dispose() {
            Close();
        }
    }
}

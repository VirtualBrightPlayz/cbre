using ImGuiNET;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public abstract class PopupUI : IDisposable {
        private readonly string title;
        private readonly ImColor? color = null;
        
        protected virtual bool canBeClosed => true;
        protected virtual bool canBeDefocused => true;

        protected int popupIndex => GameMain.Instance.Popups.IndexOf(this);

        protected PopupUI(string title, ImColor? color = null) {
            this.title = title;
            this.color = color;
        }

        public virtual void Update() { }

        public void Draw(out bool shouldBeOpen)
        {
            if (!canBeDefocused) {
                ImGui.SetNextWindowPos(new Num.Vector2(0,0));
                ImGui.SetNextWindowSize(ImGui.GetWindowViewport().Size);
                using (new ColorPush(ImGuiCol.WindowBg, Color.Transparent)) {
                    if (ImGui.Begin($"##blocker{popupIndex}",
                        ImGuiWindowFlags.NoCollapse
                        | ImGuiWindowFlags.NoDecoration
                        | ImGuiWindowFlags.NoMove
                        | ImGuiWindowFlags.NoResize
                        | ImGuiWindowFlags.NoTitleBar
                        | ImGuiWindowFlags.NoDocking)) {
                        ImGui.End();
                    }
                }
            }
            
            shouldBeOpen = true;
            bool closeButtonWasntHit = true; //must default to true because ImGui.Begin only writes this when the X button is hit
            
            using var _ = new ColorPush(ImGuiCol.WindowBg, color);

            string titleAndIndex = $"{title}##popup{popupIndex}";
            bool windowWasInitialized = false;
            if (!canBeDefocused) {
                ImGui.SetWindowFocus(titleAndIndex);
            }
            if (canBeClosed) {
                windowWasInitialized = ImGui.Begin(titleAndIndex, ref closeButtonWasntHit,
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking);
            } else {
                windowWasInitialized = ImGui.Begin(titleAndIndex, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking);
            }
            if (windowWasInitialized) {
                if (!closeButtonWasntHit) { OnCloseButtonHit(ref shouldBeOpen); }

                if (shouldBeOpen) {
                    ImGuiLayout(out shouldBeOpen);
                    OkButton(out bool okButtonHit);
                    if (okButtonHit) { shouldBeOpen = false; }
                }
                ImGui.End();
            }
        }

        protected abstract void ImGuiLayout(out bool shouldBeOpen);
        
        protected void OkButton(out bool hit) {
            hit = ImGui.Button("OK");
        }

        protected virtual void OnCloseButtonHit(ref bool shouldBeOpen) { shouldBeOpen = false; }

        public virtual void Dispose() { }
    }
}

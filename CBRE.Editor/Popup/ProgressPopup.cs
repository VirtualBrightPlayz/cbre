using System;
using CBRE.Editor.Compiling;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using CBRE.Settings;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class ProgressPopup : PopupUI {
        protected override bool canBeDefocused => false;
        public float progress;
        public string message;

        public ProgressPopup(string title) : base(title) {
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            ImGui.SetWindowSize(new Num.Vector2(300,200), ImGuiCond.Once);
            ImGui.Text(message);
            // ImGui.SameLine();
            ImGui.ProgressBar(progress, new Num.Vector2(250f, 0f));
            shouldBeOpen = true;
        }
    }
}

using System;
using CBRE.Editor.Compiling;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using CBRE.Settings;
using ImGuiNET;

namespace CBRE.Editor.Popup {
    public class ProgressPopup : PopupUI {
        public float progress;
        public string message;

        public ProgressPopup(string title) : base(title) {
            DrawAlways = true;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text(message);
            ImGui.SameLine();
            ImGui.ProgressBar(progress);
            return base.ImGuiLayout();
        }
    }
}
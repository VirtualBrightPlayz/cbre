using System.Diagnostics;
using System.Numerics;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class VisgroupsWindow : DockableWindow
    {
        public VisgroupsWindow() : base("visgroups", ImGuiWindowFlags.None) { }
        
        public void DrawVisgroupUI(Visgroup visgroup) {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(visgroup.Colour.R / 255f, visgroup.Colour.G / 255f, visgroup.Colour.B / 255f, 1f));
            bool tOpened = ImGui.TreeNodeEx(visgroup.Name);
            ImGui.PopStyleColor();
            if (tOpened) {
                bool visible = visgroup.Visible;
                if (ImGui.Checkbox("Visible", ref visible)) {
                    visgroup.Visible = visible;
                }
                ImGui.SameLine();
                if (ImGui.Button("Edit")) {
                    new EditVisgroupPopup(visgroup);
                }

                ImGui.Indent(5f);
                for (int i = 0; i < visgroup.Children.Count; i++) {
                    DrawVisgroupUI(visgroup.Children[i]);
                }
                ImGui.Unindent(5f);
                ImGui.TreePop();
            }
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            var window = GameMain.Instance.Window;
            var doc = DocumentManager.CurrentDocument;
            if (doc?.Map?.Visgroups is { } visgroups) {
                for (int i = 0; i < visgroups.Count; i++) {
                    DrawVisgroupUI(visgroups[i]);
                }
            }
        }
    }
}

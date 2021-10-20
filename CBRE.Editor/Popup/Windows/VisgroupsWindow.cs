using System.Diagnostics;
using System.Numerics;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class VisgroupsWindow : WindowUI
    {
        public VisgroupsWindow() : base("")
        {
        }

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

        public override bool Draw() {
            if (ImGui.Begin("visgroups", ref open)) {
                var Window = GameMain.Instance.Window;
                var doc = DocumentManager.CurrentDocument;
                if (doc != null) {
                    var visgroups = doc.Map?.Visgroups;
                    for (int i = 0; i < visgroups.Count; i++) {
                        DrawVisgroupUI(visgroups[i]);
                    }
                }
                ImGui.End();
            }
            return open;
        }
    }
}

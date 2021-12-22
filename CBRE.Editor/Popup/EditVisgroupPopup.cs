using System;
using System.Drawing;
using System.Threading.Tasks;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Compiling;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using CBRE.Providers.Model;
using CBRE.Settings;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class EditVisgroupPopup : PopupUI {

        private Visgroup visgroup;

        public EditVisgroupPopup(Visgroup visgroup) : base("Edit Visgroup") {
            this.visgroup = visgroup;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            string name = visgroup.Name;
            if (ImGui.InputText("Name", ref name, 1024)) {
                visgroup.Name = name;
            }
            Num.Vector4 col = new Num.Vector4(visgroup.Colour.R, visgroup.Colour.G, visgroup.Colour.B, visgroup.Colour.A) / 255f;
            if (ImGui.ColorEdit4("Color", ref col)) {
                col *= 255f;
                visgroup.Colour = System.Drawing.Color.FromArgb((int)col.W, (int)col.X, (int)col.Y, (int)col.Z);
            }
            if (ImGui.Button("New Child")) {
                visgroup.Children.Add(new Visgroup() {
                    Parent = visgroup,
                    Colour = Color.White,
                    Name = "New Visgroup",
                    Visible = true
                });
            }
        }
    }
}

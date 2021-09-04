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

        private Visgroup _visgroup;

        public EditVisgroupPopup(Visgroup visgroup) : base("Edit Visgroup") {
            _visgroup = visgroup;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            string name = _visgroup.Name;
            if (ImGui.InputText("Name", ref name, 1024)) {
                _visgroup.Name = name;
            }
            Num.Vector4 col = new Num.Vector4(_visgroup.Colour.R, _visgroup.Colour.G, _visgroup.Colour.B, _visgroup.Colour.A) / 255f;
            if (ImGui.ColorEdit4("Color", ref col)) {
                col *= 255f;
                _visgroup.Colour = System.Drawing.Color.FromArgb((int)col.W, (int)col.X, (int)col.Y, (int)col.Z);
            }
            if (ImGui.Button("New Child")) {
                _visgroup.Children.Add(new Visgroup() {
                    Parent = _visgroup,
                    Colour = Color.White,
                    Name = "New Visgroup",
                    Visible = true
                });
            }
            return base.ImGuiLayout();
        }
    }
}
using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools.Widgets;
using CBRE.Graphics;
using ImGuiNET;
using ImGuizmoNET;
using Microsoft.Xna.Framework;

namespace CBRE.Editor.Tools.Widgets {
    public class ImGuizmoWidget : Widget {
        private bool wasUsing = false;
        private Matrix oldWorld;

        public ImGuizmoWidget(Document document) {
            SetDocument(document);
        }

        public override void MouseLifted(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e) {
        }

        public override void Render(ViewportBase viewport) {
        }

        public override void ViewportUi(ViewportBase viewport) {
            if (viewport is Viewport3D vp3d) {
                float[] view = vp3d.GetCameraMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                float[] proj = vp3d.GetViewportMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                Matrix matWorld = Matrix.CreateTranslation(Document.Selection.GetSelectionBoundingBox()?.Center.ToXna() ?? Vector3.Zero);
                // if (wasUsing) matWorld = oldWorld;
                float[] world = matWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                float[] worldDelta = new float[16];
                ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
                ImGuizmo.SetRect(vp3d.X, vp3d.Y, vp3d.Width, vp3d.Height);
                ImGuizmo.Manipulate(ref view[0], ref proj[0], OPERATION.TRANSLATE, MODE.WORLD, ref world[0], ref worldDelta[0]);
                if (ImGuizmo.IsUsing()) {
                    oldWorld = new DataStructures.Geometric.Matrix(worldDelta.Select(x => (decimal)x).ToArray()).ToXna();
                    OnTransforming.Invoke(oldWorld.ToCbre());
                } else if (ImGuizmo.IsOver()) {
                    OnTransforming.Invoke(null);
                } else if (wasUsing) {
                    oldWorld = new DataStructures.Geometric.Matrix(worldDelta.Select(x => (decimal)x).ToArray()).ToXna();
                    OnTransformed.Invoke(oldWorld.ToCbre());
                }
                else {
                    OnTransformed.Invoke(null);
                }
                wasUsing = ImGuizmo.IsUsing();
            }
        }

        public override void SelectionChanged() {
            // OnTransformed.Invoke(Matrix.Identity.ToCbre());
        }
    }
}
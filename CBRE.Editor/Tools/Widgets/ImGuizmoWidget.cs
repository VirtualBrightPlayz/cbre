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
        private Matrix oldWorld = Matrix.Identity;
        public readonly OPERATION op;

        public ImGuizmoWidget(Document document, OPERATION op) {
            SetDocument(document);
            this.op = op;
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
                // if (wasUsing && Document.Selection.GetSelectedObjects().Any() && Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Has<DataStructures.Geometric.Matrix>("SelectionMatrix")) matWorld = Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Get<DataStructures.Geometric.Matrix>("SelectionMatrix").ToXna();
                // if (wasUsing) matWorld = oldWorld;
                float[] world = matWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                float[] worldDelta = oldWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
                ImGuizmo.SetRect(vp3d.X, vp3d.Y, vp3d.Width, vp3d.Height);
                ImGuizmo.Manipulate(ref view[0], ref proj[0], op, MODE.LOCAL, ref world[0], ref worldDelta[0]);
                if (ImGuizmo.IsUsing()) {
                    oldWorld = new DataStructures.Geometric.Matrix(worldDelta.Select(x => (decimal)x).ToArray()).ToXna();
                    OnTransforming.Invoke(oldWorld.ToCbre());
                    // oldWorld = new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()).ToXna();
                    foreach (var obj in Document.Selection.GetSelectedObjects()) {
                        obj.MetaData.Set("SelectionMatrix", new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()));
                    }
                } else if (wasUsing) {
                    oldWorld = new DataStructures.Geometric.Matrix(worldDelta.Select(x => (decimal)x).ToArray()).ToXna();
                    OnTransformed.Invoke(oldWorld.ToCbre());
                    // oldWorld = new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()).ToXna();
                    foreach (var obj in Document.Selection.GetSelectedObjects()) {
                        obj.MetaData.Set("SelectionMatrix", new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()));
                    }
                }
                else {
                    // oldWorld = Matrix.Identity;
                    OnTransformed.Invoke(null);
                }
                if (ImGuizmo.IsOver()) {
                    OnTransforming.Invoke(null);
                }
                wasUsing = ImGuizmo.IsUsing();
            }
        }

        public override void SelectionChanged() {
            // OnTransformed.Invoke(Matrix.Identity.ToCbre());
        }
    }
}
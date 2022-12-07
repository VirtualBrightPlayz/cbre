using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools.Widgets;
using CBRE.Graphics;
using CBRE.Settings;
using ImGuiNET;
using ImGuizmoNET;
using Num = System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Matrix = System.Numerics.Matrix4x4;

namespace CBRE.Editor.Tools.Widgets {
    public class ImGuizmoWidget : Widget {
        private bool wasUsing = false;
        private Matrix matWorld = Matrix.Identity;
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
                var keyboardState = Keyboard.GetState();
                float[] view = vp3d.GetCameraMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                float[] proj = vp3d.GetViewportMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                // matWorld = Matrix.CreateTranslation(Document.Selection.GetSelectionBoundingBox()?.Center.ToXna() ?? Vector3.Zero);
                float[] world = matWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                float[] worldDelta = oldWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
                ImGuizmo.SetRect(vp3d.X, vp3d.Y, vp3d.Width, vp3d.Height);
                bool snap = (Select.SnapStyle == SnapStyle.SnapOnAlt && ViewportManager.Ctrl) || (Select.SnapStyle == SnapStyle.SnapOffAlt && !ViewportManager.Ctrl);
                float snapAmount = snap && Document.Map.SnapToGrid ? (op.HasFlag(OPERATION.TRANSLATE) ? (float)Document.Map.GridSpacing : 5f) : 0f;
                ImGuizmo.Manipulate(ref view[0], ref proj[0], op, MODE.LOCAL, ref world[0], ref worldDelta[0], ref snapAmount);
                if (ImGuizmo.IsUsing()) {
                    var old = oldWorld;
                    oldWorld = new DataStructures.Geometric.Matrix(worldDelta.Select(x => (decimal)x).ToArray()).ToXna();
                    oldWorld.Decompose(out var oscl, out var orot, out var opos);
                    if (op.HasFlag(OPERATION.TRANSLATE)) {
                        // matWorld = new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()).ToXna();
                        // matWorld -= oldWorld;
                    }
                    if (op.HasFlag(OPERATION.ROTATE)) {
                        oldWorld = old * oldWorld;
                    }
                    if (op.HasFlag(OPERATION.SCALE)) {
                        oldWorld = old * oldWorld;
                    }

                    OnTransforming.Invoke(oldWorld.ToCbre());
                    // foreach (var obj in Document.Selection.GetSelectedObjects()) {
                    //     new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()).ToXna().Decompose(out var scl, out var rot, out var pos);
                    //     obj.MetaData.Set("SelectionMatrix", rot);
                    // }
                } else if (wasUsing) {
                    OnTransformed.Invoke(oldWorld.ToCbre());
                    // foreach (var obj in Document.Selection.GetSelectedObjects()) {
                    //     new DataStructures.Geometric.Matrix(world.Select(x => (decimal)x).ToArray()).ToXna().Decompose(out var scl, out var rot, out var pos);
                    //     obj.MetaData.Set("SelectionMatrix", rot);
                    // }
                }
                else {
                    oldWorld = Matrix.Identity;
                    // matWorld = Matrix.CreateTranslation(Document.Selection.GetSelectionBoundingBox()?.Center.ToXna() ?? Vector3.Zero);
                    // matWorld = Matrix.Identity;
                    matWorld = Matrix.CreateTranslation(Document.Selection.GetSelectionBoundingBox()?.Center.ToXna() ?? Vector3.Zero);
                    // if (Document.Selection.GetSelectedObjects().Any() && Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Has<DataStructures.Geometric.Matrix>("SelectionMatrix")) matWorld = Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Get<DataStructures.Geometric.Matrix>("SelectionMatrix").ToXna();
                    // if (Document.Selection.GetSelectedObjects().Any() && Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Has<Quaternion>("SelectionMatrix")) matWorld *= Matrix.CreateFromQuaternion(Document.Selection.GetSelectedObjects().FirstOrDefault().MetaData.Get<Quaternion>("SelectionMatrix"));
                    // matWorld.Translation = Document.Selection.GetSelectionBoundingBox()?.Center.ToXna() ?? matWorld.Translation;
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
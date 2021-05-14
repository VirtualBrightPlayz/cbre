using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools.DisplacementTool {
    public class DPSelectTool : DisplacementSubTool {
        private float selectDistance = 10f;

        public DPSelectTool(DisplacementTool tool) : base(tool) {
        }

        public override void UpdateGui() {
            ImGui.InputFloat("Selection Distance", ref selectDistance);
        }

        public override void Render3D(Viewport3D viewport) {
            // Get us into 2D rendering
            const float near = -1000000;
            const float far = 1000000;
            var matrix = Microsoft.Xna.Framework.Matrix.CreateOrthographic(viewport.Width, viewport.Height, near, far);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            BasicEffect basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            basicEffect.LightingEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = matrix;
            basicEffect.View = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            var half = new Vector3(viewport.Width, viewport.Height, 0) / 2;
            // Render out the point handles
            PrimitiveDrawing.Begin(PrimitiveType.QuadList);
            foreach (var point in MainTool.selected) {

                var c = viewport.WorldToScreen(point.CurrentPosition.Location);
                if (c == null || c.Z > 1) continue;
                c -= half;

                PrimitiveDrawing.SetColor(Color.Black);
                PrimitiveDrawing.Vertex2(c.DX - 4, c.DY - 4);
                PrimitiveDrawing.Vertex2(c.DX - 4, c.DY + 4);
                PrimitiveDrawing.Vertex2(c.DX + 4, c.DY + 4);
                PrimitiveDrawing.Vertex2(c.DX + 4, c.DY - 4);

                PrimitiveDrawing.SetColor(Color.Red);
                PrimitiveDrawing.Vertex2(c.DX - 3, c.DY - 3);
                PrimitiveDrawing.Vertex2(c.DX - 3, c.DY + 3);
                PrimitiveDrawing.Vertex2(c.DX + 3, c.DY + 3);
                PrimitiveDrawing.Vertex2(c.DX + 3, c.DY - 3);
            }
            PrimitiveDrawing.End();

            // Get back into 3D rendering
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            ViewportManager.basicEffect.CurrentTechnique.Passes[0].Apply();
        }

        public override void DragEnd() {
        }

        public override void DragMove(Vector3 distance) {
        }

        public override void DragStart(List<DisplacementPoint> clickedPoints) {
        }

        public override string GetContextualHelp() {
            return "*Click* to clear and select.\n" +
            "*Ctrl+Click* to select.\n" +
            "*Shift+Click* to deselect.";
        }

        public override string GetName() {
            return "Displacement Select Tool";
        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyUp(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D vp) {
                var ray = vp.CastRayFromScreen(e.X, e.Y);
                if (!ViewportManager.Shift && !ViewportManager.Ctrl)
                    MainTool.selected.Clear();
                foreach (var displacement in MainTool.GetActiveDisplacements()) {
                    var point = displacement.GetClosestDisplacementPoint(ray);
                    if ((point.Location - ray.ClosestPoint(point.Location)).VectorMagnitude() <= (decimal)selectDistance) {
                        /*if (ViewportManager.Ctrl)
                            MainTool.selected.Add(point);
                        else*/ if (ViewportManager.Shift && MainTool.selected.Contains(point))
                            MainTool.selected.Remove(point);
                        else if (!ViewportManager.Shift)
                            MainTool.selected.Add(point);
                    }
                }
            }
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseDown(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseUp(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e) {
        }

        public override void Render(ViewportBase viewport) {
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) {
        }
    }
}
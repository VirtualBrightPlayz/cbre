using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools.DisplacementTool {
    public class DisplacementTool : BaseTool {
        public float Multiplier = 1f;
        private Dictionary<Solid, Solid> _copies;
        private Displacement[] _selected;
        public DisplacementSubTool current;

        public override string GetContextualHelp() {
            return "*Click* on a displacement to raise it\n" +
                //    "*Ctrl+Click* to select multiple\n" +
                   "*Shift+Click* to lower";
        }

        public override HotkeyTool? GetHotkeyToolType() {
            return HotkeyTool.Displacement;
        }

        public override string GetIcon() {
            return "Tool_Test";
        }

        public override string GetName() {
            return "Displacement Tool";
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters) {
            return HotkeyInterceptResult.Continue;
        }

        public override void UpdateGui()
        {
            ImGui.InputFloat("Multiplier", ref Multiplier);
        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e) {
        }

        public override void KeyUp(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e) {
        }

        public static DataStructures.Geometric.Vector3 ToCbre(Microsoft.Xna.Framework.Vector3 input) {
            return new DataStructures.Geometric.Vector3((decimal)input.X, (decimal)input.Y, (decimal)input.Z);
        }

        public override void ToolSelected(bool preventHistory)
        {
            _copies = new Dictionary<Solid, Solid>();
            foreach (var obj in Document.Selection.GetSelectedObjects().Where(x => x is Solid)) {
                var copy = (Solid)obj.Clone();
                copy.IsSelected = false;
                foreach (var f in copy.Faces) f.IsSelected = false;
                _copies.Add(copy, (Solid)obj);
                ((Solid)copy).Faces.ForEach(p => Document.ObjectRenderer.RemoveFace(p));
            }
            _selected = Document.Selection.GetSelectedObjects().Where(x => x is Solid).SelectMany(x => ((Solid)x).Faces).Where(x => x is Displacement).Select(x => x as Displacement).ToArray();
            Mediator.Subscribe(EditorMediator.SelectionChanged, this);

            current = new DPDragTool(this);
        }
        
        public override void ToolDeselected(bool preventHistory)
        {
            Mediator.UnsubscribeAll(this);

            // Commit the changes
            Commit(_copies.Keys.ToList());

            _copies = null;
            current = null;
        }

        private void Commit(IList<Solid> solids)
        {
            Document.ObjectRenderer.MarkDirty();
            if (!solids.Any()) return;

            // Unhide the solids
            foreach (var solid in solids)
            {
                solid.IsCodeHidden = false;
            }
            var kvs = _copies.Where(x => solids.Contains(x.Value)).ToList();
            foreach (var kv in kvs)
            {
                _copies.Remove(kv.Key);
                foreach (var f in kv.Key.Faces) f.IsSelected = false;
                foreach (var f in kv.Value.Faces) f.IsSelected = false;
            }
            // if (_dirty)
            {
                // Commit the changes
                var edit = new ReplaceObjects(kvs.Select(x => x.Value), kvs.Select(x => x.Key));
                Document.PerformAction("Displacement Manipulation", edit);
            }
        }

        public override void MouseDown(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D vp) {
                var ray = vp.CastRayFromScreen(e.X, e.Y);
                var half = new Vector3(vp.Width, vp.Height, 0) / 2;

                foreach (var copy in _copies.Keys) {
                    var f = copy.Faces.FirstOrDefault();
                    if (f is Displacement displacement) {
                        List<DisplacementPoint> points = new List<DisplacementPoint>();
                        foreach (var dpoint in displacement.GetPoints()) {
                            // var pos = vp.WorldToScreen(dpoint.CurrentPosition.Location);
                            // if (pos == null || pos.Z > 1) continue;
                            // pos -= half;
                            // if (Math.Abs(pos.X - e.X) <= 20 && Math.Abs(pos.Y - e.Y) <= 20) {
                            // if ((pos - ToCbre(e.Location)).VectorMagnitude() <= 20) {
                            var mag = (ray.ClosestPoint(dpoint.CurrentPosition.Location) - dpoint.CurrentPosition.Location).VectorMagnitude();
                            if (mag <= 10) {
                                points.Add(dpoint);
                            }
                        }
                        current?.DragStart(points);
                        return;
                        var point = displacement.GetClosestDisplacementPoint(ray);
                        float mul = Multiplier;
                        if (e.Shift)
                            mul *= -1f;
                        point.Displacement.Distance += (decimal)mul;
                        point.CurrentPosition.Location.DZ += mul;
                    }
                }
            }
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e) {
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D vp) {
                if (e.Button.HasFlag(MouseButtons.Left)) {
                    current?.DragMove(new Vector3(e.DeltaX, e.DeltaY, 0));
                }
            }
        }

        public override void MouseUp(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D vp) {
                current?.DragEnd();
            }
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e) {
        }

        public override void Render(ViewportBase viewport) {
            if (viewport is Viewport3D vp3d) Render3D(vp3d);
        }

        public void Render3D(Viewport3D vp) {
            // Get us into 2D rendering
            const float near = -1000000;
            const float far = 1000000;
            var matrix = Microsoft.Xna.Framework.Matrix.CreateOrthographic(vp.Width, vp.Height, near, far);
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            BasicEffect basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            basicEffect.LightingEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = matrix;
            basicEffect.View = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            var half = new Vector3(vp.Width, vp.Height, 0) / 2;
            foreach (var disp in _copies.Keys.SelectMany(x => x.Faces).Where(x => x is Displacement).Select(x => x as Displacement)) {
                // Render out the point handles
                PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                foreach (var point in disp.GetPoints()) {

                    var c = vp.WorldToScreen(point.CurrentPosition.Location);
                    if (c == null || c.Z > 1) continue;
                    c -= half;

                    PrimitiveDrawing.SetColor(Color.Black);
                    PrimitiveDrawing.Vertex2(c.DX - 4, c.DY - 4);
                    PrimitiveDrawing.Vertex2(c.DX - 4, c.DY + 4);
                    PrimitiveDrawing.Vertex2(c.DX + 4, c.DY + 4);
                    PrimitiveDrawing.Vertex2(c.DX + 4, c.DY - 4);

                    PrimitiveDrawing.SetColor(Color.White);
                    PrimitiveDrawing.Vertex2(c.DX - 3, c.DY - 3);
                    PrimitiveDrawing.Vertex2(c.DX - 3, c.DY + 3);
                    PrimitiveDrawing.Vertex2(c.DX + 3, c.DY + 3);
                    PrimitiveDrawing.Vertex2(c.DX + 3, c.DY - 3);
                }
                PrimitiveDrawing.End();
            }

            // Get back into 3D rendering
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            ViewportManager.basicEffect.CurrentTechnique.Passes[0].Apply();

            current?.Render3D(vp);
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) {
        }
    }
}
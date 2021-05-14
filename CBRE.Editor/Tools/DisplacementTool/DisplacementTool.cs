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
            Mediator.Subscribe(EditorMediator.SelectionChanged, this);
        }
        
        public override void ToolDeselected(bool preventHistory)
        {
            Mediator.UnsubscribeAll(this);

            // Commit the changes
            Commit(_copies.Values.ToList());

            _copies = null;
        }

        private void Commit(IList<Solid> solids)
        {
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
                var wpos = vp.ScreenToWorld(ToCbre(e.Location));
                var ray = vp.CastRayFromScreen(e.X, e.Y);

                // Grab all the elements that intersect with the ray
                var hits = Document.Map.WorldSpawn.GetAllNodesIntersectingWith(ray);

                // Sort the list of intersecting elements by distance from ray origin and grab the first hit
                var hit = hits
                    .Select(x => new { Item = x, Intersection = x.GetIntersectionPoint(ray) })
                    .Where(x => x.Intersection != null)
                    .OrderBy(x => (x.Intersection - ray.Start).VectorMagnitude())
                    .FirstOrDefault();

                if (hit == null) return; // Nothing was clicked

                if (hit.Item is Solid solid) {
                    var f = solid.Faces.FirstOrDefault();
                    if (f is Displacement displacement) {
                        var point = displacement.GetClosestDisplacementPoint(ray);
                        point.OffsetDisplacement.DZ += Multiplier;
                        point.CurrentPosition.Location.DZ += Multiplier;
                        Document.ObjectRenderer.MarkDirty();
                        // displacement.CalculatePoints();
                    }
                }
            }
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
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CBRE.Editor.Popup;
using ImGuiNET;

namespace CBRE.Editor.Tools.VMTool
{
    public class StandardTool : VMSubTool
    {
        private enum VMState
        {
            None,
            Down,
            Moving
        }

        private VMState _state;
        private bool merge;
        private bool split;

        public StandardTool(VMTool mainTool) : base(mainTool)
        {
            /*var sc = new StandardControl();
            sc.Merge += Merge;
            sc.Split += Split;
            Control = sc;*/
        }

        public override void UpdateGui() {
            ImGui.Text($"Split Enabled {split.ToString()}");
            ImGui.Checkbox("Automatically Merge", ref merge);
        }

        private bool AutomaticallyMerge()
        {
            return merge;
            /*return ((StandardControl)Control).AutomaticallyMerge;*/
        }

        private void UpdateSplitEnabled()
        {
            split = CanSplit();
            /*((StandardControl)Control).SplitEnabled = CanSplit();*/
        }

        private bool CanSplit()
        {
            return GetSplitFace() != null;
        }

        private Face GetSplitFace()
        {
            if (MainTool.Points == null) return null;

            var selected = MainTool.Points.Where(x => x.IsSelected).ToList();

            // Must have two points selected
            if (selected.Count != 2) return null;

            // Selection must share a face
            var commonFace = selected[0].GetAdjacentFaces().Intersect(selected[1].GetAdjacentFaces()).ToList();
            if (commonFace.Count != 1) return null;

            var face = commonFace[0];
            var s = selected[0].Vector3;
            var e = selected[1].Vector3;
            var edges = face.GetEdges();

            // The points cannot be adjacent
            return edges.Any(x => (x.Start == s && x.End == e) || (x.Start == e && x.End == s))
                       ? null
                       : face;
        }

        private void Merge(object sender)
        {
            CheckMergedVertices();
        }

        private void Split(object sender)
        {
            var face = GetSplitFace();
            if (face == null) return;

            var solid = face.Parent;

            var sel = MainTool.Points.Where(x => x.IsSelected).ToList();
            var p1 = sel[0];
            var p2 = sel[1];

            if (p1.IsMidPoint) AddAdjacentPoint(face, p1);
            if (p2.IsMidPoint) AddAdjacentPoint(face, p2);

            var polygon = new Polygon(face.Vertices.Select(x => x.Location));
            var clip = new Plane(p1.Vector3, p2.Vector3, p1.Vector3 + face.Plane.Normal * 10);
            Polygon back, front;
            polygon.Split(clip, out back, out front);
            if (back == null || front == null) return;

            solid.Faces.Remove(face);
            face.Parent = null;

            CreateFace(back, solid, face);
            CreateFace(front, solid, face);

            solid.UpdateBoundingBox();

            MainTool.SetDirty(true, true);
        }

        private void VMSplitFace()
        {
            if (CanSplit())
            {
                Split(null);
            }
        }

        private void CreateFace(Polygon polygon, Solid parent, Face original)
        {
            var verts = polygon.Vertices;
            var f = new Face(Document.Map.IDGenerator.GetNextFaceID())
            {
                Parent = parent,
                Plane = new Plane(verts[0], verts[1], verts[2]),
                Colour = parent.Colour,
                Texture = original.Texture.Clone()
            };
            f.Vertices.AddRange(verts.Select(x => new Vertex(x, f)));
            f.UpdateBoundingBox();
            f.CalculateTextureCoordinates(true);
            parent.Faces.Add(f);
        }

        private void AddAdjacentPoint(Face face, VMPoint point)
        {
            var solid = face.Parent;
            var s = point.MidpointStart.Vector3;
            var e = point.MidpointEnd.Vector3;

            foreach (var f in solid.Faces.Where(x => x != face))
            {
                foreach (var edge in f.GetEdges())
                {
                    if (edge.Start == s && edge.End == e)
                    {
                        var idx = f.Vertices.FindIndex(x => x.Location == e);
                        f.Vertices.Insert(idx, new Vertex(point.Vector3, f));
                        return;
                    }
                    if (edge.Start == e && edge.End == s)
                    {
                        var idx = f.Vertices.FindIndex(x => x.Location == s);
                        f.Vertices.Insert(idx, new Vertex(point.Vector3, f));
                        return;
                    }
                }
            }
        }

        public override string GetName()
        {
            return "Standard";
        }

        public override string GetContextualHelp()
        {
            return
@"*Click* a vertex to select all points under the cursor.
 - Hold *control* to select multiple points.
 - Hold *shift* to only select the topmost point.
Drag vertices to move them around.

Select two (non-adjacent) points on a face to enable splitting.";
        }

        public override void ToolSelected(bool preventHistory)
        {
            _state = VMState.None;
            UpdateSplitEnabled();
            Mediator.Subscribe(HotkeysMediator.VMSplitFace, this);
        }

        public override void ToolDeselected(bool preventHistory)
        {
            _state = VMState.None;
            Mediator.UnsubscribeAll(this);
        }

        public override List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport2D viewport)
        {
            return MainTool.GetVerticesAtPoint(x, y, viewport);
        }

        public override List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport3D viewport)
        {
            return MainTool.GetVerticesAtPoint(x, y, viewport);
        }

        public override void DragStart(List<VMPoint> clickedPoints)
        {
            _state = VMState.Down;
            // throw new NotImplementedException();
            /*Editor.Instance.CaptureAltPresses = true;*/
        }

        public override void DragMove(Vector3 distance)
        {
            _state = VMState.Moving;
            // Move each selected point by the delta value
            foreach (var p in MainTool.GetSelectedPoints())
            {
                p.Move(distance);
            }

            //MainTool.SetDirty(false, false);
        }

        public override void DragEnd()
        {
            if (_state == VMState.Moving)
            {
                if (AutomaticallyMerge()) { CheckMergedVertices(); }
                else if (CanMerge()) {
                    GameMain.Instance.Popups.Add(new ConfirmPopup(
                        "Merge vertices?",
                        "Overlapping vertices detected! Merge vertices?") {
                        Buttons = new[]{
                            new ConfirmPopup.Button("Yes", CheckMergedVertices),
                            new ConfirmPopup.Button("No", () => MainTool.SetDirty(true, true) )
                        }.ToImmutableArray()
                    });
                }
                else { MainTool.SetDirty(true, true); }
            }
            _state = VMState.None;
            // throw new NotImplementedException();
            /*Editor.Instance.CaptureAltPresses = false;*/
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        private bool CanMerge()
        {
            foreach (var solid in MainTool.GetCopies())
            {
                foreach (var face in solid.Faces)
                {
                    for (var i = 0; i < face.Vertices.Count; i++)
                    {
                        var j = (i + 1) % face.Vertices.Count;
                        var v1 = face.Vertices[i];
                        var v2 = face.Vertices[j];

                        if (!v1.Location.EquivalentTo(v2.Location, 0.01m)) continue;
                        return true;
                    }
                }
            }
            return false;
        }

        private void CheckMergedVertices()
        {
            var mergedVertices = 0;
            var removedFaces = 0;
            foreach (var solid in MainTool.GetCopies())
            {
                foreach (var face in solid.Faces)
                {
                    // Remove adjacent duplicates
                    for (var i = 0; i < face.Vertices.Count; i++)
                    {
                        // Loop through to the start to cater for when the first & last vertices are equal
                        var j = (i + 1) % face.Vertices.Count;
                        var v1 = face.Vertices[i];
                        var v2 = face.Vertices[j];

                        if (!v1.Location.EquivalentTo(v2.Location, 0.01m)) continue;

                        // Two adjacent vertices are equivalent, remove the latter...
                        face.Vertices.RemoveAt(j);
                        mergedVertices++;

                        // Check i again with its new neighbour
                        i--;
                    }
                }

                // Remove empty faces from the solid
                removedFaces += solid.Faces.RemoveAll(x => x.Vertices.Count < 3);
            }

            // throw new NotImplementedException();
            /*((StandardControl)Control).ShowMergeResult(mergedVertices, removedFaces);*/
            MainTool.SetDirty(true, true);
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseLifted(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void KeyHit(ViewportBase viewport, ViewportEvent e)
        {
            var nudge = GetNudgeValue(e.KeyCode) ?? Vector3.Zero;
            var sel = MainTool.GetSelectedPoints();
            if (viewport is Viewport2D vp && _state == VMState.None && sel.Any())
            {
                var translate = vp.Expand(nudge);
                foreach (var p in sel)
                {
                    p.Move(translate);
                }
                CheckMergedVertices();
            }
        }

        public override void KeyLift(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame)
        {

        }

        public override void Render(ViewportBase viewport)
        {

        }

        public override void Render2D(Viewport2D viewport)
        {

        }

        public override void Render3D(Viewport3D viewport)
        {

        }

        public override void SelectionChanged()
        {
            UpdateSplitEnabled();
        }

        public override bool ShouldDeselect(List<VMPoint> vtxs)
        {
            return true;
        }

        public override bool NoSelection()
        {
            return false;
        }

        public override bool No3DSelection()
        {
            return false;
        }

        public override bool DrawVertices()
        {
            return true;
        }
    }
}

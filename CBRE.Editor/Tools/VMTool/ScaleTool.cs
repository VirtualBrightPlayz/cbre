﻿using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.Graphics;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Vertex = CBRE.DataStructures.MapObjects.Vertex;

namespace CBRE.Editor.Tools.VMTool
{
    public class ScaleTool : VMSubTool
    {
        private enum VMState
        {
            None,
            Down,
            Moving
        }

        private VMState _state;
        private VMPoint _origin;
        private decimal _prevValue;
        private Dictionary<VMPoint, Vector3> _originals;

        public ScaleTool(VMTool mainTool) : base(mainTool)
        {
            /*var sc = new ScaleControl();
            Control = sc;
            sc.ValueChanged += ValueChanged;
            sc.ValueReset += ValueReset;
            sc.ResetOrigin += ResetOrigin;*/
            _origin = new VMPoint { Vector3 = Vector3.Zero, Vertices = new List<Vertex>() };
        }

        private void ResetOrigin(object sender)
        {
            var points = MainTool.GetSelectedPoints().Select(x => x.Vector3).ToList();
            if (!points.Any()) points = MainTool.Points.Where(x => !x.IsMidPoint).Select(x => x.Vector3).ToList();
            if (!points.Any()) _origin.Vector3 = Vector3.Zero;
            else _origin.Vector3 = points.Aggregate(Vector3.Zero, (a, b) => a + b) / points.Count;
        }

        private void ValueChanged(object sender, decimal value)
        {
            MovePoints(value);
            _prevValue = value;
        }

        private void ValueReset(object sender, decimal value)
        {
            _prevValue = value;
            _originals = MainTool.Points.ToDictionary(x => x, x => x.Vector3);
        }

        public override void SelectionChanged()
        {
            throw new NotImplementedException();
            /*((ScaleControl)Control).ResetValue();
            if (MainTool.GetSelectedPoints().Any()) ResetOrigin(null);*/
        }

        public override bool ShouldDeselect(List<VMPoint> vtxs)
        {
            return !vtxs.Contains(_origin);
        }

        public override bool NoSelection()
        {
            return false;
        }

        public override bool No3DSelection()
        {
            return true;
        }

        public override bool DrawVertices()
        {
            return true;
        }

        private void MovePoints(decimal value)
        {
            var o = _origin.Vector3;
            // Move each selected point by the computed offset from the origin
            foreach (var p in MainTool.GetSelectedPoints())
            {
                var orig = _originals[p];
                var diff = orig - o;
                var move = o + diff * value / 100;
                p.Move(move - p.Vector3);
            }
            MainTool.SetDirty(false, true);
        }

        public override string GetName()
        {
            return "Scale";
        }

        public override string GetContextualHelp()
        {
            return
@"*Click* a vertex to select all points under the cursor.
 - Hold *control* to select multiple points.
 - Hold *shift* to only select the topmost point.
Move the origin point around by *clicking and dragging* it.";
        }

        public override void ToolSelected(bool preventHistory)
        {
            _state = VMState.None;
            _originals = MainTool.Points.ToDictionary(x => x, x => x.Vector3);
            ResetOrigin(null);
        }

        public override void ToolDeselected(bool preventHistory)
        {
            _state = VMState.None;
            _originals = null;
            Mediator.UnsubscribeAll(this);
        }

        public override List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport2D viewport)
        {
            var verts = MainTool.GetVerticesAtPoint(x, y, viewport);

            var p = viewport.ScreenToWorld(x, y);
            var d = 8 / viewport.Zoom; // Tolerance value = 8 pixels
            var c = viewport.Flatten(_origin.Vector3);
            if (p.X >= c.X - d && p.X <= c.X + d && p.Y >= c.Y - d && p.Y <= c.Y + d)
            {
                verts.Insert(0, _origin);
            }

            return verts;
        }

        public override List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport3D viewport)
        {
            return MainTool.GetVerticesAtPoint(x, y, viewport);
        }

        public override void DragStart(List<VMPoint> clickedPoints)
        {
            if (!clickedPoints.Contains(_origin)) return;
            _state = VMState.Down;
            throw new NotImplementedException();
            /*Editor.Instance.CaptureAltPresses = true;*/
        }

        public override void DragMove(Vector3 distance)
        {
            if (_state == VMState.None) return;
            _state = VMState.Moving;
            // Move the origin point by the delta value
            _origin.Move(distance);
        }

        public override void DragEnd()
        {
            _state = VMState.None;
            throw new NotImplementedException();
            /*Editor.Instance.CaptureAltPresses = false;*/
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
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
            if (nudge != null && viewport is Viewport2D vp && _state == VMState.None)
            {
                var translate = vp.Expand(nudge);
                _origin.Move(translate);
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
            var pos = viewport.Flatten(_origin.Vector3);

            throw new NotImplementedException();
            /*GL.Color3(Color.Cyan);
            GL.Begin(PrimitiveType.Lines);
            GLX.Circle(new Vector2d(pos.DX, pos.DY), 8, (double)viewport.Zoom);
            GL.End();
            GL.Begin(PrimitiveType.Points);
            GL.Vertex2(pos.DX, pos.DY);
            GL.End();*/
        }

        public override void Render3D(Viewport3D viewport)
        {

        }
    }
}

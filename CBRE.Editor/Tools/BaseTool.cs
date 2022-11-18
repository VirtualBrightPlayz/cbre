﻿using System;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Extensions;
using CBRE.Graphics;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Tools
{
    public abstract class BaseTool : IMediatorListener
    {
        public enum ToolUsage
        {
            View2D,
            View3D,
            Both
        }

        protected Vector3 SnapIfNeeded(Vector3 c)
        {
            return Document.Snap(c);
        }

        protected Vector3 SnapToSelection(Vector3 c, Viewport2D vp)
        {
            if (!Document.Map.SnapToGrid) return c;

            bool snap = true;
            /* TODO: reimplement
            var snap = (Select.SnapStyle == SnapStyle.SnapOnAlt && ViewportManager.Alt) ||
                       (Select.SnapStyle == SnapStyle.SnapOffAlt && !ViewportManager.Alt);*/

            if (!snap) return c;

            var snapped = c.Snap(Document.Map.GridSpacing);
            if (Document.Selection.InFaceSelection || Document.Selection.IsEmpty()) return snapped;

            // Try and snap the the selection box center
            var selBox = Document.Selection.GetSelectionBoundingBox();
            var selCenter = vp.Flatten(selBox.Center);
            if (DMath.Abs(selCenter.X - c.X) < selBox.Width / 10 && DMath.Abs(selCenter.Y - c.Y) < selBox.Height / 10) return selCenter;

            var objects = Document.Selection.GetSelectedObjects().ToList();

            // Try and snap to an object center
            foreach (var mo in objects)
            {
                if (!(mo is Entity) && !(mo is Solid)) continue;
                var center = vp.Flatten(mo.BoundingBox.Center);
                if (DMath.Abs(center.X - c.X) >= mo.BoundingBox.Width / 10) continue;
                if (DMath.Abs(center.Y - c.Y) >= mo.BoundingBox.Height / 10) continue;
                return center;
            }

            // Get all the edges of the selected objects
            var lines = objects.SelectMany(x =>
            {
                if (x is Entity) return x.BoundingBox.GetBoxLines();
                if (x is Solid) return ((Solid)x).Faces.SelectMany(f => f.GetLines());
                return new Line[0];
            }).Select(x => new Line(vp.Flatten(x.Start), vp.Flatten(x.End))).ToList();

            // Try and snap to an edge
            var closest = snapped;
            foreach (var line in lines)
            {
                // if the line and the grid are in the same spot, return the snapped point
                if (line.ClosestPoint(snapped).EquivalentTo(snapped)) return snapped;

                // Test for corners and midpoints within a 10% tolerance
                var pointTolerance = (line.End - line.Start).VectorMagnitude() / 10;
                if ((line.Start - c).VectorMagnitude() < pointTolerance) return line.Start;
                if ((line.End - c).VectorMagnitude() < pointTolerance) return line.End;

                var center = (line.Start + line.End) / 2;
                if ((center - c).VectorMagnitude() < pointTolerance) return center;

                // If the line is closer to the grid point, return the line
                var lineSnap = line.ClosestPoint(c);
                if ((closest - c).VectorMagnitude() > (lineSnap - c).VectorMagnitude()) closest = lineSnap;
            }
            return closest;
        }

        protected Vector3? GetNudgeValue(Keys k)
        {
            if (!Select.ArrowKeysNudgeSelection) { return null; }
            var keyboardState = Keyboard.GetState();
            var ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            var gridoff = Select.NudgeStyle == NudgeStyle.GridOffCtrl;
            var grid = (gridoff && !ctrl) || (!gridoff && ctrl);
            var val = grid ? Document.Map.GridSpacing : Select.NudgeUnits;
            switch (k)
            {
                case Keys.Left:
                    return new Vector3(-val, 0, 0);
                case Keys.Right:
                    return new Vector3(val, 0, 0);
                case Keys.Up:
                    return new Vector3(0, val, 0);
                case Keys.Down:
                    return new Vector3(0, -val, 0);
            }
            return null;
        }

        protected Documents.Document Document { get; set; }
        public ViewportBase Viewport { get; set; }
        public ToolUsage Usage { get; set; }

        public abstract string GetIcon();
        public abstract string GetName();
        public abstract HotkeyTool? GetHotkeyToolType();
        public abstract string GetContextualHelp();

        /*public virtual IEnumerable<KeyValuePair<string, Control>> GetSidebarControls()
        {
            yield break;
        }*/

        protected BaseTool()
        {
            Viewport = null;
            Usage = ToolUsage.View2D;
        }

        public void SetDocument(Documents.Document document)
        {
            Document = document;
            DocumentChanged();
        }

        public virtual void ToolSelected(bool preventHistory)
        {
            // Virtual
        }

        public virtual void ToolDeselected(bool preventHistory)
        {
            // Virtual
        }

        public virtual void DocumentChanged()
        {
            // Virtual
        }

        public virtual void Notify(Enum message, object data)
        {
            Mediator.ExecuteDefault(this, message, data);
        }

        public abstract void MouseEnter(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseLeave(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseClick(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseDoubleClick(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseLifted(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseWheel(ViewportBase viewport, ViewportEvent e);
        public abstract void MouseMove(ViewportBase viewport, ViewportEvent e);
        public virtual void MouseMoveBackground(ViewportBase viewport, ViewportEvent e) { }
        public virtual void KeyHitBackground(ViewportBase viewport, ViewportEvent e) { }
        public abstract void KeyHit(ViewportBase viewport, ViewportEvent e);
        public abstract void KeyLift(ViewportBase viewport, ViewportEvent e);
        public virtual void KeyUpBackground(ViewportBase viewport, ViewportEvent e) { }
        public abstract void UpdateFrame(ViewportBase viewport, FrameInfo frame);
        public abstract void Render(ViewportBase viewport);

        public virtual void KeyHit(ViewportEvent e) {
            return;
        }

        public virtual void KeyLift(ViewportEvent e) {
            return;
        }

        public virtual void Update() {
            return;
        }

        public virtual void ViewportUi(ViewportBase viewport) {
            return;
        }
        
        public virtual void UpdateGui() {
            return;
        }

        public virtual void PreRender(ViewportBase viewport)
        {
            return;
        }

        public virtual bool IsCapturingMouseWheel()
        {
            return false;
        }

        /// <summary>
        /// Intercepts a document hotkey. Returns false if the hotkey should not be executed.
        /// </summary>
        /// <param name="hotkeyMessage">The hotkey message</param>
        /// <param name="parameters">The hotkey parameters</param>
        /// <returns>False to prevent execution of the document hotkey</returns>
        public abstract HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters);

        /*public virtual void OverrideViewportContextMenu(ViewportContextMenu menu, Viewport2D vp, ViewportEvent e)
        {
            // Default: nothing...
        }*/
    }

    public enum HotkeyInterceptResult
    {
        Continue,
        Abort,
        SwitchToSelectTool
    }
}

﻿using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Transformations;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.MapObjects.Operations.EditOperations;
using CBRE.Editor.Actions.MapObjects.Selection;
using CBRE.Editor.Clipboard;
using CBRE.Editor.Tools.SelectTool.TransformationTools;
using CBRE.Editor.Tools.Widgets;
using CBRE.Graphics;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools.SelectTool
{
    /// <summary>
    /// The select tool is used to select objects in several different ways:
    /// 1. Single click in the 2D view will perform edge-detection selection
    /// 2. Single click in the 3D view allows ray-casting selection (with mouse wheel cycling)
    /// 3. Drawing a box in the 2D view and confirming it will select everything in the box
    /// </summary>
    class SelectTool : BaseBoxTool
    {
        private MapObject ChosenItemFor3DSelection { get; set; }
        private List<MapObject> IntersectingObjectsFor3DSelection { get; set; }
        public bool TransformState { get; set; } = false;

        private readonly List<TransformationTool> _tools;
        private TransformationTool _lastTool;
        private TransformationTool _currentTool;
        private List<Widget> _widgets;

        //private readonly SelectToolSidebarPanel _sidebarPanel;

        private Matrix? CurrentTransform { get; set; }

        public SelectTool()
        {
            Usage = ToolUsage.Both;
            _tools = new List<TransformationTool>
                         {
                            new ResizeTool(),
                            new RotateTool(),
                            new SkewTool(),
                            new ThreeDGizmosTool(ImGuizmoNET.OPERATION.TRANSLATE),
                            new ThreeDGizmosTool(ImGuizmoNET.OPERATION.ROTATE),
                            // new ThreeDGizmosTool(ImGuizmoNET.OPERATION.SCALE),
                         };
            _widgets = new List<Widget>();

            SetCurrentTool(_tools.First());

            /*_sidebarPanel = new SelectToolSidebarPanel();
            _sidebarPanel.ChangeTransformationTool += (sender, type) =>
            {
                var tool = _tools.FirstOrDefault(x => x.GetType() == type);
                if (tool != null) SetCurrentTool(tool);
            };
            _sidebarPanel.ToggleShow3DWidgets += (sender, show) =>
            {
                CBRE.Settings.Select.Show3DSelectionWidgets = show;
                SetCurrentTool(_currentTool);
            };*/
        }

        public override string GetIcon()
        {
            return "Tool_Select";
        }

        public override string GetName()
        {
            return "Select Tool";
        }

        public override HotkeyTool? GetHotkeyToolType()
        {
            return HotkeyTool.Selection;
        }

        protected override Color BoxColour
        {
            get { return Color.Yellow; }
        }

        protected override Color FillColour
        {
            get { return Color.FromArgb(CBRE.Settings.View.SelectionBoxBackgroundOpacity, Color.Gray); }
        }

        /*public override IEnumerable<KeyValuePair<string, Control>> GetSidebarControls()
        {
            yield return new KeyValuePair<string, Control>(GetName(), _sidebarPanel);
        }*/

        public override string GetContextualHelp()
        {
            return "*Click* to select an object.\n" +
                   "In the 3D view, *click and hold* and use the *mouse wheel* to cycle through objects behind the cursor.\n" +
                   "In the 2D view, *click the selection box* to cycle between manipulation modes.";
        }

        public override void ToolSelected(bool preventHistory)
        {
            TransformState = false;
            SetCurrentTool(_currentTool);
            IgnoreGroupingChanged();

            Mediator.Subscribe(EditorMediator.SelectionChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeStructureChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeObjectsChanged, this);
            Mediator.Subscribe(EditorMediator.IgnoreGroupingChanged, this);

            SelectionChanged();
        }

        public override void ToolDeselected(bool preventHistory)
        {
            TransformState = false;
            SetCurrentTool(null);
        }

        private bool IgnoreGrouping()
        {
            return Document.Map.IgnoreGrouping;
        }

        private void IgnoreGroupingChanged()
        {
            var selected = Document.Selection.GetSelectedObjects().ToList();
            var select = new List<MapObject>();
            var deselect = new List<MapObject>();
            if (Document.Map.IgnoreGrouping)
            {
                deselect.AddRange(selected.Where(x => x.HasChildren));
            }
            else
            {
                var parents = selected.Select(x => x.FindTopmostParent(y => y is Group || y is Entity) ?? x).Distinct();
                foreach (var p in parents)
                {
                    var children = p.GetSelfAndAllChildren();
                    var leaves = children.Where(x => !x.HasChildren);
                    if (leaves.All(selected.Contains)) select.AddRange(children.Where(x => !selected.Contains(x)));
                    else deselect.AddRange(children.Where(selected.Contains));
                }
            }
            if (deselect.Any() || select.Any())
            {
                Document.PerformAction("Apply group selection", new ChangeSelection(select, deselect));
            }
        }

        #region Current tool

        private void SetCurrentTool(TransformationTool tool)
        {
            if (tool != null) _lastTool = tool;
            _currentTool = tool;
            //throw new NotImplementedException();
            //_sidebarPanel.TransformationToolChanged(_currentTool);
            _widgets = (_currentTool == null || !CBRE.Settings.Select.Show3DSelectionWidgets) ? new List<Widget>() : _currentTool.GetWidgets(Document).ToList();
            foreach (var widget in _widgets)
            {
                widget.OnTransforming = OnWidgetTransforming;
                widget.OnTransformed = OnWidgetTransformed;
                widget.SelectionChanged();
            }
        }

        private void OnWidgetTransformed(Matrix transformation)
        {
            TransformState = false;
            if (transformation != null)
            {
                ExecuteTransform("Manipulate", CreateMatrixMultTransformation(transformation), false);
            }

            Document.EndSelectionTransform();
        }

        private void OnWidgetTransforming(Matrix transformation)
        {
            TransformState = true;
            if (transformation != null) Document.SetSelectListTransform(transformation);
        }

        private void DocumentTreeStructureChanged()
        {
            SelectionChanged();
        }

        private void DocumentTreeObjectsChanged(IEnumerable<MapObject> objects)
        {
            if (objects.Any(x => x.IsSelected))
            {
                SelectionChanged();
            }
        }

        private void SelectionChanged()
        {
            TransformState = false;
            if (Document == null) return;
            var selectedObjects = Document.Selection.GetSelectedObjects().ToArray();
            var types = selectedObjects.Select(o => o.GetType()).Distinct().ToArray();
            foreach (var t in types) {
                Debug.WriteLine($"{t}: {selectedObjects.Count(o => o.GetType() == t)}");
            }
            
            UpdateBoxBasedOnSelection();
            if (State.Action != BoxAction.ReadyToResize && _currentTool != null) SetCurrentTool(null);
            else if (State.Action == BoxAction.ReadyToResize && _currentTool == null) SetCurrentTool(_lastTool ?? _tools[0]);

            foreach (var widget in _widgets) widget.SelectionChanged();
        }

        /// <summary>
        /// Updates the box based on the currently selected objects.
        /// </summary>
        private void UpdateBoxBasedOnSelection()
        {
            if (Document.Selection.IsEmpty())
            {
                State.BoxStart = State.BoxEnd = null;
                State.Action = BoxAction.ReadyToDraw;
            }
            else
            {
                State.Action = BoxAction.ReadyToResize;
                var box = Document.Selection.GetSelectionBoundingBox();
                State.BoxStart = box.Start;
                State.BoxEnd = box.End;
            }
            OnBoxChanged();
        }

        #endregion

        #region Widget
        private bool WidgetAction(Action<Widget, ViewportBase, ViewportEvent> action, ViewportBase viewport, ViewportEvent? ev)
        {
            if (_widgets == null) return false;
            foreach (var widget in _widgets)
            {
                action(widget, viewport, ev ?? default);
                if (ev is { Handled: true }) return true;
            }
            return false;
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseMove(vp, ev), viewport, e)) return;
            base.MouseMove(viewport, e);
        }

        public override void MouseLifted(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseLifted(vp, ev), viewport, e)) return;
            base.MouseLifted(viewport, e);
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseClick(vp, ev), viewport, e)) return;
            base.MouseClick(viewport, e);
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseEnter(vp, ev), viewport, e)) return;
            base.MouseEnter(viewport, e);
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseLeave(vp, ev), viewport, e)) return;
            base.MouseLeave(viewport, e);
        }

        public override void PreRender(ViewportBase viewport)
        {
            WidgetAction((w, vp, ev) => w.PreRender(vp), viewport, null);
            base.PreRender(viewport);
        }

        public override void Render(ViewportBase viewport)
        {
            WidgetAction((w, vp, ev) => w.Render(vp), viewport, null);
            base.Render(viewport);
        }

        public override void ViewportUi(ViewportBase viewport) {
            WidgetAction((w, vp, ev) => w.ViewportUi(vp), viewport, null);
            base.ViewportUi(viewport);
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame)
        {
            WidgetAction((w, vp, ev) => w.UpdateFrame(vp, frame), viewport, null);
            base.UpdateFrame(viewport, frame);
        }

        #endregion

        #region Perform selection

        /// <summary>
        /// If ignoreGrouping is disabled, this will convert the list of objects into their topmost group or entity.
        /// If ignoreGrouping is enabled, this will remove objects that have children from the list.
        /// </summary>
        /// <param name="objects">The object list to normalise</param>
        /// <param name="ignoreGrouping">True if grouping is being ignored</param>
        /// <returns>The normalised list of objects</returns>
        private static IEnumerable<MapObject> NormaliseSelection(IEnumerable<MapObject> objects, bool ignoreGrouping)
        {
            return ignoreGrouping
                       ? objects.Where(x => !x.HasChildren)
                       : objects.Select(x => x.FindTopmostParent(y => y is Group || y is Entity) ?? x).Distinct().SelectMany(x => x.GetSelfAndAllChildren());
        }

        /// <summary>
        /// Deselect (first) a list of objects and then select (second) another list.
        /// </summary>
        /// <param name="objectsToDeselect">The objects to deselect</param>
        /// <param name="objectsToSelect">The objects to select</param>
        /// <param name="deselectAll">If true, this will ignore the objectToDeselect parameter and just deselect everything</param>
        /// <param name="ignoreGrouping">If true, object groups will be ignored</param>
        private void SetSelected(IEnumerable<MapObject> objectsToDeselect, IEnumerable<MapObject> objectsToSelect, bool deselectAll, bool ignoreGrouping)
        {
            if (objectsToDeselect == null) objectsToDeselect = new MapObject[0];
            if (objectsToSelect == null) objectsToSelect = new MapObject[0];

            if (deselectAll)
            {
                objectsToDeselect = Document.Selection.GetSelectedObjects();
                _lastTool = null;
            }

            // Normalise selections
            objectsToDeselect = NormaliseSelection(objectsToDeselect.Where(x => x != null), ignoreGrouping);
            objectsToSelect = NormaliseSelection(objectsToSelect.Where(x => x != null), ignoreGrouping);

            // The following line breaks undo, don't uncomment unless you know what you're doing!
            //objectsToDeselect = objectsToDeselect.Where(x => !objectsToSelect.Contains(x));

            // Perform selections
            var deselected = objectsToDeselect.ToList();
            var selected = objectsToSelect.ToList();

            Document.PerformAction("Selection changed", new ChangeSelection(selected, deselected));
        }

        #endregion

        #region Double Click
        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Don't show Object Properties while navigating the view, because mouse cursor will be hidden
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Space)) { return; }

            if (WidgetAction((w, vp, ev) => w.MouseDoubleClick(vp, ev), viewport, e)) { return; }

            if (CBRE.Settings.Select.DoubleClick3DAction == DoubleClick3DAction.Nothing) { return; }
            if (viewport is Viewport3D && !Document.Selection.IsEmpty()) {
                if (CBRE.Settings.Select.DoubleClick3DAction == DoubleClick3DAction.ObjectProperties /*&& !ObjectPropertiesDialog.IsShowing*/) {
                    Mediator.Publish(HotkeysMediator.ObjectProperties);
                }
                else if (CBRE.Settings.Select.DoubleClick3DAction == DoubleClick3DAction.TextureTool) {
                    Mediator.Publish(HotkeysMediator.SwitchTool, HotkeyTool.Texture);
                }
            }
        }
        #endregion

        #region 3D interaction

        protected override void MouseMove3D(Viewport3D viewport, ViewportEvent e)
        {
            base.MouseMove3D(viewport, e);
        }

        private Vector3? GetIntersectionPoint(MapObject obj, Line line)
        {
            if (obj == null) { return null; }

            var solid = obj as Solid;
            if (solid == null) return obj.GetIntersectionPoint(line);

            return solid.Faces.Where(x => x.Opacity > 0 && !x.IsHidden)
                .Select(x => x.GetIntersectionPoint(line))
                .Where(x => x != null)
                .OrderBy(x => (x.Value - line.Start).VectorMagnitude())
                .FirstOrDefault();
        }

        /// <summary>
        /// When the mouse is pressed in the 3D view, we want to select the clicked object.
        /// </summary>
        /// <param name="viewport">The viewport that was clicked</param>
        /// <param name="e">The click event</param>
        protected override void MouseDown3D(Viewport3D viewport, ViewportEvent e)
        {
            if (TransformState) return;
            if (e.Button == MouseButtons.Right) {
                var idx = _tools.IndexOf(_currentTool);
                SetCurrentTool(_tools[(idx + 1) % _tools.Count]);
                return;
            }
            var keyboardState = Keyboard.GetState();
            // Do not perform selection if space is down
            if (CBRE.Settings.View.Camera3DPanRequiresMouseClick && keyboardState.IsKeyDown(Keys.Space)) return;

            // First, get the ray that is cast from the clicked point along the viewport frustrum
            var ray = viewport.CastRayFromScreen(e.X, e.Y);

            // Grab all the elements that intersect with the ray
            var hits = Document.Map.WorldSpawn.GetAllNodesIntersectingWith(ray);

            // Sort the list of intersecting elements by distance from ray origin
            IntersectingObjectsFor3DSelection = hits
                .Select(x => new { Item = x, Intersection = GetIntersectionPoint(x, ray) })
                .Where(x => x.Intersection != null)
                .OrderBy(x => (x.Intersection.Value - ray.Start).VectorMagnitude())
                .Select(x => x.Item)
                .ToList();

            // By default, select the closest object
            ChosenItemFor3DSelection = IntersectingObjectsFor3DSelection.FirstOrDefault();

            // If Ctrl is down and the object is already selected, we should deselect it instead.
            var list = new[] { ChosenItemFor3DSelection };
            var desel = ChosenItemFor3DSelection != null && ViewportManager.Ctrl && ChosenItemFor3DSelection.IsSelected;
            SetSelected(desel ? list : null, desel ? null : list, !ViewportManager.Ctrl, IgnoreGrouping());

            State.ActiveViewport = null;
        }

        /// <summary>
        /// Once the mouse is released in the 3D view, the 3D select cycle has finished.
        /// </summary>
        /// <param name="viewport">The 3D viewport</param>
        /// <param name="e">The mouse event</param>
        protected override void MouseUp3D(Viewport3D viewport, ViewportEvent e)
        {
            IntersectingObjectsFor3DSelection = null;
            ChosenItemFor3DSelection = null;
        }

        /// <summary>
        /// When the mouse wheel is scrolled while the mouse is down in the 3D view, cycle through the candidate elements.
        /// </summary>
        /// <param name="viewport">The viewport that was scrolled</param>
        /// <param name="e">The scroll event</param>
        public override void MouseWheel(ViewportBase viewport, ViewportEvent e)
        {
            if (WidgetAction((w, vp, ev) => w.MouseWheel(vp, ev), viewport, e)) return;

            // If we're not in 3D cycle mode, carry on
            if (!(viewport is Viewport3D)
                || IntersectingObjectsFor3DSelection == null
                || ChosenItemFor3DSelection == null)
            {
                return;
            }

            var desel = new List<MapObject>();
            var sel = new List<MapObject>();

            // Select (or deselect) the current element
            if (ChosenItemFor3DSelection.IsSelected) desel.Add(ChosenItemFor3DSelection);
            else sel.Add(ChosenItemFor3DSelection);

            // Get the index of the current element
            var index = IntersectingObjectsFor3DSelection.IndexOf(ChosenItemFor3DSelection);
            if (index < 0) return;

            // Move the index in the mouse wheel direction, cycling if needed
            var dir = e.Delta / Math.Abs(e.Delta);
            index = (index + dir) % IntersectingObjectsFor3DSelection.Count;
            if (index < 0) index += IntersectingObjectsFor3DSelection.Count;

            ChosenItemFor3DSelection = IntersectingObjectsFor3DSelection[index];

            // Select (or deselect) the new current element
            if (ChosenItemFor3DSelection.IsSelected) desel.Add(ChosenItemFor3DSelection);
            else sel.Add(ChosenItemFor3DSelection);

            SetSelected(desel, sel, false, IgnoreGrouping());

            State.ActiveViewport = null;
        }

        /// <summary>
        /// The select tool captures the mouse wheel when the mouse is down in the 3D viewport
        /// </summary>
        /// <returns>True if the select tool is capturing wheel events</returns>
        public override bool IsCapturingMouseWheel()
        {
            return IntersectingObjectsFor3DSelection != null
                   && IntersectingObjectsFor3DSelection.Any()
                   && ChosenItemFor3DSelection != null;
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters)
        {
            return HotkeyInterceptResult.Continue;
        }

        #endregion

        #region 2D interaction

        protected override MouseCursor CursorForHandle(ResizeHandle handle)
        {
            var def = base.CursorForHandle(handle);
            return _currentTool == null || handle == ResizeHandle.Center
                       ? def
                       : _currentTool.CursorForHandle(handle) ?? def;
        }

        /// <summary>
        /// When the mouse is hovering over the box, do collision tests against the handles and change the cursor if needed.
        /// </summary>
        /// <param name="viewport">The viewport</param>
        /// <param name="e">The mouse event</param>
        protected override void MouseHoverWhenDrawn(Viewport2D viewport, ViewportEvent e)
        {
            if (_currentTool == null)
            {
                base.MouseHoverWhenDrawn(viewport, e);
                return;
            }

            var padding = 7 / viewport.Zoom;

            viewport.Cursor = MouseCursor.Arrow;
            State.Action = BoxAction.Drawn;
            State.ActiveViewport = null;

            var now = viewport.ScreenToWorld(e.X, viewport.Height - e.Y);
            var start = viewport.Flatten(State.BoxStart ?? Vector3.Zero);
            var end = viewport.Flatten(State.BoxEnd ?? Vector3.Zero);

            var ccs = new Vector3(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), 0);
            var cce = new Vector3(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), 0);

            // Check center handle
            if (now.X > ccs.X && now.X < cce.X && now.Y > ccs.Y && now.Y < cce.Y)
            {
                State.Handle = ResizeHandle.Center;
                State.ActiveViewport = viewport;
                State.Action = BoxAction.ReadyToResize;
                viewport.Cursor = CursorForHandle(State.Handle);
                return;
            }

            // Check other handles
            foreach (var handle in _currentTool.GetHandles(start, end, viewport.Zoom).Where(x => _currentTool.FilterHandle(x.Item1)))
            {
                var x = handle.Item2;
                var y = handle.Item3;
                if (now.X < x - padding || now.X > x + padding || now.Y < y - padding || now.Y > y + padding) continue;
                State.Handle = handle.Item1;
                State.ActiveViewport = viewport;
                State.Action = BoxAction.ReadyToResize;
                viewport.Cursor = CursorForHandle(State.Handle);
                return;
            }
        }

        /// <summary>
        /// The select tool will deselect all selected objects if ctrl is not held down when drawing a box.
        /// </summary>
        /// <param name="viewport">The viewport to draw in</param>
        /// <param name="e">The mouse event</param>
        protected override void LeftMouseDownToDraw(Viewport2D viewport, ViewportEvent e)
        {
            // If we've clicked outside a selection box and not holding down control, clear the selection
            if (!Document.Selection.IsEmpty() && !ViewportManager.Ctrl)
            {
                SetSelected(null, null, true, IgnoreGrouping());
            }

            // We're drawing a selection box, so clear the current tool
            SetCurrentTool(null);

            base.LeftMouseDownToDraw(viewport, e);
        }

        private MapObject SelectionTest(Viewport2D viewport, ViewportEvent e)
        {
            // Create a box to represent the click, with a tolerance level
            var unused = viewport.GetUnusedCoordinate(new Vector3(100000, 100000, 100000));
            var tolerance = 4 / viewport.Zoom; // Selection tolerance of four pixels
            var used = viewport.Expand(new Vector3(tolerance, tolerance, 0));
            var add = used + unused;
            var click = viewport.Expand(viewport.ScreenToWorld(e.X, viewport.Height - e.Y));
            var box = new Box(click - add, click + add);

            var centerHandles = CBRE.Settings.Select.DrawCenterHandles;
            var centerOnly = CBRE.Settings.Select.ClickSelectByCenterHandlesOnly;
            // Get the first element that intersects with the box, selecting or deselecting as needed
            return Document.Map.WorldSpawn.GetAllNodesIntersecting2DLineTest(box, centerHandles, centerOnly).FirstOrDefault();
        }

        /// <summary>
        /// If the mouse is single-clicked in a 2D viewport, select the closest element that is under the cursor
        /// </summary>
        /// <param name="viewport">The 2D viewport</param>
        /// <param name="e">The mouse event</param>
        protected override void LeftMouseClick(Viewport2D viewport, ViewportEvent e)
        {
            var seltest = SelectionTest(viewport, e);
            if (seltest != null)
            {
                var list = new[] { seltest };
                SetSelected(seltest.IsSelected ? list : null, seltest.IsSelected ? null : list, false, IgnoreGrouping());
            }

            base.LeftMouseClick(viewport, e);
            SelectionChanged();
        }

        protected override void LeftMouseClickOnResizeHandle(Viewport2D viewport, ViewportEvent e)
        {
            base.LeftMouseClickOnResizeHandle(viewport, e);

            if (_currentTool == null) return;

            if (ViewportManager.Ctrl)
            {
                var seltest = SelectionTest(viewport, e);
                if (seltest != null)
                {
                    var list = new[] { seltest };
                    SetSelected(seltest.IsSelected ? list : null, seltest.IsSelected ? null : list, false, IgnoreGrouping());
                    SelectionChanged();
                    return;
                }
            }

            // Cycle through active tools
            var idx = _tools.IndexOf(_currentTool);
            SetCurrentTool(_tools[(idx + 1) % _tools.Count]);
        }

        private Matrix GetTransformMatrix(Viewport2D viewport, ViewportEvent e)
        {
            if (_currentTool == null) return null;
            return State.Handle == ResizeHandle.Center
                       ? _tools.OfType<ResizeTool>().First().GetTransformationMatrix(viewport, e, State, Document, _widgets)
                       : _currentTool.GetTransformationMatrix(viewport, e, State, Document, _widgets);
        }

        protected override void LeftMouseUpDrawing(Viewport2D viewport, ViewportEvent e)
        {
            base.LeftMouseUpDrawing(viewport, e);
            if (CBRE.Settings.Select.AutoSelectBox)
            {
                BoxDrawnConfirm(viewport);
            }
        }

        protected override void LeftMouseUpResizing(Viewport2D viewport, ViewportEvent e)
        {
            if (_currentTool == null)
            {
                base.LeftMouseUpResizing(viewport, e);
                return;
            }

            // Execute the transform on the selection
            var transformation = GetTransformMatrix(viewport, e);
            if (transformation != null)
            {
                var createClone = ViewportManager.Shift && State.Handle == ResizeHandle.Center;
                ExecuteTransform(_currentTool.GetTransformName(), CreateMatrixMultTransformation(transformation), createClone);
            }
            Document.EndSelectionTransform();
            State.ActiveViewport = null;
            State.Action = BoxAction.Drawn;

            SelectionChanged();
        }

        protected override Vector3? GetResizeOrigin(Viewport2D viewport)
        {
            if (State.Action == BoxAction.Resizing && State.Handle == ResizeHandle.Center && !Document.Selection.IsEmpty())
            {
                var sel = Document.Selection.GetSelectedParents().ToList();
                if (sel.Count == 1 && sel[0] is Entity && !sel[0].HasChildren)
                {
                    return viewport.Flatten(((Entity)sel[0]).Origin);
                }
            }
            return base.GetResizeOrigin(viewport);
        }

        protected override void MouseDraggingToResize(Viewport2D viewport, ViewportEvent e)
        {
            if (_currentTool == null)
            {
                base.MouseDraggingToResize(viewport, e);
                return;
            }

            State.Action = BoxAction.Resizing;
            CurrentTransform = GetTransformMatrix(viewport, e);
            if (CurrentTransform != null)
            {
                Document.SetSelectListTransform(CurrentTransform);
                var box = new Box(State.PreTransformBoxStart ?? Vector3.Zero, State.PreTransformBoxEnd ?? Vector3.Zero);
                var trans = CreateMatrixMultTransformation(CurrentTransform);
                Mediator.Publish(EditorMediator.SelectionBoxChanged, box.Transform(trans));
            }
            else
            {
                //OnBoxChanged();
            }
        }

        public override void KeyHit(ViewportBase viewport, ViewportEvent e) {
            if (!e.MouseOver) { return; }
            var nudge = GetNudgeValue(e.KeyCode);
            if (nudge.HasValue && viewport is Viewport2D vp && (State.Action == BoxAction.ReadyToResize || State.Action == BoxAction.Drawn) && !Document.Selection.IsEmpty())
            {
                var translate = vp.Expand(nudge.Value);
                var transformation = Matrix.Translation(translate);
                ExecuteTransform("Nudge", CreateMatrixMultTransformation(transformation), ViewportManager.Shift);
                SelectionChanged();
            }
            base.KeyHit(viewport, e);
        }

        #endregion

        #region Box drawn cancel/confirm

        /// <summary>
        /// Once a box is confirmed, we select all element intersecting with the box (contained within if shift is down).
        /// </summary>
        /// <param name="viewport">The viewport that the box was confirmed in</param>
        public override void BoxDrawnConfirm(ViewportBase viewport)
        {
            // don't do anything if the current tool is not null
            if (_currentTool != null) return;

            Box boundingbox;
            if (GetSelectionBox(out boundingbox))
            {
                // If the shift key is down, select all brushes that are fully contained by the box
                // If select by handles only is on, select all brushes with centers inside the box
                // Otherwise, select all brushes that intersect with the box
                Func<Box, IEnumerable<MapObject>> selector = x => Document.Map.WorldSpawn.GetAllNodesIntersectingWith(x);
                if (CBRE.Settings.Select.BoxSelectByCenterHandlesOnly) selector = x => Document.Map.WorldSpawn.GetAllNodesWithCentersContainedWithin(x);
                if (ViewportManager.Shift) selector = x => Document.Map.WorldSpawn.GetAllNodesContainedWithin(x);

                var nodes = selector(boundingbox).ToList();
                SetSelected(null, nodes, false, IgnoreGrouping());
            }
            base.BoxDrawnConfirm(viewport);
            SelectionChanged();
        }

        public override void BoxDrawnCancel(ViewportBase viewport)
        {
            // don't do anything if the current tool is not null
            if (_currentTool != null) return;

            base.BoxDrawnCancel(viewport);
            SelectionChanged();
        }

        #endregion

        #region Render

        protected override bool ShouldRenderResizeBox(Viewport2D viewport)
        {
            if (_currentTool != null)
            {
                return State.Action == BoxAction.ReadyToResize && State.Handle == ResizeHandle.Center;
            }
            return base.ShouldRenderResizeBox(viewport);
        }

        /// <summary>
        /// Returns true if the handles should be rendered, false otherwise
        /// </summary>
        /// <returns>Whether or not to draw the handles</returns>
        private bool ShouldRenderHandles()
        {
            return _currentTool != null
                   && State.Action != BoxAction.Resizing;
        }

        /// <summary>
        /// Render all the handles as squares or circles depending on class implementation
        /// </summary>
        /// <param name="viewport">The viewport to draw in</param>
        /// <param name="start">The start of the box</param>
        /// <param name="end">The end of the box</param>
        private void RenderHandles(Viewport2D viewport, Vector3 start, Vector3 end)
        {
            if (_currentTool == null) return;
            var circles = _currentTool.RenderCircleHandles;

            // Get the filtered list of handles, and convert them to vector locations
            var z = viewport.Zoom;
            var handles = _currentTool.GetHandles(start, end, viewport.Zoom)
                .Where(x => _currentTool.FilterHandle(x.Item1))
                .Select(x => new Vector2d(x.Item2, x.Item3))
                .ToList();

            // Draw the insides of the handles in white
            foreach (var handle in handles)
            {
                PrimitiveDrawing.Begin(PrimitiveType.TriangleFan);
                PrimitiveDrawing.SetColor(Color.White);
                if (circles) PrimitiveDrawing.Circle(new Vector3(handle.X, handle.Y, z), (double)(4m / viewport.Zoom));
                else PrimitiveDrawing.Square(new Vector3(handle.X, handle.Y, z), (double)(4m / viewport.Zoom));
                PrimitiveDrawing.End();
            }

            // Draw the borders of the handles in black
            foreach (var handle in handles) {
                PrimitiveDrawing.Begin(PrimitiveType.LineLoop);
                PrimitiveDrawing.SetColor(Color.Black);
                if (circles) PrimitiveDrawing.Circle(new Vector3(handle.X, handle.Y, z), (double)(4m / viewport.Zoom));
                else PrimitiveDrawing.Square(new Vector3(handle.X, handle.Y, z), (double)(4m / viewport.Zoom));
                PrimitiveDrawing.End();
            }
        }

        protected override void Render2D(Viewport2D viewport)
        {
            if (_currentTool == null)
            {
                base.Render2D(viewport);
                return;
            }

            var start = viewport.Flatten(State.BoxStart ?? Vector3.Zero);
            var end = viewport.Flatten(State.BoxEnd ?? Vector3.Zero);

            if (ShouldDrawBox(viewport))
            {
                RenderBox(viewport, start, end);
            }

            if (ShouldRenderSnapHandle(viewport))
            {
                RenderSnapHandle(viewport);
            }

            if (ShouldRenderResizeBox(viewport))
            {
                RenderResizeBox(viewport, start, end);
            }

            if (ShouldRenderHandles())
            {
                RenderHandles(viewport, start, end);
            }

            if (State.Action == BoxAction.Resizing && CurrentTransform != null)
            {
                RenderTransformBox(viewport);
            }
        }

        private void RenderTransformBox(Viewport2D viewport)
        {
            if (CurrentTransform == null) { return; }

            var box = new Box(State.PreTransformBoxStart ?? Vector3.Zero, State.PreTransformBoxEnd ?? Vector3.Zero);
            var trans = CreateMatrixMultTransformation(CurrentTransform);
            box = box.Transform(trans);
            var s = viewport.Flatten(box.Start);
            var e = viewport.Flatten(box.End);

            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            PrimitiveDrawing.SetColor(Color.FromArgb(64, BoxColour));

            PrimitiveDrawing.DottedLine(new Vector3(s.X, s.Y, e.Z), new Vector3(e.X, s.Y, e.Z), 4m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(s.X, e.Y, e.Z), new Vector3(e.X, e.Y, e.Z), 4m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(s.X, s.Y, e.Z), new Vector3(s.X, e.Y, e.Z), 4m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(e.X, s.Y, e.Z), new Vector3(e.X, e.Y, e.Z), 4m / viewport.Zoom);

            PrimitiveDrawing.End();

            //RenderBoxText(viewport, s, e);
        }

        #endregion

        #region Transform stuff

        /// <summary>
        /// Runs the transform on all the currently selected objects
        /// </summary>
        /// <param name="transformationName">The name of the transformation</param>
        /// <param name="transform">The transformation to apply</param>
        /// <param name="clone">True to create a clone before transforming the original.</param>
        private void ExecuteTransform(string transformationName, IUnitTransformation transform, bool clone)
        {
            if (clone) transformationName += "-clone";
            var objects = Document.Selection.GetSelectedParents().ToList();
            var name = String.Format("{0} {1} object{2}", transformationName, objects.Count, (objects.Count == 1 ? "" : "s"));

            var cad = new CreateEditDelete();
            var action = new ActionCollection(cad);

            if (clone)
            {
                // Copy the selection, transform it, and reselect
                var copies = ClipboardManager.CloneFlatHierarchy(Document, Document.Selection.GetSelectedObjects()).ToList();
                foreach (var mo in copies)
                {
                    mo.Transform(transform, Document.Map.GetTransformFlags());
                    if (CBRE.Settings.Select.KeepVisgroupsWhenCloning) continue;
                    foreach (var o in mo.GetSelfAndAllChildren()) o.Visgroups.Clear();
                }
                cad.Create(Document.Map.WorldSpawn.ID, copies);
                var sel = new ChangeSelection(copies.SelectMany(x => x.GetSelfAndAllChildren()), Document.Selection.GetSelectedObjects());
                action.Add(sel);
            }
            else
            {
                // Transform the selection
                cad.Edit(objects, new TransformEditOperation(transform, Document.Map.GetTransformFlags()));
            }

            // Execute the action
            Document.PerformAction(name, action);
        }

        private IUnitTransformation CreateMatrixMultTransformation(Matrix mat)
        {
            return new UnitMatrixMult(mat);
        }

        #endregion
    }
}

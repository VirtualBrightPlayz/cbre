using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.MapObjects.Selection;
using CBRE.Editor.Brushes;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Select = CBRE.Settings.Select;
using ImGuiNET;
using CBRE.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools
{
    public class BrushTool : BaseBoxTool
    {
        private Box _lastBox;
        private bool _updatePreview;
        private List<Face> _preview;

        public override string GetIcon()
        {
            return "Tool_Brush";
        }

        public override string GetName()
        {
            return "Brush Tool";
        }

        public override HotkeyTool? GetHotkeyToolType()
        {
            return HotkeyTool.Brush;
        }

        /*public override IEnumerable<KeyValuePair<string, Control>> GetSidebarControls()
        {
            yield return new KeyValuePair<string, Control>(GetName(), BrushManager.SidebarControl);
        }*/

        public override string GetContextualHelp()
        {
            return "Draw a box in the 2D view to define the size of the brush.\n" +
                   "Select the type of the brush to create in the sidebar.\n" +
                   "Press *enter* in the 2D view to create the brush.";
        }

        protected override Color BoxColour
        {
            get { return Color.Turquoise; }
        }

        protected override Color FillColour
        {
            get { return Color.FromArgb(CBRE.Settings.View.SelectionBoxBackgroundOpacity, Color.Green); }
        }

        public override void ToolSelected(bool preventHistory)
        {
            var sel = Document.Selection.GetSelectedObjects().OfType<Solid>().ToList();
            if (sel.Any())
            {
                _lastBox = new Box(sel.Select(x => x.BoundingBox));
            }
            else if (_lastBox == null)
            {
                var gs = Document.Map.GridSpacing;
                _lastBox = new Box(Vector3.Zero, new Vector3(gs, gs, gs));
            }
            _updatePreview = true;
        }

        public override void ToolDeselected(bool preventHistory)
        {
            _updatePreview = false;
        }

        public override void UpdateGui() {
            if (ImGui.BeginCombo("Brush shape", BrushManager.CurrentBrush?.Name ?? "<none>")) {
                foreach (var brush in BrushManager.Brushes) {
                    if (ImGui.Selectable(brush.Name)) {
                        BrushManager.CurrentBrush = brush;
                        _updatePreview = true;
                    }
                }
            }
        }

        protected override void OnBoxChanged()
        {
            _updatePreview = true;
            base.OnBoxChanged();
        }

        public override void MouseDown(ViewportBase viewport, ViewportEvent e) {
            if (BrushManager.CurrentBrush == null) return;
            base.MouseDown(viewport, e);
        }

        protected override void LeftMouseDownToDraw(Viewport2D viewport, ViewportEvent e)
        {
            base.LeftMouseDownToDraw(viewport, e);
            if (_lastBox == null) return;
            State.BoxStart += viewport.GetUnusedCoordinate(_lastBox.Start);
            State.BoxEnd += viewport.GetUnusedCoordinate(_lastBox.End);
            _updatePreview = true;
        }

        private void CreateBrush(Box bounds)
        {
            var brush = GetBrush(bounds, Document.Map.IDGenerator);
            if (brush == null) return;

            brush.IsSelected = Select.SelectCreatedBrush;
            IAction action = new Create(Document.Map.WorldSpawn.ID, brush);
            if (Select.SelectCreatedBrush && Select.DeselectOthersWhenSelectingCreation)
            {
                action = new ActionCollection(new ChangeSelection(new MapObject[0], Document.Selection.GetSelectedObjects()), action);
            }

            Document.PerformAction("Create " + BrushManager.CurrentBrush.Name.ToLower(), action);
        }

        private MapObject GetBrush(Box bounds, IDGenerator idg)
        {
            Box _bounds = new Box(bounds.Start, bounds.End);
            if ((_bounds.Start-_bounds.End).VectorMagnitude() > 1000000m) {
                _bounds = new Box(bounds.Start, ((bounds.End - bounds.Start).Normalise() * 1000000m) + bounds.Start);
            }
            var brush = BrushManager.CurrentBrush;
            var ti = TextureProvider.SelectedTexture;
            var texture = ti != null ? ti.Texture : null;
            var created = brush.Create(idg, bounds, texture, BrushManager.RoundCreatedVertices ? 0 : 2).ToList();
            if (created.Count > 1)
            {
                var g = new Group(idg.GetNextObjectID());
                created.ForEach(x => x.SetParent(g));
                g.UpdateBoundingBox();
                return g;
            }
            return created.FirstOrDefault();
        }

        public override void BoxDrawnConfirm(ViewportBase viewport)
        {
            var box = new Box(State.BoxStart, State.BoxEnd);
            if (box.Start.X != box.End.X && box.Start.Y != box.End.Y && box.Start.Z != box.End.Z)
            {
                CreateBrush(box);
                _lastBox = box;
            }
            _preview = null;
            base.BoxDrawnConfirm(viewport);
            if (Select.SwitchToSelectAfterCreation)
            {
                Mediator.Publish(HotkeysMediator.SwitchTool, HotkeyTool.Selection);
            }
            if (Select.ResetBrushTypeOnCreation)
            {
                Mediator.Publish(EditorMediator.ResetSelectedBrushType);
            }
        }

        public override void BoxDrawnCancel(ViewportBase viewport)
        {
            _lastBox = new Box(State.BoxStart, State.BoxEnd);
            _preview = null;
            base.BoxDrawnCancel(viewport);
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame)
        {
            if (_updatePreview && ShouldDrawBox(viewport))
            {
                var box = new Box(State.BoxStart, State.BoxEnd);
                var brush = GetBrush(box, new IDGenerator());
                _preview = new List<Face>();
                CollectFaces(_preview, new[] { brush });
                var color = GetRenderBoxColour();
                _preview.ForEach(x => { x.Colour = color; });
            }
            _updatePreview = false;
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters)
        {
            switch (hotkeyMessage)
            {
                case HotkeysMediator.OperationsPasteSpecial:
                case HotkeysMediator.OperationsPaste:
                    return HotkeyInterceptResult.SwitchToSelectTool;
            }
            return HotkeyInterceptResult.Continue;
        }

        /*public override void OverrideViewportContextMenu(ViewportContextMenu menu, Viewport2D vp, ViewportEvent e)
        {
            menu.Items.Clear();
            if (State.Handle == ResizeHandle.Center)
            {
                var item = new ToolStripMenuItem("Create Object");
                item.Click += (sender, args) => BoxDrawnConfirm(vp);
                menu.Items.Add(item);
            }
        }*/

        private Color GetRenderColour()
        {
            var col = GetRenderBoxColour();
            return Color.FromArgb(128, col);
        }

        protected override void Render2D(Viewport2D viewport)
        {
            base.Render2D(viewport);
            if (ShouldDrawBox(viewport) && _preview != null)
            {
                PrimitiveDrawing.Begin(PrimitiveType.LineList);
                PrimitiveDrawing.SetColor(GetRenderColour());
                var matrix = viewport.GetModelViewMatrix();
                PrimitiveDrawing.FacesWireframe(_preview, matrix.ToCbre());
                PrimitiveDrawing.End();
            }
        }

        protected override void Render3D(Viewport3D viewport)
        {
            base.Render3D(viewport);
            if (ShouldDraw3DBox() && _preview != null) {
                var matrix = viewport.GetModelViewMatrix();

                PrimitiveDrawing.Begin(PrimitiveType.TriangleList);
                PrimitiveDrawing.SetColor(GetRenderColour());
                PrimitiveDrawing.FacesSolid(_preview, matrix.ToCbre());
                PrimitiveDrawing.End();

                PrimitiveDrawing.Begin(PrimitiveType.LineList);
                PrimitiveDrawing.SetColor(GetRenderColour());
                PrimitiveDrawing.FacesWireframe(_preview, matrix.ToCbre());
                PrimitiveDrawing.End();
            }
        }

        private static void CollectFaces(List<Face> faces, IEnumerable<MapObject> list)
        {
            foreach (var mo in list)
            {
                if (mo is Solid)
                {
                    faces.AddRange(((Solid)mo).Faces);
                }
                else if (mo is Entity || mo is Group)
                {
                    CollectFaces(faces, mo.GetChildren());
                }
            }
        }
    }
}

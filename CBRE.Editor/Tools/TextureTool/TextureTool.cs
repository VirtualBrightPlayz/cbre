using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.MapObjects.Selection;
using CBRE.Editor.Documents;
using CBRE.Editor.History;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImGuiNET;
using CBRE.Graphics;
using Num = System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using CBRE.Editor.Popup;

namespace CBRE.Editor.Tools.TextureTool {
    public class TextureTool : BaseTool {

        #region Enums

        public enum SelectBehaviour {
            LiftSelect,
            Lift,
            Select,
            Apply,
            ApplyWithValues,
            AlignToView
        }

        public enum JustifyMode {
            Fit,
            Left,
            Right,
            Center,
            Top,
            Bottom
        }

        public enum AlignMode {
            Face,
            World
        }

        #endregion

        public struct TextureData {
            public AsyncTexture AsyncTexture;
            public TextureItem Texture;

            public TextureData(AsyncTexture asyncTexture, TextureItem texture) {
                AsyncTexture = asyncTexture;
                Texture = texture;
            }
        }

        //private readonly TextureApplicationForm _form;
        //private readonly TextureToolSidebarPanel _sidebarPanel;
        // private TexturePopupUI texturePopup;
        private TextureData _texture;
        private bool _showOffset = false;
        private SelectBehaviour _leftCombo = SelectBehaviour.LiftSelect;
        private SelectBehaviour _rightCombo = SelectBehaviour.ApplyWithValues;
        private double xscl = 1;
        private double yscl = 1;
        private double xoff = 0;
        private double yoff = 0;
        private double trot = 0;

        private class Popup : PopupUI {
            private readonly Action guiMethod;

            protected override bool hasOkButton => false;

            protected override void OnCloseButtonHit(ref bool shouldBeOpen) {
                GameMain.Instance.SelectedTool = GameMain.Instance.ToolBarItems
                    .Select(tb => tb.Tool)
                    .First(t => t is SelectTool.SelectTool);
            }

            public Popup(Action guiMethod) : base("Texture Tool", color: null) {
                this.guiMethod = guiMethod;
            }

            protected override void ImGuiLayout(out bool shouldBeOpen) {
                shouldBeOpen = GameMain.Instance.SelectedTool is TextureTool;
                guiMethod();
            }
        }

        public TextureTool() {
            Usage = ToolUsage.View3D;
            /*_form = new TextureApplicationForm();
            _form.PropertyChanged += TexturePropertyChanged;
            _form.TextureAlign += TextureAligned;
            _form.TextureApply += TextureApplied;
            _form.TextureJustify += TextureJustified;
            _form.HideMaskToggled += HideMaskToggled;
            _form.TextureChanged += TextureChanged;

            _sidebarPanel = new TextureToolSidebarPanel();
            _sidebarPanel.TileFit += TileFit;
            _sidebarPanel.RandomiseXShiftValues += RandomiseXShiftValues;
            _sidebarPanel.RandomiseYShiftValues += RandomiseYShiftValues;*/
        }

        public override void UpdateGui() {
            if (ImGui.BeginChild("Texture Tool")) {
                GuiElements();
            }
            ImGui.EndChild();
        }

        private void GuiElements() {
            if (ImGui.BeginCombo("Left Click", _leftCombo.ToString())) {
                var e = Enum.GetValues<SelectBehaviour>();
                for (int i = 0; i < e.Length; i++) {
                    if (ImGui.Selectable(e[i].ToString())) {
                        _leftCombo = e[i];
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.NewLine();
            if (ImGui.BeginCombo("Right Click", _rightCombo.ToString())) {
                var e = Enum.GetValues<SelectBehaviour>();
                for (int i = 0; i < e.Length; i++) {
                    if (ImGui.Selectable(e[i].ToString())) {
                        _rightCombo = e[i];
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.NewLine();
            ImGui.Checkbox("Show Offset Preview", ref _showOffset);
            if (ImGui.Button("Align World")) {
                TextureAligned(this, AlignMode.World);
            }

            if (ImGui.Button("Align Face")) {
                TextureAligned(this, AlignMode.Face);
            }

            ImGui.NewLine();
            ImGui.InputDouble("X Scale", ref xscl);
            ImGui.InputDouble("Y Scale", ref yscl);
            ImGui.NewLine();
            ImGui.InputDouble("X Offset", ref xoff);
            ImGui.InputDouble("Y Offset", ref yoff);
            ImGui.NewLine();
            ImGui.InputDouble("Rotation", ref trot);
            ImGui.NewLine();
            if (_texture.AsyncTexture.ImGuiTexture != IntPtr.Zero) {
                if (_showOffset) {
                    ImGui.Image(_texture.AsyncTexture.ImGuiTexture, new Num.Vector2(100f, 100f),
                        // new Num.Vector2(0, 0), new Num.Vector2(1, 1));
                        new Num.Vector2((float)xscl * (float)xoff / _texture.AsyncTexture.Width - (float)xscl,
                            (float)yscl * (float)yoff / _texture.AsyncTexture.Height - (float)yscl),
                        new Num.Vector2((float)xscl * (float)xoff / _texture.AsyncTexture.Width,
                            (float)yscl * (float)yoff / _texture.AsyncTexture.Height));
                }

                if (ImGui.ImageButton(_texture.AsyncTexture.ImGuiTexture, new Num.Vector2(100f, 100f))) {
                    GameMain.Instance.Popups.Add(new TexturePopupUI(t => {
                        _texture = t;
                        TextureChanged(this, t.Texture);
                    }));
                }
            }

            if (ImGui.Button("Apply")) {
                TextureApplied(this, _texture.Texture, xscl, yscl, xoff, yoff, trot);
            }
        }

        public override void DocumentChanged() {
            //throw new NotImplementedException();
            /*_form.Document = Document;*/
        }

        private void HideMaskToggled(object sender, bool hide) {
            Document.Map.HideFaceMask = !hide;
            Mediator.Publish(HotkeysMediator.ToggleHideFaceMask);
        }

        private void RandomiseXShiftValues(object sender, int min, int max) {
            if (Document.Selection.IsEmpty()) return;

            var rand = new Random();
            Action<Document, Face> action = (d, f) => {
                f.Texture.XShift = rand.Next(min, max + 1); // Upper bound is exclusive
                f.CalculateTextureCoordinates(true);
            };
            Document.PerformAction("Randomise X shift values", new EditFace(Document.Selection.GetSelectedFaces(), action, false));
        }

        private void RandomiseYShiftValues(object sender, int min, int max) {
            if (Document.Selection.IsEmpty()) return;

            var rand = new Random();
            Action<Document, Face> action = (d, f) => {
                f.Texture.YShift = rand.Next(min, max + 1); // Upper bound is exclusive
                f.CalculateTextureCoordinates(true);
            };
            Document.PerformAction("Randomise Y shift values", new EditFace(Document.Selection.GetSelectedFaces(), action, false));
        }

        private void TextureJustified(object sender, JustifyMode justifymode, bool treatasone) {
            TextureJustified(justifymode, treatasone, 1, 1);
        }

        private void TileFit(object sender, int tileX, int tileY) {
            throw new NotImplementedException();
            /*TextureJustified(JustifyMode.Fit, _form.ShouldTreatAsOne(), tileX, tileY);*/
        }

        private void TextureJustified(JustifyMode justifymode, bool treatasone, int tileX, int tileY) {
            if (Document.Selection.IsEmpty()) return;
            var boxAlignMode = (justifymode == JustifyMode.Fit)
                                   ? Face.BoxAlignMode.Center // Don't care about the align mode when centering
                                   : (Face.BoxAlignMode)Enum.Parse(typeof(Face.BoxAlignMode), justifymode.ToString());
            Cloud cloud = null;
            Action<Document, Face> action;
            if (treatasone) {
                // If we treat as one, it means we want to align to one great big cloud
                cloud = new Cloud(Document.Selection.GetSelectedFaces().SelectMany(x => x.Vertices).Select(x => x.Location));
            }

            if (justifymode == JustifyMode.Fit) {
                action = (d, x) => x.FitTextureToPointCloud(cloud ?? new Cloud(x.Vertices.Select(y => y.Location)), tileX, tileY);
            } else {
                action = (d, x) => x.AlignTextureWithPointCloud(cloud ?? new Cloud(x.Vertices.Select(y => y.Location)), boxAlignMode);
            }

            Document.PerformAction("Align texture", new EditFace(Document.Selection.GetSelectedFaces(), action, false));
        }

        private void TextureApplied(object sender, TextureItem texture, double xscl, double yscl, double xoff, double yoff, double trot) {
            var ti = texture.Texture;
            Action<Document, Face> action = (document, face) => {
                document.ObjectRenderer.RemoveFace(face);
                face.Texture.XScale = (decimal)xscl;
                face.Texture.YScale = (decimal)yscl;
                face.Texture.XShift = (decimal)xoff;
                face.Texture.YShift = (decimal)yoff;
                face.SetTextureRotation((decimal)trot);
                face.Texture.Name = texture.Name;
                face.Texture.Texture = ti;
                face.CalculateTextureCoordinates(false);
                document.ObjectRenderer.AddFace(face);
            };
            // When the texture changes, the entire list needs to be regenerated, can't do a partial update.
            Document.PerformAction("Apply texture", new EditFace(Document.Selection.GetSelectedFaces(), action, true));

            Mediator.Publish(EditorMediator.TextureSelected, texture);
        }

        private void TextureAligned(object sender, AlignMode align) {
            Action<Document, Face> action = (document, face) => {
                if (align == AlignMode.Face) face.AlignTextureToFace();
                else if (align == AlignMode.World) face.AlignTextureToWorld();
                face.CalculateTextureCoordinates(false);
            };

            Document.PerformAction("Align texture", new EditFace(Document.Selection.GetSelectedFaces(), action, false));
        }

        /*private void TexturePropertyChanged(object sender, TextureApplicationForm.CurrentTextureProperties properties) {
            if (Document.Selection.IsEmpty()) return;

            Action<Document, Face> action = (document, face) => {
                if (!properties.DifferentXScaleValues) face.Texture.XScale = properties.XScale;
                if (!properties.DifferentYScaleValues) face.Texture.YScale = properties.YScale;
                if (!properties.DifferentXShiftValues) face.Texture.XShift = properties.XShift;
                if (!properties.DifferentYShiftValues) face.Texture.YShift = properties.YShift;
                if (!properties.DifferentRotationValues) face.SetTextureRotation(properties.Rotation);
                face.CalculateTextureCoordinates(false);
            };

            Document.PerformAction("Modify texture properties", new EditFace(Document.Selection.GetSelectedFaces(), action, false));
        }*/

        private void TextureChanged(object sender, TextureItem texture) {
            Mediator.Publish(EditorMediator.TextureSelected, texture);
        }

        public override string GetIcon() {
            return "Tool_Texture";
        }

        public override string GetName() {
            return "Texture Application Tool";
        }

        public override HotkeyTool? GetHotkeyToolType() {
            return HotkeyTool.Texture;
        }

        public override string GetContextualHelp() {
            return "*Click* a face to select it\n" +
                   "*Ctrl+Click* to select multiple\n" +
                   "*Shift+Click* to select all faces of a solid";
        }

        /*public override IEnumerable<KeyValuePair<string, Control>> GetSidebarControls() {
            yield return new KeyValuePair<string, Control>("Texture Power Tools", _sidebarPanel);
        }*/

        public override void ToolSelected(bool preventHistory) {
            // _texture = GameMain.MenuTextures["Menu_Close"];
            var tmptex = TextureProvider.GetItem("tooltextures/remove_face");
            _texture = new TextureData(tmptex.Texture as AsyncTexture, tmptex);

            if (!preventHistory) {
                Document.History.AddHistoryItem(new HistoryAction("Switch selection mode", new ChangeToFaceSelectionMode(GetType(), Document.Selection.GetSelectedObjects())));
                var currentSelection = Document.Selection.GetSelectedObjects();
                Document.Selection.SwitchToFaceSelection();
                var newSelection = Document.Selection.GetSelectedFaces().Select(x => x.Parent);
                Document.RenderObjects(currentSelection.Union(newSelection));
            }

            var selection = Document.Selection.GetSelectedFaces().OrderBy(x => x.Texture.Texture == null ? 1 : 0).FirstOrDefault();
            if (selection != null) {
                var itemToSelect = TextureProvider.GetItem(selection.Texture.Name);/*
                                   ?? new TextureItem(null, selection.Texture.Name, );*/
                Mediator.Publish(EditorMediator.TextureSelected, itemToSelect);
            }

            Mediator.Subscribe(EditorMediator.TextureSelected, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeFacesChanged, this);
            Mediator.Subscribe(EditorMediator.SelectionChanged, this);

            if (!GameMain.Instance.Popups.Any(p => p is Popup)) {
                GameMain.Instance.Popups.Add(new Popup(GuiElements));
            }
        }

        public override void ToolDeselected(bool preventHistory) {
            var selected = Document.Selection.GetSelectedFaces().ToList();

            if (!preventHistory) {
                Document.History.AddHistoryItem(new HistoryAction("Switch selection mode", new ChangeToObjectSelectionMode(GetType(), selected)));
                var currentSelection = Document.Selection.GetSelectedFaces().Select(x => x.Parent);
                Document.Selection.SwitchToObjectSelection();
                var newSelection = Document.Selection.GetSelectedObjects();
                Document.RenderObjects(currentSelection.Union(newSelection));
            }

            Mediator.UnsubscribeAll(this);
            // throw new NotImplementedException();
            /*_form.Clear();
            _form.Hide();
            Mediator.UnsubscribeAll(this);*/
        }

        private void TextureSelected(TextureItem texture) {
            if (texture == null)
                return;
            _texture = new TextureData(texture.Texture as AsyncTexture, texture);
            /*_form.SelectTexture(texture);*/
        }

        private void SelectionChanged() {
            // throw new NotImplementedException();
            /*_form.SelectionChanged();*/
        }

        private void DocumentTreeFacesChanged() {
            // throw new NotImplementedException();
            /*_form.SelectionChanged();*/
        }

        public override void MouseDown(ViewportBase viewport, ViewportEvent e) {
            // throw new NotImplementedException();
            var vp = viewport as Viewport3D;
            if (vp == null || (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)) return;

            var behaviour = e.Button == MouseButtons.Left ? _leftCombo : _rightCombo;
                                // ? _form.GetLeftClickBehaviour(ViewportManager.Ctrl, ViewportManager.Shift, ViewportManager.Alt)
                                // : _form.GetRightClickBehaviour(ViewportManager.Ctrl, ViewportManager.Shift, ViewportManager.Alt);

            var ray = vp.CastRayFromScreen(e.X, e.Y);
            var hits = Document.Map.WorldSpawn.GetAllNodesIntersectingWith(ray).OfType<Solid>();
            var clickedFace = hits.SelectMany(f => f.Faces)
                .Select(x => new { Item = x, Intersection = x.GetIntersectionPoint(ray) })
                .Where(x => x.Intersection != null)
                .OrderBy(x => (x.Intersection.Value - ray.Start).VectorMagnitude())
                .Select(x => x.Item)
                .FirstOrDefault();

            if (clickedFace == null) return;

            var faces = new List<Face>();
            if (ViewportManager.Shift) faces.AddRange(clickedFace.Parent.Faces);
            else faces.Add(clickedFace);

            var firstSelected = Document.Selection.GetSelectedFaces().FirstOrDefault();
            var firstClicked = faces.FirstOrDefault(face => !String.IsNullOrWhiteSpace(face.Texture.Name));

            var ac = new ActionCollection();

            var select = new ChangeFaceSelection(
                ViewportManager.Ctrl ? faces.Where(x => !x.IsSelected) : faces,
                ViewportManager.Ctrl ? faces.Where(x => x.IsSelected) : Document.Selection.GetSelectedFaces().Where(x => !faces.Contains(x)));

            Action lift = () => {
                if (firstClicked == null) return;
                var itemToSelect = TextureProvider.GetItem(firstClicked.Texture.Name);
                                   //?? new TextureItem(null, firstClicked.Texture.Name, TextureFlags.Missing, 64, 64);
                xscl = (double)firstClicked.Texture.XScale;
                yscl = (double)firstClicked.Texture.YScale;
                xoff = (double)firstClicked.Texture.XShift;
                yoff = (double)firstClicked.Texture.YShift;
                trot = (double)firstClicked.Texture.Rotation;
                Mediator.Publish(EditorMediator.TextureSelected, itemToSelect);
            };

            switch (behaviour) {
                case SelectBehaviour.Select:
                    ac.Add(select);
                    break;
                case SelectBehaviour.LiftSelect:
                    lift();
                    ac.Add(select);
                    break;
                case SelectBehaviour.Lift:
                    lift();
                    break;
                case SelectBehaviour.Apply:
                case SelectBehaviour.ApplyWithValues:
                    // throw new NotImplementedException();
                    var item = _texture.Texture;
                    if (item != null) {
                        var texture = item.Texture;
                        ac.Add(new EditFace(faces, (document, face) => {
                            face.Texture.Name = item.Name;
                            face.Texture.Texture = texture;
                            if (behaviour == SelectBehaviour.ApplyWithValues && firstSelected != null) {
                                // Calculates the texture coordinates
                                face.AlignTextureWithFace(firstSelected);
                            } else if (behaviour == SelectBehaviour.ApplyWithValues) {
                                face.Texture.XScale = (decimal)xscl;
                                face.Texture.YScale = (decimal)yscl;
                                face.Texture.XShift = (decimal)xoff;
                                face.Texture.YShift = (decimal)yoff;
                                face.SetTextureRotation((decimal)trot);
                            } else {
                                face.CalculateTextureCoordinates(true);
                            }
                        }, true));
                    }
                    break;
                case SelectBehaviour.AlignToView:
                    var right = vp.Camera.GetRight();
                    var up = vp.Camera.GetUp();
                    var loc = vp.Camera.EyePosition;
                    var point = new Vector3((decimal)loc.X, (decimal)loc.Y, (decimal)loc.Z);
                    var uaxis = new Vector3((decimal)right.X, (decimal)right.Y, (decimal)right.Z);
                    var vaxis = new Vector3((decimal)up.X, (decimal)up.Y, (decimal)up.Z);
                    ac.Add(new EditFace(faces, (document, face) => {
                        face.Texture.XScale = 1;
                        face.Texture.YScale = 1;
                        face.Texture.UAxis = uaxis;
                        face.Texture.VAxis = vaxis;
                        face.Texture.XShift = face.Texture.UAxis.Dot(point);
                        face.Texture.YShift = face.Texture.VAxis.Dot(point);
                        face.Texture.Rotation = 0;
                        face.CalculateTextureCoordinates(true);
                    }, false));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (!ac.IsEmpty()) {
                Document.PerformAction("Texture selection", ac);
            }
        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e) {
            //throw new NotImplementedException();
        }

        public override void Render(ViewportBase viewport) {
            if (Document.Map.HideFaceMask) return;

            foreach (var face in Document.Selection.GetSelectedFaces()) {
                var lineStart = face.BoundingBox.Center + face.Plane.Normal * 0.5m;
                var uEnd = lineStart + face.Texture.UAxis * 20;
                var vEnd = lineStart + face.Texture.VAxis * 20;

                PrimitiveDrawing.Begin(PrimitiveType.LineList);

                PrimitiveDrawing.SetColor(Color.Yellow);
                PrimitiveDrawing.Vertex3(lineStart.DX, lineStart.DY, lineStart.DZ);
                PrimitiveDrawing.Vertex3(uEnd.DX, uEnd.DY, uEnd.DZ);

                PrimitiveDrawing.SetColor(Color.FromArgb(0, 255, 0));
                PrimitiveDrawing.Vertex3(lineStart.DX, lineStart.DY, lineStart.DZ);
                PrimitiveDrawing.Vertex3(vEnd.DX, vEnd.DY, vEnd.DZ);
                
                PrimitiveDrawing.End();
            }

            // throw new NotImplementedException();
            /*TextureHelper.Unbind();
            GL.Begin(PrimitiveType.Lines);
            foreach (var face in Document.Selection.GetSelectedFaces()) {
                var lineStart = face.BoundingBox.Center + face.Plane.Normal * 0.5m;
                var uEnd = lineStart + face.Texture.UAxis * 20;
                var vEnd = lineStart + face.Texture.VAxis * 20;

                GL.Color3(Color.Yellow);
                GL.Vertex3(lineStart.DX, lineStart.DY, lineStart.DZ);
                GL.Vertex3(uEnd.DX, uEnd.DY, uEnd.DZ);

                GL.Color3(Color.FromArgb(0, 255, 0));
                GL.Vertex3(lineStart.DX, lineStart.DY, lineStart.DZ);
                GL.Vertex3(vEnd.DX, vEnd.DY, vEnd.DZ);
            }
            GL.End();*/
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters) {
            switch (hotkeyMessage) {
                case HotkeysMediator.OperationsCopy:
                case HotkeysMediator.OperationsCut:
                case HotkeysMediator.OperationsPaste:
                case HotkeysMediator.OperationsPasteSpecial:
                    return HotkeyInterceptResult.Abort;
                case HotkeysMediator.OperationsDelete:
                    var faces = Document.Selection.GetSelectedFaces().ToList();
                    var removeFaceTexture = TextureProvider.GetItem("tooltextures/remove_face");
                    if (faces.Count > 0 && removeFaceTexture != null) {
                        Action<Document, Face> action = (doc, face) => {
                            face.Texture.Name = "tooltextures/remove_face";
                            face.Texture.Texture = removeFaceTexture.Texture;
                            face.CalculateTextureCoordinates(false);
                        };
                        Document.PerformAction("Apply texture", new EditFace(faces, action, true));
                        Mediator.Publish(EditorMediator.TextureSelected, faces[0]);
                    }
                    return HotkeyInterceptResult.Abort;
            }
            return HotkeyInterceptResult.Continue;
        }

        public  void OperationsDelete() {

        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e) {
            // Not used
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e) {
            // Not used
        }

        public override void MouseUp(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void KeyUp(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) {
            //
        }
    }
}

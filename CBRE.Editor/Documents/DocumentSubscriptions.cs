﻿using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.DataStructures.GameData;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Transformations;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Groups;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.MapObjects.Operations.EditOperations;
using CBRE.Editor.Actions.MapObjects.Selection;
using CBRE.Editor.Actions.Visgroups;
using CBRE.Editor.Clipboard;
using CBRE.Editor.Compiling;
using CBRE.Editor.Enums;
using CBRE.Editor.Tools;
using CBRE.Editor.Tools.SelectTool;
using CBRE.Extensions;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Quaternion = CBRE.DataStructures.Geometric.Quaternion;
using CBRE.Editor.Popup;
using CBRE.Editor.Popup.ObjectProperties;

namespace CBRE.Editor.Documents {
    /// <summary>
    /// A simple container to separate out the document mediator listeners from the document itself.
    /// </summary>
    public class DocumentSubscriptions : IMediatorListener {
        private readonly Document _document;

        public DocumentSubscriptions(Document document) {
            _document = document;
        }

        public void Subscribe() {
            Mediator.Subscribe(EditorMediator.DocumentTreeStructureChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeObjectsChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeSelectedObjectsChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeFacesChanged, this);
            Mediator.Subscribe(EditorMediator.DocumentTreeSelectedFacesChanged, this);

            Mediator.Subscribe(EditorMediator.SettingsChanged, this);

            Mediator.Subscribe(HotkeysMediator.FileClose, this);
            Mediator.Subscribe(HotkeysMediator.FileSave, this);
            Mediator.Subscribe(HotkeysMediator.FileSaveAs, this);
            //Mediator.Subscribe(HotkeysMediator.FileExport, this);
            Mediator.Subscribe(HotkeysMediator.FileCompile, this);

            Mediator.Subscribe(HotkeysMediator.HistoryUndo, this);
            Mediator.Subscribe(HotkeysMediator.HistoryRedo, this);

            Mediator.Subscribe(HotkeysMediator.OperationsCopy, this);
            Mediator.Subscribe(HotkeysMediator.OperationsCut, this);
            Mediator.Subscribe(HotkeysMediator.OperationsPaste, this);
            Mediator.Subscribe(HotkeysMediator.OperationsPasteSpecial, this);
            Mediator.Subscribe(HotkeysMediator.OperationsDelete, this);
            Mediator.Subscribe(HotkeysMediator.SelectionClear, this);
            Mediator.Subscribe(HotkeysMediator.SelectAll, this);
            Mediator.Subscribe(HotkeysMediator.ObjectProperties, this);

            Mediator.Subscribe(HotkeysMediator.QuickHideSelected, this);
            Mediator.Subscribe(HotkeysMediator.QuickHideUnselected, this);
            Mediator.Subscribe(HotkeysMediator.QuickHideShowAll, this);

            Mediator.Subscribe(HotkeysMediator.SwitchTool, this);
            Mediator.Subscribe(HotkeysMediator.ApplyCurrentTextureToSelection, this);

            Mediator.Subscribe(HotkeysMediator.RotateClockwise, this);
            Mediator.Subscribe(HotkeysMediator.RotateCounterClockwise, this);

            Mediator.Subscribe(HotkeysMediator.Carve, this);
            Mediator.Subscribe(HotkeysMediator.MakeHollow, this);
            Mediator.Subscribe(HotkeysMediator.GroupingGroup, this);
            Mediator.Subscribe(HotkeysMediator.GroupingUngroup, this);
            Mediator.Subscribe(HotkeysMediator.TieToEntity, this);
            Mediator.Subscribe(HotkeysMediator.TieToWorld, this);
            Mediator.Subscribe(HotkeysMediator.Transform, this);
            Mediator.Subscribe(HotkeysMediator.ReplaceTextures, this);
            Mediator.Subscribe(HotkeysMediator.SnapSelectionToGrid, this);
            Mediator.Subscribe(HotkeysMediator.SnapSelectionToGridIndividually, this);
            Mediator.Subscribe(HotkeysMediator.AlignXMax, this);
            Mediator.Subscribe(HotkeysMediator.AlignXMin, this);
            Mediator.Subscribe(HotkeysMediator.AlignYMax, this);
            Mediator.Subscribe(HotkeysMediator.AlignYMin, this);
            Mediator.Subscribe(HotkeysMediator.AlignZMax, this);
            Mediator.Subscribe(HotkeysMediator.AlignZMin, this);
            Mediator.Subscribe(HotkeysMediator.FlipX, this);
            Mediator.Subscribe(HotkeysMediator.FlipY, this);
            Mediator.Subscribe(HotkeysMediator.FlipZ, this);

            Mediator.Subscribe(HotkeysMediator.GridIncrease, this);
            Mediator.Subscribe(HotkeysMediator.GridDecrease, this);
            Mediator.Subscribe(HotkeysMediator.CenterAllViewsOnSelection, this);
            Mediator.Subscribe(HotkeysMediator.Center2DViewsOnSelection, this);
            Mediator.Subscribe(HotkeysMediator.Center3DViewsOnSelection, this);
            Mediator.Subscribe(HotkeysMediator.GoToBrushID, this);
            Mediator.Subscribe(HotkeysMediator.GoToCoordinates, this);

            Mediator.Subscribe(HotkeysMediator.ToggleSnapToGrid, this);
            Mediator.Subscribe(HotkeysMediator.ToggleShow2DGrid, this);
            Mediator.Subscribe(HotkeysMediator.ToggleShow3DGrid, this);
            Mediator.Subscribe(HotkeysMediator.ToggleIgnoreGrouping, this);
            Mediator.Subscribe(HotkeysMediator.ToggleTextureLock, this);
            Mediator.Subscribe(HotkeysMediator.ToggleTextureScalingLock, this);
            //Mediator.Subscribe(HotkeysMediator.ToggleCordon, this);
            Mediator.Subscribe(HotkeysMediator.ToggleHideFaceMask, this);
            Mediator.Subscribe(HotkeysMediator.ToggleHideDisplacementSolids, this);
            Mediator.Subscribe(HotkeysMediator.ToggleHideNullTextures, this);

            Mediator.Subscribe(HotkeysMediator.ShowSelectedBrushID, this);
            Mediator.Subscribe(HotkeysMediator.ShowMapInformation, this);
            Mediator.Subscribe(HotkeysMediator.ShowLogicalTree, this);
            Mediator.Subscribe(HotkeysMediator.ShowEntityReport, this);
            Mediator.Subscribe(HotkeysMediator.CheckForProblems, this);

            Mediator.Subscribe(EditorMediator.ViewportRightClick, this);

            Mediator.Subscribe(EditorMediator.WorldspawnProperties, this);

            Mediator.Subscribe(EditorMediator.VisgroupSelect, this);
            Mediator.Subscribe(EditorMediator.VisgroupShowAll, this);
            Mediator.Subscribe(EditorMediator.VisgroupShowEditor, this);
            Mediator.Subscribe(EditorMediator.VisgroupToggled, this);
            Mediator.Subscribe(HotkeysMediator.VisgroupCreateNew, this);
            Mediator.Subscribe(EditorMediator.SetZoomValue, this);
            Mediator.Subscribe(EditorMediator.TextureSelected, this);
            Mediator.Subscribe(EditorMediator.SelectMatchingTextures, this);

            Mediator.Subscribe(EditorMediator.ViewportCreated, this);
        }

        public void Unsubscribe() {
            Mediator.UnsubscribeAll(this);
        }

        public void Notify(string message, object data) {
            HotkeysMediator val;
            if (ToolManager.ActiveTool != null && Enum.TryParse(message, true, out val)) {
                var result = ToolManager.ActiveTool.InterceptHotkey(val, data);
                if (result == HotkeyInterceptResult.Abort) return;
                if (result == HotkeyInterceptResult.SwitchToSelectTool) {
                    ToolManager.Activate(typeof(SelectTool));
                }
            }
            if (!Mediator.ExecuteDefault(this, message, data)) {
                throw new Exception("Invalid document message: " + message + ", with data: " + data);
            }
        }

        // ReSharper disable UnusedMember.Global
        // ReSharper disable MemberCanBePrivate.Global

        private void DocumentTreeStructureChanged() {
            _document.RenderAll();
        }

        private void DocumentTreeObjectsChanged(IEnumerable<MapObject> objects) {
            _document.RenderObjects(objects);
        }

        private void DocumentTreeSelectedObjectsChanged(IEnumerable<MapObject> objects) {
            _document.RenderSelection(objects);
        }

        private void DocumentTreeFacesChanged(IEnumerable<Face> faces) {
            _document.RenderFaces(faces);
        }

        private void DocumentTreeSelectedFacesChanged(IEnumerable<Face> faces) {
            _document.RenderSelection(faces.Select(x => x.Parent).Distinct());
        }

        public void SettingsChanged() {
            RebuildGrid();
            _document.RenderAll();
        }

        public void HistoryUndo() {
            _document.History.Undo();
        }

        public void HistoryRedo() {
            _document.History.Redo();
        }

        public void FileClose() {
            if (_document.History.TotalActionsSinceLastSave > 0) {
                new SaveMap("", _document, true);
                return;
            }
            DocumentManager.Remove(_document);
        }

        public void FileSave() {
            _document.SaveFile();
        }

        public void FileSaveAs() {
            new SaveMap("", _document, false);
        }

        public void FileCompile() {
            ExportPopup form = new ExportPopup(_document);
            // throw new NotImplementedException();
        }

        public void OperationsCopy() {
            if (!_document.Selection.IsEmpty() && !_document.Selection.InFaceSelection) {
                ClipboardManager.Push(_document.Selection.GetSelectedObjects());
            }
        }

        public void OperationsCut() {
            OperationsCopy();
            OperationsDelete();
        }

        public void OperationsPaste() {
            if (!ClipboardManager.CanPaste()) return;

            var content = ClipboardManager.GetPastedContent(_document);
            if (content == null) return;

            var list = content.ToList();
            if (!list.Any()) return;

            list.SelectMany(x => x.FindAll()).ToList().ForEach(x => x.IsSelected = true);
            _document.Selection.SwitchToObjectSelection();

            var name = "Pasted " + list.Count + " item" + (list.Count == 1 ? "" : "s");
            var selected = _document.Selection.GetSelectedObjects().ToList();
            _document.PerformAction(name, new ActionCollection(
                                              new Deselect(selected), // Deselect the current objects
                                              new Create(_document.Map.WorldSpawn.ID, list))); // Add and select the new objects
        }

        public void OperationsPasteSpecial() {
            if (!ClipboardManager.CanPaste()) return;

            var content = ClipboardManager.GetPastedContent(_document);
            if (content == null) return;

            var list = content.ToList();
            if (!list.Any()) return;

            foreach (var face in list.SelectMany(x => x.FindAll().OfType<Solid>().SelectMany(y => y.Faces))) {
                face.Texture.Texture = _document.GetTexture(face.Texture.Name);
            }

            var box = new Box(list.Select(x => x.BoundingBox));

            throw new NotImplementedException();
            /*using (var psd = new PasteSpecialDialog(box)) {
                if (psd.ShowDialog() == DialogResult.OK) {
                    var name = "Paste special (" + psd.NumberOfCopies + (psd.NumberOfCopies == 1 ? " copy)" : " copies)");
                    var action = new PasteSpecial(list, psd.NumberOfCopies, psd.StartPoint, psd.Grouping,
                                                  psd.AccumulativeOffset, psd.AccumulativeRotation,
                                                  psd.MakeEntitiesUnique, psd.PrefixEntityNames, psd.EntityNamePrefix);
                    _document.PerformAction(name, action);
                }
            }*/
        }

        public void OperationsDelete() {
            if (!_document.Selection.IsEmpty() && !_document.Selection.InFaceSelection) {
                var sel = _document.Selection.GetSelectedParents().Select(x => x.ID).ToList();
                var name = "Removed " + sel.Count + " item" + (sel.Count == 1 ? "" : "s");
                _document.PerformAction(name, new Delete(sel));
            }
        }

        public void SelectionClear() {
            var selected = _document.Selection.GetSelectedObjects().ToList();
            _document.PerformAction("Clear selection", new Deselect(selected));
        }

        public void SelectAll() {
            var all = _document.Map.WorldSpawn.Find(x => !(x is World));
            _document.PerformAction("Select all", new Actions.MapObjects.Selection.Select(all));
        }

        public void ObjectProperties() {
            new ObjectPropertiesUI(_document, _document.Selection.GetSelectedParents().FirstOrDefault());
            // throw new NotImplementedException();
            /*var pd = new ObjectPropertiesDialog(_document);
            pd.Show(Editor.Instance);*/
        }

        public void SwitchTool(HotkeyTool tool) {
            if (ToolManager.ActiveTool != null && ToolManager.ActiveTool.GetHotkeyToolType() == tool) tool = HotkeyTool.Selection;
            ToolManager.Activate(tool);
            GameMain.Instance.SelectedTool = ToolManager.Tools.FirstOrDefault(p => p.GetHotkeyToolType() == tool);
        }

        public void ApplyCurrentTextureToSelection() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;
            var texture = TextureProvider.SelectedTexture;
            if (texture == null) return;
            var ti = texture.Texture;
            if (ti == null) return;
            Action<Document, Face> action = (document, face) => {
                face.Texture.Name = texture.Name;
                face.Texture.Texture = ti;
                face.CalculateTextureCoordinates(true);
            };
            var faces = _document.Selection.GetSelectedObjects().OfType<Solid>().SelectMany(x => x.Faces);
            _document.PerformAction("Apply current texture", new EditFace(faces, action, true));
        }

        public void QuickHideSelected() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var autohide = _document.Map.GetAllVisgroups().FirstOrDefault(x => x.Name == "Autohide");
            if (autohide == null) return;

            var objects = _document.Selection.GetSelectedObjects();
            _document.PerformAction("Hide objects", new QuickHideObjects(objects));
        }

        public void QuickHideUnselected() {
            if (_document.Selection.InFaceSelection) return;

            var autohide = _document.Map.GetAllVisgroups().FirstOrDefault(x => x.Name == "Autohide");
            if (autohide == null) return;

            var objects = _document.Map.WorldSpawn.FindAll().Except(_document.Selection.GetSelectedObjects()).Where(x => !(x is World) && !(x is Group));
            _document.PerformAction("Hide objects", new QuickHideObjects(objects));
        }

        public void QuickHideShowAll() {
            var autohide = _document.Map.GetAllVisgroups().FirstOrDefault(x => x.Name == "Autohide");
            if (autohide == null) return;

            var objects = _document.Map.WorldSpawn.Find(x => x.IsInVisgroup(autohide.ID, true));
            _document.PerformAction("Show hidden objects", new QuickShowObjects(objects));
        }

        public void WorldspawnProperties() {
            throw new NotImplementedException();
            /*var pd = new ObjectPropertiesDialog(_document) { FollowSelection = false, AllowClassChange = false };
            pd.SetObjects(new[] { _document.Map.WorldSpawn });
            pd.Show(Editor.Instance);*/
        }

        public void Carve() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var carver = _document.Selection.GetSelectedObjects().OfType<Solid>().FirstOrDefault();
            if (carver == null) return;

            var carvees = _document.Map.WorldSpawn.Find(x => x is Solid && x.BoundingBox.IntersectsWith(carver.BoundingBox)).OfType<Solid>();

            _document.PerformAction("Carve objects", new Carve(carvees, carver));
        }

        public class MakeHollowData
        {
            public int Width = 32;
        }

        public void MakeHollow() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var solids = _document.Selection.GetSelectedObjects().OfType<Solid>().ToList();
            if (!solids.Any()) return;
            
            MakeHollowData data = new MakeHollowData();

            new MessageConfigPopup<MakeHollowData>("Make Hollow", "Select wall width", data, (p, d) => {
                _document.PerformAction("Make objects hollow", new MakeHollow(solids, d.Width));
            });
        }

        public void GroupingGroup() {
            if (!_document.Selection.IsEmpty() && !_document.Selection.InFaceSelection) {
                _document.PerformAction("Grouped objects", new GroupAction(_document.Selection.GetSelectedParents()));
            }
        }

        public void GroupingUngroup() {
            if (!_document.Selection.IsEmpty() && !_document.Selection.InFaceSelection) {
                _document.PerformAction("Ungrouped objects", new UngroupAction(_document.Selection.GetSelectedParents()));
            }
        }

        private class EntityContainer {
            public Entity Entity { get; set; }
            public override string ToString() {
                var name = Entity.EntityData.Properties.FirstOrDefault(x => x.Key.ToLower() == "targetname");
                if (name != null) return name.Value + " (" + Entity.EntityData.Name + ")";
                return Entity.EntityData.Name;
            }
        }

        public void TieToEntity() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var entities = _document.Selection.GetSelectedObjects().OfType<Entity>().ToList();

            Entity existing = null;

            throw new NotImplementedException();
            /*
            if (entities.Count == 1) {
                var result = new QuickForms.QuickForm("Existing Entity in Selection") { Width = 400 }
                    .Label(String.Format("You have selected an existing entity (a '{0}'), how would you like to proceed?", entities[0].ClassName))
                    .Label(" - Keep the existing entity and add the selected items to the entity")
                    .Label(" - Create a new entity and add the selected items to the new entity")
                    .Item(new QuickFormDialogButtons()
                              .Button("Keep Existing", DialogResult.Yes)
                              .Button("Create New", DialogResult.No)
                              .Button("Cancel", DialogResult.Cancel))
                    .ShowDialog();
                if (result == DialogResult.Yes) {
                    existing = entities[0];
                }
            } else if (entities.Count > 1) {
                var qf = new QuickForms.QuickForm("Multiple Entities Selected") { Width = 400 }
                    .Label("You have selected multiple entities, which one would you like to keep?")
                    .ComboBox("Entity", entities.Select(x => new EntityContainer { Entity = x }))
                    .OkCancel();
                var result = qf.ShowDialog();
                if (result == DialogResult.OK) {
                    var cont = qf.Object("Entity") as EntityContainer;
                    if (cont != null) existing = cont.Entity;
                }
            }

            var ac = new ActionCollection();

            if (existing == null) {
                var def = _document.Game.DefaultBrushEntity;
                var entity = _document.GameData.Classes.FirstOrDefault(x => x.Name.ToLower() == def.ToLower())
                             ?? _document.GameData.Classes.Where(x => x.ClassType == ClassType.Solid)
                                 .OrderBy(x => x.Name.StartsWith("trigger_once") ? 0 : 1)
                                 .FirstOrDefault();
                if (entity == null) {
                    MessageBox.Show("No solid entities found. Please make sure your FGDs are configured correctly.", "No entities found!");
                    return;
                }
                existing = new Entity(_document.Map.IDGenerator.GetNextObjectID()) {
                    EntityData = new EntityData(entity),
                    ClassName = entity.Name,
                    Colour = Colour.GetDefaultEntityColour()
                };
                ac.Add(new Create(_document.Map.WorldSpawn.ID, existing));
            } else {
                // Move the new parent to the root, in case it is a descendant of a selected parent...
                ac.Add(new Reparent(_document.Map.WorldSpawn.ID, new[] { existing }));

                // todo: get rid of all the other entities...
            }

            var reparent = _document.Selection.GetSelectedParents().Where(x => x != existing).ToList();
            ac.Add(new Reparent(existing.ID, reparent));
            ac.Add(new Actions.MapObjects.Selection.Select(existing));

            _document.PerformAction("Tie to Entity", ac);

            if (CBRE.Settings.Select.OpenObjectPropertiesWhenCreatingEntity && !ObjectPropertiesDialog.IsShowing) {
                Mediator.Publish(HotkeysMediator.ObjectProperties);
            }*/
        }

        public void TieToWorld() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var entities = _document.Selection.GetSelectedObjects().OfType<Entity>().ToList();
            var children = entities.SelectMany(x => x.GetChildren()).ToList();

            var ac = new ActionCollection();
            ac.Add(new Reparent(_document.Map.WorldSpawn.ID, children));
            ac.Add(new Delete(entities.Select(x => x.ID)));

            _document.PerformAction("Tie to World", ac);
        }

        private IUnitTransformation GetSnapTransform(Box box) {
            var offset = box.Start.Snap(_document.Map.GridSpacing) - box.Start;
            return new UnitTranslate(offset);
        }

        public class TransformOptions {
            public bool translate = true;
            public bool rotate = false;
            public bool scale = false;
            public float X;
            public float Y;
            public float Z;
        }

        public void Transform() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;
            var box = _document.Selection.GetSelectionBoundingBox();

            // throw new NotImplementedException();


            // using (var td = new TransformDialog(box)) {
                // if (td.ShowDialog() != DialogResult.OK) return;
            new MessageConfigPopup<TransformOptions>("Transform", "Transform selection.", new TransformOptions(), (p, td) => {

                var value = new Vector3((decimal)td.X, (decimal)td.Y, (decimal)td.Z);
                IUnitTransformation transform = null;
                if (td.rotate)
                {
                    var mov = Matrix.Translation(-box.Center); // Move to zero
                    var rot = Matrix.Rotation(Quaternion.EulerAngles(value * DMath.PI / 180)); // Do rotation
                    var fin = Matrix.Translation(box.Center); // Move to final origin
                    transform = new UnitMatrixMult(fin * rot * mov);
                }
                else if (td.translate)
                    transform = new UnitTranslate(value);
                else if (td.scale)
                    transform = new UnitScale(value, box.Center);

                if (transform == null) return;

                var selected = _document.Selection.GetSelectedParents();
                _document.PerformAction("Transform selection", new Edit(selected, new TransformEditOperation(transform, _document.Map.GetTransformFlags())));
            });
        }

        public void RotateClockwise() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;
            var focused = ViewportManager.Viewports.FirstOrDefault(x => x.IsFocused && x is Viewport2D) as Viewport2D;
            if (focused == null) return;
            var center = new Box(_document.Selection.GetSelectedObjects().Select(x => x.BoundingBox).Where(x => x != null)).Center;
            var axis = focused.GetUnusedCoordinate(Vector3.One);
            var transform = new UnitRotate(DMath.DegreesToRadians(90), new Line(center, center + axis));
            var selected = _document.Selection.GetSelectedParents();
            _document.PerformAction("Transform selection", new Edit(selected, new TransformEditOperation(transform, _document.Map.GetTransformFlags())));
        }

        public void RotateCounterClockwise() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;
            var focused = ViewportManager.Viewports.FirstOrDefault(x => x.IsFocused && x is Viewport2D) as Viewport2D;
            if (focused == null) return;
            var center = new Box(_document.Selection.GetSelectedObjects().Select(x => x.BoundingBox).Where(x => x != null)).Center;
            var axis = focused.GetUnusedCoordinate(Vector3.One);
            var transform = new UnitRotate(DMath.DegreesToRadians(-90), new Line(center, center + axis));
            var selected = _document.Selection.GetSelectedParents();
            _document.PerformAction("Transform selection", new Edit(selected, new TransformEditOperation(transform, _document.Map.GetTransformFlags())));
        }

        public void ReplaceTextures() {
            throw new NotImplementedException();
            /*using (var trd = new TextureReplaceDialog(_document)) {
                if (trd.ShowDialog() == DialogResult.OK) {
                    var action = trd.GetAction();
                    _document.PerformAction("Replace textures", action);
                }
            }*/
        }

        public void SnapSelectionToGrid() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var selected = _document.Selection.GetSelectedParents();

            var box = _document.Selection.GetSelectionBoundingBox();
            var transform = GetSnapTransform(box);

            _document.PerformAction("Snap to grid", new Edit(selected, new TransformEditOperation(transform, _document.Map.GetTransformFlags())));
        }

        public void SnapSelectionToGridIndividually() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var selected = _document.Selection.GetSelectedParents();

            _document.PerformAction("Snap to grid individually", new Edit(selected, new SnapToGridEditOperation(_document.Map.GridSpacing, _document.Map.GetTransformFlags())));
        }

        private void AlignObjects(AlignObjectsEditOperation.AlignAxis axis, AlignObjectsEditOperation.AlignDirection direction) {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var selected = _document.Selection.GetSelectedParents();
            var box = _document.Selection.GetSelectionBoundingBox();

            _document.PerformAction("Align Objects", new Edit(selected, new AlignObjectsEditOperation(box, axis, direction, _document.Map.GetTransformFlags())));
        }

        public void AlignXMax() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.X, AlignObjectsEditOperation.AlignDirection.Max);
        }

        public void AlignXMin() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.X, AlignObjectsEditOperation.AlignDirection.Min);
        }

        public void AlignYMax() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.Y, AlignObjectsEditOperation.AlignDirection.Max);
        }

        public void AlignYMin() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.Y, AlignObjectsEditOperation.AlignDirection.Min);
        }

        public void AlignZMax() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.Z, AlignObjectsEditOperation.AlignDirection.Max);
        }

        public void AlignZMin() {
            AlignObjects(AlignObjectsEditOperation.AlignAxis.Z, AlignObjectsEditOperation.AlignDirection.Min);
        }

        private void FlipObjects(Vector3 scale) {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var selected = _document.Selection.GetSelectedParents();
            var box = _document.Selection.GetSelectionBoundingBox();

            var transform = new UnitScale(scale, box.Center);
            _document.PerformAction("Flip Objects", new Edit(selected, new TransformEditOperation(transform, _document.Map.GetTransformFlags())));
        }

        public void FlipX() {
            FlipObjects(new Vector3(-1, 1, 1));
        }

        public void FlipY() {
            FlipObjects(new Vector3(1, -1, 1));
        }

        public void FlipZ() {
            FlipObjects(new Vector3(1, 1, -1));
        }

        public void GridIncrease() {
            var curr = _document.Map.GridSpacing;
            if (curr >= 1024) return;
            _document.Map.GridSpacing *= 2;
            RebuildGrid();
        }

        public void GridDecrease() {
            var curr = _document.Map.GridSpacing;
            if (curr <= 1) return;
            _document.Map.GridSpacing /= 2;
            RebuildGrid();
        }

        public void RebuildGrid() {
            ViewportManager.MarkForRerender();
            // throw new NotImplementedException();
            /*_document.Renderer.UpdateGrid(_document.Map.GridSpacing, _document.Map.Show2DGrid, _document.Map.Show3DGrid, true);
            Mediator.Publish(EditorMediator.DocumentGridSpacingChanged, _document.Map.GridSpacing);*/
        }

        public void CenterAllViewsOnSelection() {
            var box = _document.Selection.GetSelectionBoundingBox()
                      ?? new Box(Vector3.Zero, Vector3.Zero);
            foreach (var vp in ViewportManager.Viewports) {
                vp.FocusOn(box);
            }
        }

        public void Center2DViewsOnSelection() {
            var box = _document.Selection.GetSelectionBoundingBox()
                      ?? new Box(Vector3.Zero, Vector3.Zero);
            foreach (var vp in ViewportManager.Viewports.OfType<Viewport2D>()) {
                vp.FocusOn(box);
            }
        }

        public void Center3DViewsOnSelection() {
            var box = _document.Selection.GetSelectionBoundingBox()
                      ?? new Box(Vector3.Zero, Vector3.Zero);
            foreach (var vp in ViewportManager.Viewports.OfType<Viewport3D>()) {
                vp.FocusOn(box);
            }
        }

        public void GoToCoordinates() {
            throw new NotImplementedException();
            /*using (var qf = new QuickForm("Enter Coordinates") { LabelWidth = 50, UseShortcutKeys = true }
                .TextBox("X", "0")
                .TextBox("Y", "0")
                .TextBox("Z", "0")
                .OkCancel()) {
                qf.ClientSize = new Size(180, qf.ClientSize.Height);
                if (qf.ShowDialog() != DialogResult.OK) return;

                decimal x, y, z;
                if (!Decimal.TryParse(qf.String("X"), out x)) return;
                if (!Decimal.TryParse(qf.String("Y"), out y)) return;
                if (!Decimal.TryParse(qf.String("Z"), out z)) return;

                var coordinate = new Coordinate(x, y, z);

                ViewportManager.Viewports.ForEach(vp => vp.FocusOn(coordinate));
            }*/
        }

        public void GoToBrushID() {
            throw new NotImplementedException();
            /*using (var qf = new QuickForm("Enter Brush ID") { LabelWidth = 100, UseShortcutKeys = true }
                .TextBox("Brush ID")
                .OkCancel()) {
                qf.ClientSize = new Size(230, qf.ClientSize.Height);

                if (qf.ShowDialog() != DialogResult.OK) return;

                long id;
                if (!long.TryParse(qf.String("Brush ID"), out id)) return;

                var obj = _document.Map.WorldSpawn.FindByID(id);
                if (obj == null) return;

                // Select and go to the brush
                _document.PerformAction("Select brush ID " + id, new ChangeSelection(new[] { obj }, _document.Selection.GetSelectedObjects()));
                ViewportManager.Viewports.ForEach(x => x.FocusOn(obj.BoundingBox));
            }*/
        }

        public void ToggleSnapToGrid() {
            _document.Map.SnapToGrid = !_document.Map.SnapToGrid;
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleShow2DGrid() {
            _document.Map.Show2DGrid = !_document.Map.Show2DGrid;
            RebuildGrid();
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleShow3DGrid() {
            _document.Map.Show3DGrid = !_document.Map.Show3DGrid;
            throw new NotImplementedException();
            /*if (_document.Map.Show3DGrid && CBRE.Settings.View.Renderer != RenderMode.OpenGL3) {
                MessageBox.Show("The 3D grid is only available when the OpenGL 3.0 renderer is used.");
                _document.Map.Show3DGrid = false;
            }
            _document.Renderer.UpdateGrid(_document.Map.GridSpacing, _document.Map.Show2DGrid, _document.Map.Show3DGrid, false);
            Mediator.Publish(EditorMediator.UpdateToolstrip);*/
        }

        public void ToggleIgnoreGrouping() {
            _document.Map.IgnoreGrouping = !_document.Map.IgnoreGrouping;
            Mediator.Publish(EditorMediator.IgnoreGroupingChanged);
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleTextureLock() {
            _document.Map.TextureLock = !_document.Map.TextureLock;
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleTextureScalingLock() {
            _document.Map.TextureScalingLock = !_document.Map.TextureScalingLock;
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleCordon() {
            _document.Map.Cordon = !_document.Map.Cordon;
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleHideFaceMask() {
            _document.Map.HideFaceMask = !_document.Map.HideFaceMask;
            throw new NotImplementedException();
            /*_document.Renderer.UpdateDocumentToggles();*/
        }

        public void ToggleHideDisplacementSolids() {
            _document.Map.HideDisplacementSolids = !_document.Map.HideDisplacementSolids;
            // todo hide displacement solids
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ToggleHideNullTextures() {
            _document.Map.HideNullTextures = !_document.Map.HideNullTextures;
            _document.RenderAll();
            Mediator.Publish(EditorMediator.UpdateToolstrip);
        }

        public void ShowSelectedBrushID() {
            if (_document.Selection.IsEmpty() || _document.Selection.InFaceSelection) return;

            var selectedIds = _document.Selection.GetSelectedObjects().Select(x => x.ID);
            var idString = String.Join(", ", selectedIds);

            throw new NotImplementedException();
            /*MessageBox.Show("Selected Object IDs: " + idString);*/
        }

        public void ShowMapInformation() {
            throw new NotImplementedException();
            /*using (var mid = new MapInformationDialog(_document)) {
                mid.ShowDialog();
            }*/
        }

        public void ShowLogicalTree() {
            throw new NotImplementedException();
            /*var mtw = new MapTreeWindow(_document);
            mtw.Show(Editor.Instance);*/
        }

        public void ShowEntityReport() {
            throw new NotImplementedException();
            /*var erd = new EntityReportDialog();
            erd.Show(Editor.Instance);*/
        }

        public void CheckForProblems() {
            throw new NotImplementedException();
            /*using (var cfpd = new CheckForProblemsDialog(_document)) {
                cfpd.ShowDialog(Editor.Instance);
            }*/
        }

        /*public void ViewportRightClick(Viewport2D vp, ViewportEvent e) {
            ViewportContextMenu.Instance.AddNonSelectionItems(_document, vp);
            if (!_document.Selection.IsEmpty() && !_document.Selection.InFaceSelection && ToolManager.ActiveTool is SelectTool) {
                var selectionBoundingBox = _document.Selection.GetSelectionBoundingBox();
                var point = vp.ScreenToWorld(e.X, vp.Height - e.Y);
                var start = vp.Flatten(selectionBoundingBox.Start);
                var end = vp.Flatten(selectionBoundingBox.End);
                if (point.X >= start.X && point.X <= end.X && point.Y >= start.Y && point.Y <= end.Y) {
                    // Clicked inside the selection bounds
                    ViewportContextMenu.Instance.AddSelectionItems(_document, vp);
                }
            }
            if (ToolManager.ActiveTool != null) ToolManager.ActiveTool.OverrideViewportContextMenu(ViewportContextMenu.Instance, vp, e);
            if (ViewportContextMenu.Instance.Items.Count > 0) ViewportContextMenu.Instance.Show(vp, e.X, e.Y);
        }*/

        public void VisgroupSelect(int visgroupId) {
            if (_document.Selection.InFaceSelection) return;
            var objects = _document.Map.WorldSpawn.Find(x => x.IsInVisgroup(visgroupId, true), true).Where(x => !x.IsVisgroupHidden);
            _document.PerformAction("Select visgroup", new ChangeSelection(objects, _document.Selection.GetSelectedObjects()));
        }

        public void VisgroupShowEditor() {
            throw new NotImplementedException();
            /*using (var vef = new VisgroupEditForm(_document)) {
                if (vef.ShowDialog() == DialogResult.OK) {
                    var nv = new List<Visgroup>();
                    var cv = new List<Visgroup>();
                    var dv = new List<Visgroup>();
                    vef.PopulateChangeLists(_document, nv, cv, dv);
                    if (nv.Any() || cv.Any() || dv.Any()) {
                        _document.PerformAction("Edit visgroups", new CreateEditDeleteVisgroups(nv, cv, dv));
                    }
                }
            }*/
        }

        public void VisgroupShowAll() {
            _document.PerformAction("Show all visgroups", new ShowAllVisgroups());
        }

        /*public void VisgroupToggled(int visgroupId, CheckState state) {
            if (state == CheckState.Indeterminate) return;
            var visible = state == CheckState.Checked;
            _document.PerformAction((visible ? "Show" : "Hide") + " visgroup", new ToggleVisgroup(visgroupId, visible));
        }*/

        public void VisgroupCreateNew() {
            throw new NotImplementedException();
            /*using (var qf = new QuickForm("Create New Visgroup") { UseShortcutKeys = true }.TextBox("Name").CheckBox("Add selection to visgroup", true).OkCancel()) {
                if (qf.ShowDialog() != DialogResult.OK) return;

                var ids = _document.Map.Visgroups.Where(x => !x.IsAutomatic).Select(x => x.ID).ToList();
                var id = Math.Max(1, ids.Any() ? ids.Max() + 1 : 1);

                var name = qf.String("Name");
                if (String.IsNullOrWhiteSpace(name)) name = "Visgroup " + id.ToString();

                var vg = new Visgroup {
                    ID = id,
                    Colour = Colour.GetRandomLightColour(),
                    Name = name,
                    Visible = true
                };
                IAction action = new CreateEditDeleteVisgroups(new[] { vg }, new Visgroup[0], new Visgroup[0]);
                if (qf.Bool("Add selection to visgroup") && !_document.Selection.IsEmpty()) {
                    action = new ActionCollection(action, new EditObjectVisgroups(_document.Selection.GetSelectedObjects(), new[] { id }, new int[0]));
                }
                _document.PerformAction("Create visgroup", action);
            }*/
        }

        public void SetZoomValue(decimal value) {
            throw new NotImplementedException();
            /*foreach (var vp in ViewportManager.Viewports.OfType<Viewport2D>()) {
                vp.Zoom = value;
            }
            Mediator.Publish(EditorMediator.ViewZoomChanged, value);*/
        }

        public void TextureSelected(TextureItem selection) {
            TextureProvider.SelectedTexture = selection;
        }

        public void ViewportCreated(ViewportBase viewport) {
            throw new NotImplementedException();
            /*if (viewport is Viewport3D) viewport.RenderContext.Add(new WidgetLinesRenderable());
            _document.Renderer.Register(new[] { viewport });
            viewport.RenderContext.Add(new ToolRenderable());
            viewport.RenderContext.Add(new HelperRenderable(_document));
            _document.Renderer.UpdateGrid(_document.Map.GridSpacing, _document.Map.Show2DGrid, _document.Map.Show3DGrid, false);*/
        }

        public void SelectMatchingTextures(IEnumerable<string> textureList) {
            var list = textureList.ToList();
            var allFaces = _document.Map.WorldSpawn.Find(x => x is Solid && !x.IsCodeHidden && !x.IsVisgroupHidden).OfType<Solid>().SelectMany(x => x.Faces).ToList();
            var matchingFaces = allFaces.Where(x => list.Contains(x.Texture.Name, StringComparer.CurrentCultureIgnoreCase)).ToList();
            var fc = matchingFaces.Count;
            throw new NotImplementedException();
            /*if (_document.Selection.InFaceSelection) {
                _document.PerformAction("Select Faces by Texture", new ChangeFaceSelection(matchingFaces, _document.Selection.GetSelectedFaces()));
                MessageBox.Show(fc + " face" + (fc == 1 ? "" : "s") + " selected.");
            } else {
                var objects = matchingFaces.Select(x => x.Parent).Distinct().ToList();
                _document.PerformAction("Select Objects by Texture", new ChangeSelection(objects, _document.Selection.GetSelectedObjects()));
                var oc = objects.Count;
                MessageBox.Show(fc + " face" + (fc == 1 ? "" : "s") + " found, " + oc + " object" + (oc == 1 ? "" : "s") + " selected.");
            }*/
        }
    }
}

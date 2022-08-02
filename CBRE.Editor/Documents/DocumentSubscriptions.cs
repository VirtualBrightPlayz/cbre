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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Quaternion = CBRE.DataStructures.Geometric.Quaternion;
using CBRE.Editor.Popup;
using CBRE.Editor.Popup.ObjectProperties;
using CBRE.Providers;
using CBRE.RMesh;
using ImGuiNET;
using NativeFileDialog;
using RMeshDecomp;
using Path = CBRE.DataStructures.MapObjects.Path;
using CBRE.Graphics;

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
            Mediator.Subscribe(EditorMediator.DocumentTreeStructureChanged, this, priority: -1);
            Mediator.Subscribe(EditorMediator.DocumentTreeObjectsChanged, this, priority: -1);
            Mediator.Subscribe(EditorMediator.DocumentTreeSelectedObjectsChanged, this, priority: -1);
            Mediator.Subscribe(EditorMediator.DocumentTreeFacesChanged, this, priority: -1);
            Mediator.Subscribe(EditorMediator.DocumentTreeSelectedFacesChanged, this, priority: -1);

            Mediator.Subscribe(EditorMediator.SettingsChanged, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.FileClose, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FileSave, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FileSaveAs, this, priority: -1);
            //Mediator.Subscribe(HotkeysMediator.FileExport, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FileCompile, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.HistoryUndo, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.HistoryRedo, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.OperationsCopy, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.OperationsCut, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.OperationsPaste, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.OperationsPasteSpecial, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.OperationsDelete, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.SelectionClear, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.SelectAll, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ObjectProperties, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.QuickHideSelected, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.QuickHideUnselected, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.QuickHideShowAll, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.SwitchTool, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ApplyCurrentTextureToSelection, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.RotateClockwise, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.RotateCounterClockwise, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.Carve, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.MakeHollow, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.GroupingGroup, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.GroupingUngroup, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.TieToEntity, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.TieToWorld, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.Transform, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ReplaceTextures, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.SnapSelectionToGrid, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.SnapSelectionToGridIndividually, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignXMax, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignXMin, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignYMax, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignYMin, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignZMax, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.AlignZMin, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FlipX, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FlipY, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.FlipZ, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.GridIncrease, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.GridDecrease, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.CenterAllViewsOnSelection, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.Center2DViewsOnSelection, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.Center3DViewsOnSelection, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.GoToBrushID, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.GoToCoordinates, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.ToggleSnapToGrid, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleShow2DGrid, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleShow3DGrid, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleIgnoreGrouping, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleTextureLock, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleTextureScalingLock, this, priority: -1);
            //Mediator.Subscribe(HotkeysMediator.ToggleCordon, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleHideFaceMask, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleHideDisplacementSolids, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ToggleHideNullTextures, this, priority: -1);

            Mediator.Subscribe(HotkeysMediator.ShowSelectedBrushID, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ShowMapInformation, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ShowLogicalTree, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.ShowEntityReport, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.CheckForProblems, this, priority: -1);

            Mediator.Subscribe(EditorMediator.ViewportRightClick, this, priority: -1);

            Mediator.Subscribe(EditorMediator.WorldspawnProperties, this, priority: -1);

            Mediator.Subscribe(EditorMediator.VisgroupSelect, this, priority: -1);
            Mediator.Subscribe(EditorMediator.VisgroupShowAll, this, priority: -1);
            Mediator.Subscribe(EditorMediator.VisgroupShowEditor, this, priority: -1);
            Mediator.Subscribe(EditorMediator.VisgroupToggled, this, priority: -1);
            Mediator.Subscribe(HotkeysMediator.VisgroupCreateNew, this, priority: -1);
            Mediator.Subscribe(EditorMediator.SetZoomValue, this, priority: -1);
            Mediator.Subscribe(EditorMediator.TextureSelected, this, priority: -1);
            Mediator.Subscribe(EditorMediator.SelectMatchingTextures, this, priority: -1);

            Mediator.Subscribe(EditorMediator.ViewportCreated, this, priority: -1);
        }

        public void Unsubscribe() {
            Mediator.UnsubscribeAll(this);
        }

        public void Notify(Enum message, object data) {
            if (ToolManager.ActiveTool != null && message is HotkeysMediator val) {
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
            var selectedObjects = _document.Selection.GetSelectedObjects().ToArray();
            var types = selectedObjects.Select(o => o.GetType()).Distinct().ToArray();
            foreach (var t in types) {
                Debug.WriteLine($"{t}: {selectedObjects.Count(o => o.GetType() == t)}");
            }
            _document.RenderObjects(objects);
        }

        private void DocumentTreeFacesChanged(IEnumerable<Face> faces) {
            _document.RenderFaces(faces);
        }

        private void DocumentTreeSelectedFacesChanged(IEnumerable<Face> faces) {
            _document.RenderObjects(faces.Select(x => x.Parent).Distinct());
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
                GameMain.Instance.Popups.Add(new ConfirmPopup($"Closing {_document.MapFileName}",
                    $"{_document.MapFileName} has unsaved changes.\nWould you like to save before closing?") {
                    Buttons = new [] {
                        new ConfirmPopup.Button("Yes", () => {
                            var result = NativeFileDialog.SaveDialog.Open("vmf", _document.MapFileName, out string outPath);
                            if (result == Result.Okay) {
                                try {
                                    _document.SaveFile(outPath);
                                    DocumentManager.Remove(_document);
                                } catch (ProviderNotFoundException e) {
                                    GameMain.Instance.Popups.Add(
                                        new MessagePopup("Error", e.Message, new ImColor() { Value = new System.Numerics.Vector4(1f, 0f, 0f, 1f) }, true));
                                }
                            }
                        }),
                        new ConfirmPopup.Button("No", () => {
                            DocumentManager.Remove(_document);
                        }),
                        new ConfirmPopup.Button("Cancel", () => { }),
                    }.ToImmutableArray()
                });
            } else {
                DocumentManager.Remove(_document);
            }
        }

        public void FileSave() {
            var visibleMeshes = new List<RMesh.RMesh.VisibleMesh>();
            var invisibleCollisionMeshes = new List<RMesh.RMesh.InvisibleCollisionMesh>();

            var vertices = new List<RMesh.RMesh.VisibleMesh.Vertex>();
            var triangles = new List<RMesh.RMesh.Triangle>();
            int indexOffset = 0;
            foreach (var solid in _document.Map.WorldSpawn.GetSelfAndAllChildren().OfType<Solid>()) {
                foreach (var face in solid.Faces) {
                    vertices.AddRange(face.Vertices.Select(fv => new RMesh.RMesh.VisibleMesh.Vertex(
                        new Vector3F(fv.Location),
                        new Vector2F((float)fv.TextureU, (float)fv.TextureV),
                        Vector2F.Zero, Color.White)));
                    triangles.AddRange(face.GetTriangleIndices().Chunk(3).Select(c => new RMesh.RMesh.Triangle(
                        (ushort)(c[0] + indexOffset), (ushort)(c[1] + indexOffset), (ushort)(c[2] + indexOffset))));
                    indexOffset += face.Vertices.Count;
                }
            }
            
            var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), "", "", RMesh.RMesh.VisibleMesh.BlendMode.Opaque);
            visibleMeshes.Add(mesh);
            
            RMesh.RMesh rmesh = new RMesh.RMesh(
                visibleMeshes.ToImmutableArray(),
                invisibleCollisionMeshes.ToImmutableArray(),
                null, null);

            #if FALSE
            RMesh.RMesh.Saver.ToFile(rmesh, DocumentManager.CurrentDocument.MapFile+".rmesh");
            var result = NativeFileDialog.OpenDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string outPath);
            if (result == Result.Okay) {
                rmesh = RMesh.RMesh.Loader.FromFile(outPath);

                var idGenerator = _document.Map.IDGenerator;

                var rng = new Random();
                foreach (var subMesh in rmesh.VisibleMeshes) {
                    if (subMesh.TextureBlendMode != RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped) { continue; }
                    
                    var newFaces = new HashSet<Face>();
                    ExtractFaces.Invoke(subMesh, newFaces);

                    if (!newFaces.Any()) { continue; }
                    
                    var newSolid = new Solid(idGenerator.GetNextObjectID());
                    newSolid.Colour = Color.Chartreuse;
                    //newSolid.Faces.AddRange(newFaces);
                    foreach (var newFace in newFaces) {
                        newSolid.Faces.Add(newFace);
                        newFace.Parent = newSolid;
                        newFace.Texture = new TextureReference();
                        string tex = subMesh.DiffuseTexture.Replace(".jpg", "").Replace(".jpeg", "").Replace(".png", "");
                        TextureItem item = TextureProvider.GetItem(tex);
                        newFace.Texture.Name = item?.Name;
                        newFace.Texture.Texture = item?.Texture as AsyncTexture;
                        newFace.Colour = Color.FromArgb(255,
                            rng.Next()%256,
                            newFace.IsConvex(0.001m) && !newFace.HasColinearEdges(0.001m) ? 255 : 0,
                            newFace.IsConvex(0.001m) && !newFace.HasColinearEdges(0.001m) ? 0 : 255);
                        // newFace.AlignTextureToWorld();
                        // newFace.CalculateTextureCoordinates(false);
                    }

                    if (newSolid.Faces.Any()) {
                        newSolid.SetParent(_document.Map.WorldSpawn);
                        _document.ObjectRenderer.AddMapObject(newSolid);
                    }
                }
            }
            
            _document.ObjectRenderer.MarkDirty();
            
            return;
            #endif
            _document.SaveFile();
        }

        public void FileSaveAs() {
            var currFilePath = System.IO.Path.GetDirectoryName(DocumentManager.CurrentDocument.MapFile);
            if (string.IsNullOrEmpty(currFilePath)) { currFilePath = Directory.GetCurrentDirectory(); }

            var result = NativeFileDialog.SaveDialog.Open("vmf", currFilePath, out string outPath);
            if (result == Result.Okay) {
                try {
                    _document.SaveFile(outPath);
                }
                catch (ProviderNotFoundException e) {
                    new MessagePopup("Error", e.Message, new ImColor() { Value = new System.Numerics.Vector4(1f, 0f, 0f, 1f) }, true);
                }
            }
        }

        public void FileCompile() {
            ExportPopup form = new ExportPopup(_document);
            GameMain.Instance.Popups.Add(form);
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
            if (!ClipboardManager.CanPaste()) { return; }

            var content = ClipboardManager.GetPastedContent(_document);
            if (content == null) { return; }

            var list = content.ToList();
            if (!list.Any()) { return; }

            list.SelectMany(x => x.GetSelfAndAllChildren()).ToList().ForEach(x => x.IsSelected = true);
            _document.Selection.SwitchToObjectSelection();

            var name = "Pasted " + list.Count + " item" + (list.Count == 1 ? "" : "s");
            var selected = _document.Selection.GetSelectedObjects().ToList();
            _document.PerformAction(name, new ActionCollection(
                                              new Deselect(selected), // Deselect the current objects
                                              new Create(_document.Map.WorldSpawn.ID, list))); // Add and select the new objects
            
            var selectedObjects = _document.Selection.GetSelectedObjects().ToArray();
            var types = selectedObjects.Select(o => o.GetType()).Distinct().ToArray();
            foreach (var t in types) {
                Debug.WriteLine($"{t}: {selectedObjects.Count(o => o.GetType() == t)}");
            }
        }

        public void OperationsPasteSpecial() {
            if (!ClipboardManager.CanPaste()) return;

            var content = ClipboardManager.GetPastedContent(_document);
            if (content == null) return;

            var list = content.ToList();
            if (!list.Any()) return;

            foreach (var face in list.SelectMany(x => x.GetSelfAndAllChildren().OfType<Solid>().SelectMany(y => y.Faces))) {
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
                var deleted = _document.Selection.GetSelectedObjects().Select(x => x.ID).ToList();
                var name = "Removed " + deleted.Count + " item" + (deleted.Count == 1 ? "" : "s");
                _document.PerformAction(name, new Delete(deleted));
            }
        }

        public void SelectionClear() {
            var selected = _document.Selection.GetSelectedObjects().ToList();
            _document.PerformAction("Clear selection", new Deselect(selected));
        }

        public void SelectAll() {
            var all = _document.Map.WorldSpawn.Find(x => x is not World && !x.IsSelected);
            _document.PerformAction("Select all", new Actions.MapObjects.Selection.Select(all));
        }

        public void ObjectProperties() {
            var mapObject = _document.Selection.GetSelectedParents().FirstOrDefault();
            if (mapObject is null) { return; }
            GameMain.Instance.Popups.Add(new ObjectPropertiesUI(_document, mapObject));
        }

        public void SwitchTool(HotkeyTool tool) {
            if (ToolManager.ActiveTool != null && ToolManager.ActiveTool.GetHotkeyToolType() == tool) { tool = HotkeyTool.Selection; }
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

            var objects = _document.Map.WorldSpawn.GetSelfAndAllChildren().Except(_document.Selection.GetSelectedObjects()).Where(x => !(x is World) && !(x is Group));
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

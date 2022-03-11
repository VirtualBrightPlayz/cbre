using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.DataStructures.GameData;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Entities;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.Visgroups;
using CBRE.Editor.Documents;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup.ObjectProperties {
    public class ObjectPropertiesUI : PopupUI {
        public enum VisgroupAction {
            NoChange,
            Add,
            Remove
        }

        private Document _document;
        private MapObject _obj;
        private List<TableValue> _propVals;
        private string _className;
        private GameDataObject selectedEntity = null;
        private Dictionary<int, VisgroupAction> visgroupActions = new Dictionary<int, VisgroupAction>();
        private Dictionary<int, Color> visgroupColors = new Dictionary<int, Color>();

        protected override bool hasOkButton => false;

        public ObjectPropertiesUI(Document document, MapObject mapobject) : base("Object Properties") {
            _document = document;
            _obj = mapobject;
            Setup();
        }

        protected virtual void Setup() {
            _className = _obj.ClassName;
            visgroupActions = new Dictionary<int, VisgroupAction>();
            foreach (var group in _document.Map.Visgroups) {
                visgroupActions.Add(group.ID, VisgroupAction.NoChange);
            }

            var list = new List<TableValue>();
            if (_obj is Entity || _obj is World) {
                var data = _obj.GetEntityData();
                foreach (var prop in data.Properties) {
                    list.Add(new TableValue(prop));
                }
                var cls = _document.GameData.Classes.FirstOrDefault(p => p.Name == data.Name);
                if (cls != null) {
                    foreach (var gprop in cls.Properties) {
                        var prop = list.FirstOrDefault(p => p.OriginalKey == gprop.Name);
                        if (prop == null) {
                            list.Add(new TableValue() {
                                Class = gprop.VariableType,
                                IsAdded = true,
                                NewKey = gprop.Name,
                                OriginalKey = gprop.Name,
                                Value = gprop.DefaultValue
                            });
                        }
                        else {
                            prop.Class = gprop.VariableType;
                        }
                    }
                    foreach (var prop in list) {
                        if (!cls.Properties.Any(p => p.Name == prop.OriginalKey)) {
                            prop.IsRemoved = true;
                        }
                    }
                }
            }
            _propVals = list;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            if (_obj is Entity || _obj is World) {
                EntityGui(_obj);
                VisgroupGui(_obj);
            }
            shouldBeOpen = ImGuiButtons();
        }

        protected virtual void VisgroupGui(MapObject obj) {
            if (ImGui.TreeNode("Visgroups")) {
                for (int i = 0; i < _document.Map.Visgroups.Count; i++) {
                    if (_document.Map.Visgroups[i] is DataStructures.MapObjects.AutoVisgroup) {
                        continue;
                    }
                    var c = _document.Map.Visgroups[i].Colour;
                    using (new ColorPush(ImGuiCol.Text, c)) {
                        int id = _document.Map.Visgroups[i].ID;
                        bool isInVis = obj.IsInVisgroup(id, false);
                        if (visgroupActions[id] != VisgroupAction.NoChange) {
                            isInVis = visgroupActions[id] == VisgroupAction.Add;
                        }
                        if (ImGui.Checkbox($"{_document.Map.Visgroups[i].Name}", ref isInVis)) {
                            visgroupActions[id] = isInVis ? VisgroupAction.Add : VisgroupAction.Remove;
                        }
                    }
                    /*ImGui.SameLine();
                    if (ImGui.ColorEdit4($"{_document.Map.Visgroups[i].Name}", ref col)) {
                        if (!visgroupColors.ContainsKey(id))
                            visgroupColors.Add(id, c);
                        visgroupColors[id] = Color.FromArgb((int)(col.W * 255), (int)(col.X * 255), (int)(col.Y * 255), (int)(col.Z * 255));
                    }*/
                }
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("Auto Visgroups")) {
                for (int i = 0; i < _document.Map.Visgroups.Count; i++) {
                    if (_document.Map.Visgroups[i] is DataStructures.MapObjects.AutoVisgroup) {
                        var c = _document.Map.Visgroups[i].Colour;
                        using (new ColorPush(ImGuiCol.Text, c)) {
                            int id = _document.Map.Visgroups[i].ID;
                            bool isInVis = obj.IsInVisgroup(id, false);
                            if (ImGui.Checkbox($"{_document.Map.Visgroups[i].Name}", ref isInVis)) {
                                visgroupActions[id] = isInVis ? VisgroupAction.Add : VisgroupAction.Remove;
                            }
                        }
                        /*ImGui.SameLine();
                        if (ImGui.ColorEdit4($"{_document.Map.Visgroups[i].Name}", ref col)) {
                            if (!visgroupColors.ContainsKey(id))
                                visgroupColors.Add(id, c);
                            visgroupColors[id] = Color.FromArgb((int)(col.W * 255), (int)(col.X * 255), (int)(col.Y * 255), (int)(col.Z * 255));
                        }*/
                    }
                }
                ImGui.TreePop();
            }
        }

        protected virtual void EntityGui(MapObject obj) {
            if (ImGui.BeginCombo("Entity type", selectedEntity?.Name ?? "<No Change>")) {
                var entityTypes = _document.GameData.Classes.Where(c => c.ClassType == ClassType.Point);
                foreach (var entityType in entityTypes) {
                    if (ImGui.Selectable(entityType.Name)) {
                        selectedEntity = entityType;
                    }
                }
                ImGui.EndCombo();
            }
            
            var data = obj.GetEntityData();
            ImGui.Text($"{data.Name}");

            if (ImGui.BeginChild($"{data.Name}Properties",
                new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 12))) {
                ImGui.Columns(3, $"{data.Name}properties", true);
                ImGui.Separator();
                ImGui.Text("Key");
                ImGui.NextColumn();
                ImGui.Text("Value");
                ImGui.NextColumn();
                ImGui.Text("State");
                ImGui.NextColumn();
                ImGui.Separator();

                var props = _propVals;

                for (int i = 0; i < props.Count; i++) {
                    ImGui.Text(props[i].NewKey);
                    ImGui.NextColumn();
                    string tmp = props[i].Value;
                    switch (props[i].Class) {
                        case VariableType.Float: {
                            float fl = 0f;
                            float.TryParse(tmp, out fl);
                            if (ImGui.InputFloat(props[i].OriginalKey, ref fl)) {
                                props[i].Value = fl.ToString(CultureInfo.InvariantCulture);
                                props[i].IsModified = true;
                            }
                        }
                            break;
                        case VariableType.Integer: {
                            int fl = 0;
                            int.TryParse(tmp, out fl);
                            if (ImGui.InputInt(props[i].OriginalKey, ref fl)) {
                                props[i].Value = fl.ToString();
                                props[i].IsModified = true;
                            }
                        }
                            break;
                        case VariableType.Color255: {
                            Color color = props[i].GetColour255(Color.White);
                            Num.Vector4 v4 = new Num.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
                            if (ImGui.ColorEdit4(props[i].OriginalKey, ref v4)) {
                                props[i].Value = $"{(int)(v4.X * 255)} {(int)(v4.Y * 255)} {(int)(v4.Z * 255)}";
                                props[i].IsModified = true;
                            }
                        }
                            break;
                        case VariableType.Vector: {
                            Num.Vector3 v3 = props[i].GetVector3(Num.Vector3.Zero);
                            if (ImGui.InputFloat3(props[i].OriginalKey, ref v3)) {
                                props[i].Value = $"{v3.X} {v3.Y} {v3.Z}";
                                props[i].IsModified = true;
                            }
                        }
                            break;
                        case VariableType.Bool: {
                            bool b = default(bool);
                            bool.TryParse(props[i].Value, out b);
                            if (ImGui.Checkbox(props[i].OriginalKey, ref b)) {
                                props[i].Value = b.ToString();
                                props[i].IsModified = true;
                            }
                        }
                            break;
                        default: {
                            if (ImGui.InputText(props[i].OriginalKey, ref tmp, 1024)) {
                                props[i].Value = tmp;
                                props[i].IsModified = true;
                            }
                        }
                            break;
                    }

                    ImGui.NextColumn();
                    var col = props[i].GetStateColour();
                    ImGui.TextColored(new Num.Vector4(col.R / 255f, col.G / 255f, col.B / 255f, col.A / 255f),
                        props[i].GetState());
                    ImGui.NextColumn();
                    /*if (ImGui.Button("-")) {
                        props[i].IsRemoved = true;
                    }
                    ImGui.NextColumn();*/
                }

                ImGui.Columns(1);
                ImGui.Separator();
            }
            ImGui.EndChild();

            ImGui.NewLine();
            /*if (ImGui.Button("+")) {
                _propVals.Add(new TableValue() {
                    IsAdded = true,
                    OriginalKey = "New Key",
                    NewKey = "New Key",
                    Value = "New Value",
                });
            }*/
        }

        protected virtual bool ImGuiButtons() {
            if (ImGui.Button("Cancel")) {
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply")) {
                return !EntityApply();
            }
            return true;
        }

        protected virtual bool EntityApply() {
            string actionText = null;
            var ac = new ActionCollection();
            var editAction = GetEditEntityDataAction();
            if (editAction != null)
            {
                // The entity change is more important to show
                actionText = "Edit entity data";
                ac.Add(editAction);
            }

            var visgroupAction = GetUpdateVisgroupsAction();

            if (visgroupAction != null)
            {
                actionText = "Update visgroups";
                ac.Add(visgroupAction);
            }
            
            if (!ac.IsEmpty())
            {
                // Run if either action shows changes
                _document.PerformAction(actionText, ac);
                return true;
            }
            return false;
        }

        private IAction GetUpdateVisgroupsAction()
        {
            var add = visgroupActions.Where(p => p.Value == VisgroupAction.Add).Select(p => p.Key);
            var rem = visgroupActions.Where(p => p.Value == VisgroupAction.Remove).Select(p => p.Key);
            return new EditObjectVisgroups(new [] { _obj }, add, rem);
        }

        private EditEntityData GetEditEntityDataAction()
        {
            // var ents = _obj.Where(x => x is Entity || x is World).ToList();
            var ents = new MapObject[] { _obj };
            if (!ents.Any()) return null;
            var action = new EditEntityData();

            foreach (var entity in ents)
            {
                var entityData = entity.GetEntityData().Clone();
                var changed = false;
                var _values = _propVals;

                // Remove nonexistant properties
                var nonExistant = entityData.Properties.Where(x => _values.All(y => y.OriginalKey != x.Key));
                if (nonExistant.Any())
                {
                    changed = true;
                    entityData.Properties.RemoveAll(x => _values.All(y => y.OriginalKey != x.Key));
                }

                // Set updated/new properties
                foreach (var ent in _values.Where(x => x.IsModified || (x.IsAdded && !x.IsRemoved)))
                {
                    entityData.SetPropertyValue(ent.OriginalKey, ent.Value);
                    if (!string.IsNullOrWhiteSpace(ent.NewKey) && ent.NewKey != ent.OriginalKey)
                    {
                        var prop = entityData.Properties.FirstOrDefault(x => string.Equals(x.Key, ent.OriginalKey, StringComparison.OrdinalIgnoreCase));
                        if (prop != null && !entityData.Properties.Any(x => string.Equals(x.Key, ent.NewKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            prop.Key = ent.NewKey;
                        }
                    }
                    changed = true;
                }

                foreach (var ent in _values.Where(x => x.IsRemoved && !x.IsAdded))
                {
                    entityData.Properties.RemoveAll(x => x.Key == ent.OriginalKey);
                    changed = true;
                }

                // Set flags
                /*var flags = Enumerable.Range(0, FlagsTable.Items.Count).Select(x => FlagsTable.GetItemCheckState(x)).ToList();
                var entClass = _document.GameData.Classes.FirstOrDefault(x => x.Name == entityData.Name);
                var spawnFlags = entClass == null
                                     ? null
                                     : entClass.Properties.FirstOrDefault(x => x.Name == "spawnflags");
                var opts = spawnFlags == null ? null : spawnFlags.Options.OrderBy(x => int.Parse(x.Key)).ToList();
                if (opts != null && flags.Count == opts.Count)
                {
                    var beforeFlags = entityData.Flags;
                    for (var i = 0; i < flags.Count; i++)
                    {
                        var val = int.Parse(opts[i].Key);
                        if (flags[i] == CheckState.Unchecked) entityData.Flags &= ~val; // Switch the flag off if unchecked
                        else if (flags[i] == CheckState.Checked) entityData.Flags |= val; // Switch it on if checked
                        // No change if indeterminate
                    }
                    if (entityData.Flags != beforeFlags) changed = true;
                }*/

                // Updated class
                Console.WriteLine($"changed to {entityData.Name}");
                if (selectedEntity != null) {
                    entityData.Name = selectedEntity.Name;
                    // entityData = new EntityData(selectedEntity);
                    changed = true;
                }

                if (changed) action.AddEntity(entity, entityData);
            }

            return action.IsEmpty() ? null : action;
        }
    }
}

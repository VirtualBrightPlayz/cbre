using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Entities;
using CBRE.Editor.Documents;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup.ObjectProperties {
    public class ObjectPropertiesUI : PopupUI {
        private Document _document;
        private MapObject _obj;
        private List<TableValue> _propVals;
        private string _className;

        public ObjectPropertiesUI(Document document, MapObject mapobject) : base("Object Properties") {
            _document = document;
            _obj = mapobject;
            GameMain.Instance.PopupSelected = true;
            Setup();
        }

        protected virtual void Setup() {
            _className = _obj.ClassName;
            var list = new List<TableValue>();
            if (_obj is Entity || _obj is World) {
                var data = _obj.GetEntityData();
                foreach (var prop in data.Properties) {
                    list.Add(new TableValue(prop));
                }
            }
            _propVals = list;
        }

        protected override bool ImGuiLayout() {
            /*string tmpclass = _className;
            if (ImGui.InputText("Class", ref tmpclass, 1024)) {
                _className = tmpclass;
            }*/
            ImGui.Text($"Class {_obj.ClassName}");
            if (_obj is Entity || _obj is World)
                EntityGui(_obj);
            return ImGuiButtons();
        }

        protected virtual void EntityGui(MapObject obj) {
            var data = obj.GetEntityData();
            ImGui.Text($"{data.Name}");

            ImGui.BeginChild($"{data.Name}Properties", new Num.Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 12));
            ImGui.Columns(2, $"{data.Name}properties", true);
            ImGui.Separator();
            ImGui.Text("Key");
            ImGui.NextColumn();
            ImGui.Text("Value");
            ImGui.NextColumn();
            // ImGui.Text("Remove");
            // ImGui.NextColumn();
            ImGui.Separator();

            var props = _propVals;

            for (int i = 0; i < props.Count; i++) {
                if (props[i].IsRemoved)
                    continue;
                /*string tmp = props[i].NewKey;
                if (ImGui.InputText(props[i].OriginalKey, ref tmp, 1024)) {
                    props[i].OriginalKey = tmp;
                }*/
                ImGui.Text(props[i].NewKey);
                ImGui.NextColumn();
                string tmp = props[i].Value;
                if (ImGui.InputText(props[i].OriginalKey, ref tmp, 1024)) {
                    props[i].Value = tmp;
                    props[i].IsModified = true;
                }
                ImGui.NextColumn();
                /*if (ImGui.Button("-")) {
                    props[i].IsRemoved = true;
                }
                ImGui.NextColumn();*/
            }

            ImGui.Columns(1);
            ImGui.Separator();
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
                EntityApply();
                return false;
            }
            return true;
        }

        protected virtual void EntityApply() {
            string actionText = null;
            var ac = new ActionCollection();
            var editAction = GetEditEntityDataAction();
            if (editAction != null)
            {
                // The entity change is more important to show
                actionText = "Edit entity data";
                ac.Add(editAction);
            }
            
            if (!ac.IsEmpty())
            {
                // Run if either action shows changes
                _document.PerformAction(actionText, ac);
            }
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
                // Updated class
                /*if (_className != entity.ClassName)
                {
                    entity.ClassName = _className;
                    changed = true;
                }*/

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

                if (changed) action.AddEntity(entity, entityData);
            }

            return action.IsEmpty() ? null : action;
        }
    }
}
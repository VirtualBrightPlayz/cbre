using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CBRE.Common;

namespace CBRE.DataStructures.GameData {
    public class GameDataObject {
        public string Name { get; set; }
        public string Description { get; set; }
        public ClassType ClassType { get; set; }
        public List<string> BaseClasses { get; private set; }
        public List<Behaviour> Behaviours { get; private set; }
        public List<Property> Properties { get; private set; }
        public List<IO> InOuts { get; private set; }

        public class RMeshLayout {
            public enum WriteType {
                String,
                B3DString,
                Integer,
                Float,
                Vector,
                Bool
            }

            public struct Entry {
                public string Property;
                public WriteType As;
            }
            private List<Entry> entries;
            public IReadOnlyList<Entry> Entries => entries;

            public struct Condition {
                public string Property;
                public string Equal;
            }
            private List<Condition> conditions;
            public IReadOnlyList<Condition> Conditions => conditions;

            public RMeshLayout(XElement elem) {
                entries = new List<Entry>();
                conditions = new List<Condition>();
                foreach (var subElement in elem.Elements()) {
                    switch (subElement.Name.LocalName.ToLowerInvariant()) {
                        case "write":
                            WriteType wt = (WriteType)Enum.Parse(typeof(WriteType), subElement.GetAttributeString("as", null), ignoreCase: true);
                            entries.Add(new Entry {
                                Property = subElement.GetAttributeString("property", null),
                                As = wt
                            });
                            break;
                        case "condition":
                            conditions.Add(new Condition {
                                Property = subElement.GetAttributeString("property", null),
                                Equal = subElement.GetAttributeString("equals", null)
                            });
                            break;
                    }
                }
            }
        }

        public readonly RMeshLayout RMeshDef = null;

        private void ParseProperties(XElement elem) {
            foreach (var subElement in elem.Elements()) {
                VariableType type;
                string typeStr = subElement.GetAttributeString("type", null);
                if (typeStr.Equals("position", StringComparison.OrdinalIgnoreCase)) {
                    type = VariableType.Vector;
                } else {
                    type = (VariableType)Enum.Parse(typeof(VariableType), typeStr, ignoreCase: true);
                }
                Properties.Add(new Property(subElement.GetAttributeString("name", null), type) {
                    DefaultValue = subElement.GetAttributeString("default", "")
                });
            }
        }

        public GameDataObject(string name, string description, ClassType classType) {
            Name = name;
            Description = description;
            ClassType = classType;
            BaseClasses = new List<string>();
            Behaviours = new List<Behaviour>();
            Properties = new List<Property>();
            InOuts = new List<IO>();
        }

        public GameDataObject(XElement elem, ClassType classType) {
            Name = elem.GetAttributeString("name", null);
            Description = "";
            ClassType = classType;
            BaseClasses = new List<string>();
            Behaviours = new List<Behaviour>();
            Properties = new List<Property>();
            InOuts = new List<IO>();

            foreach (var subElement in elem.Elements()) {
                switch (subElement.Name.LocalName.ToLowerInvariant()) {
                    case "properties":
                        ParseProperties(subElement);
                        break;
                    case "rmesh":
                        RMeshDef = new RMeshLayout(subElement);
                        break;
                    case "sprite":
                        Behaviours.Add(new Behaviour("sprite", subElement.GetAttributeString("name", null)));
                        break;
                    case "light":

                        break;
                }
            }
        }

        public void Inherit(IEnumerable<GameDataObject> parents) {
            foreach (var gdo in parents) {
                MergeBehaviours(gdo.Behaviours);
                MergeProperties(gdo.Properties);
                MergeInOuts(gdo.InOuts);
            }
        }

        private void MergeInOuts(IEnumerable<IO> inOuts) {
            var inc = 0;
            foreach (var io in inOuts) {
                var existing = InOuts.FirstOrDefault(x => x.IOType == io.IOType && x.Name == io.Name);
                if (existing == null) InOuts.Insert(inc++, io);
            }
        }

        private void MergeProperties(IEnumerable<Property> properties) {
            var inc = 0;
            foreach (var p in properties) {
                var existing = Properties.FirstOrDefault(x => x.Name == p.Name);
                if (existing != null) existing.Options.AddRange(p.Options.Where(x => !existing.Options.Contains(x)));
                else Properties.Insert(inc++, p);
            }
        }

        private void MergeBehaviours(IEnumerable<Behaviour> behaviours) {
            var inc = 0;
            foreach (var b in behaviours) {
                var existing = Behaviours.FirstOrDefault(x => x.Name == b.Name);
                if (existing != null) existing.Values.AddRange(b.Values.Where(x => !existing.Values.Contains(x)));
                else Behaviours.Insert(inc++, b);
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CBRE.DataStructures.GameData {
    public class GameData {
        public int MapSizeLow { get; set; }
        public int MapSizeHigh { get; set; }
        public List<GameDataObject> Classes { get; private set; }
        public List<string> Includes { get; private set; }
        public List<string> MaterialExclusions { get; private set; }
        public List<AutoVisgroupSection> AutoVisgroups { get; private set; }

        public GameData() {
            MapSizeHigh = 16384;
            MapSizeLow = -16384;
            Classes = new List<GameDataObject>();

            IEnumerable<string> entityFiles = Directory.EnumerateFiles("Entities/");
            foreach (string entityFile in entityFiles) {
                var doc = XDocument.Load(entityFile);
                Classes.Add(new GameDataObject(doc.Root, ClassType.Point));
            }

            Includes = new List<string>();
            MaterialExclusions = new List<string>();
            AutoVisgroups = new List<AutoVisgroupSection>();
        }

        public void CreateDependencies() {
            var resolved = new List<string>();
            var unresolved = new List<GameDataObject>(Classes);
            while (unresolved.Any()) {
                var resolve = unresolved.Where(x => x.BaseClasses.All(resolved.Contains)).ToList();
                if (!resolve.Any()) throw new Exception("Circular dependencies: " + String.Join(", ", unresolved.Select(x => x.Name)));
                resolve.ForEach(x => x.Inherit(Classes.Where(y => x.BaseClasses.Contains(y.Name))));
                unresolved.RemoveAll(resolve.Contains);
                resolved.AddRange(resolve.Select(x => x.Name));
            }
        }

        public void RemoveDuplicates() {
            foreach (var g in Classes.Where(x => x.ClassType != ClassType.Base).GroupBy(x => x.Name.ToLowerInvariant()).Where(g => g.Count() > 1).ToList()) {
                foreach (var obj in g.Skip(1)) Classes.Remove(obj);
            }
        }
    }
}

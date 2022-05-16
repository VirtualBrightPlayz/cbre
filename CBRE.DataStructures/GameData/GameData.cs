using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CBRE.DataStructures.GameData {
    public class GameData {
        public int MapSizeLow { get; }
        public int MapSizeHigh { get; }
        public ImmutableArray<GameDataObject> Classes { get; }

        public GameData() {
            MapSizeHigh = 16384;
            MapSizeLow = -16384;
            
            var classes = new List<GameDataObject>();
            IEnumerable<string> entityFiles = Directory.EnumerateFiles("Entities/");
            foreach (string entityFile in entityFiles) {
                var doc = XDocument.Load(entityFile);
                classes.Add(new GameDataObject(doc.Root, ClassType.Point));
            }
            Classes = classes.ToImmutableArray();
        }
    }
}

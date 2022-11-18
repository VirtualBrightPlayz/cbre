using CBRE.DataStructures.Geometric;
using System.Collections.Generic;
using System.Drawing;

namespace CBRE.DataStructures.GameData {
    public class Behaviour {
        public string Name { get; set; }
        public List<string> Values { get; set; }

        public Behaviour(string name, params string[] values) {
            Name = name;
            Values = new List<string>(values);
        }

        public Vector3? GetVector3(int index) {
            var first = index * 3;
            return Values.Count < first + 3 ?
                null : Vector3.Parse(Values[first], Values[first + 1], Values[first + 2]);
        }

        public Color GetColour(int index) {
            var coord = GetVector3(index);
            return coord is { } value
                ? Color.FromArgb((int)value.X, (int)value.Y, (int)value.Z)
                : Color.White;
        }
    }
}

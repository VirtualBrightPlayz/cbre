using CBRE.DataStructures.GameData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Property = CBRE.DataStructures.MapObjects.Property;

namespace CBRE.Editor.Popup.ObjectProperties
{
    public class TableValue
    {
        public VariableType Class { get; set; } = VariableType.String;
        public string OriginalKey { get; set; }
        public string NewKey { get; set; }
        public string Value { get; set; }
        public bool IsModified { get; set; }
        public bool IsAdded { get; set; }
        public bool IsRemoved { get; set; }

        public Color GetStateColour()
        {
            if (IsAdded) return Color.LightBlue;
            if (IsRemoved) return Color.LightPink;
            if (IsModified) return Color.LightGreen;
            return Color.Transparent;
        }

        public string GetState()
        {
            if (IsAdded) return "Added";
            if (IsRemoved) return "Removed";
            if (IsModified) return "Modified";
            return "No change";
        }

        public Color GetColour255(Color defaultIfInvalid) {
            var spl = Value.Split(' ');
            if (spl.Length != 4 && spl.Length != 3) return defaultIfInvalid;
            int r, g, b, i;
            i = 255;
            if (int.TryParse(spl[0], out r) && int.TryParse(spl[1], out g) && int.TryParse(spl[2], out b) && (spl.Length == 3 || int.TryParse(spl[3], out i))) {
                return Color.FromArgb(i, r, g, b);
            }
            return defaultIfInvalid;
        }

        public Vector3 GetVector3(Vector3 defaultIfInvalid) {
            var spl = Value.Split(' ');
            if (spl.Length != 3) return defaultIfInvalid;
            float x, y, z;
            if (float.TryParse(spl[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(spl[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && float.TryParse(spl[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)) {
                return new Vector3(x, y, z);
            }
            return defaultIfInvalid;
        }

        public TableValue() {}

        public TableValue(Property prop) {
            OriginalKey = prop.Key;
            NewKey = prop.Key;
            Value = prop.Value;
        }
    }
}
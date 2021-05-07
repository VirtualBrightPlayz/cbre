using CBRE.DataStructures.GameData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Property = CBRE.DataStructures.MapObjects.Property;

namespace CBRE.Editor.Popup.ObjectProperties
{
    public class TableValue
    {
        public string Class { get; set; }
        public string OriginalKey { get; set; }
        public string NewKey { get; set; }
        public string Value { get; set; }
        public bool IsModified { get; set; }
        public bool IsAdded { get; set; }
        public bool IsRemoved { get; set; }

        public Color GetColour()
        {
            if (IsAdded) return Color.LightBlue;
            if (IsRemoved) return Color.LightPink;
            if (IsModified) return Color.LightGreen;
            return Color.Transparent;
        }

        public TableValue() {}

        public TableValue(Property prop) {
            OriginalKey = prop.Key;
            NewKey = prop.Key;
            Value = prop.Value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CBRE.Common;
using BindingFlags = System.Reflection.BindingFlags;

namespace CBRE.Editor.Brushes {
    public static class BrushManager {
        public static IBrush CurrentBrush { get; set; }

        public static readonly ImmutableHashSet<IBrush> Brushes;

        //private static ComboBox _comboBox;
        public static bool RoundCreatedVertices;

        static BrushManager() {
            RoundCreatedVertices = true;
            var brushTypes = ReflectionUtils.GetDerivedNonAbstract<IBrush>();
            var brushSet = new HashSet<IBrush>();
            foreach (var type in brushTypes) {
                brushSet.Add(
                    type.GetConstructor(Array.Empty<Type>())
                        .Invoke(Array.Empty<object>()) as IBrush);
            }
            Brushes = brushSet.ToImmutableHashSet();
        }
    }
}

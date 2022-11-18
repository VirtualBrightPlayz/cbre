using System;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    public static class ImGuiExtensions {
        
        public static void PushClipRectButItDoesntSuckAss(this ImDrawListPtr drawList, Num.Vector2 clipRectMin, Num.Vector2 clipRectMax) {
            Num.Vector2 prevMin = drawList.GetClipRectMin();
            Num.Vector2 prevMax = drawList.GetClipRectMax();
            clipRectMin.X = Math.Max(prevMin.X, clipRectMin.X);
            clipRectMin.Y = Math.Max(prevMin.Y, clipRectMin.Y);
            clipRectMax.X = Math.Min(prevMax.X, clipRectMax.X);
            clipRectMax.Y = Math.Min(prevMax.Y, clipRectMax.Y);
            drawList.PushClipRect(clipRectMin, clipRectMax);
        }
    }
}

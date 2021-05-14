using System;
using System.Collections.Generic;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Rendering;
using CBRE.Settings;

namespace CBRE.Editor.Tools.DisplacementTool {
    public abstract class DisplacementSubTool : BaseTool {
        public DisplacementTool MainTool { get; set; }

        protected DisplacementSubTool(DisplacementTool tool) {
            MainTool = tool;
        }

        public override string GetIcon() {
            throw new NotImplementedException();
        }

        public override HotkeyTool? GetHotkeyToolType()
        {
            return null;
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters)
        {
            switch (hotkeyMessage)
            {
                case HotkeysMediator.OperationsCopy:
                case HotkeysMediator.OperationsCut:
                case HotkeysMediator.OperationsPaste:
                case HotkeysMediator.OperationsPasteSpecial:
                case HotkeysMediator.OperationsDelete:
                    return HotkeyInterceptResult.Abort;
            }
            return HotkeyInterceptResult.Continue;
        }

        public abstract void DragStart(List<DisplacementPoint> clickedPoints);
        public abstract void DragMove(Vector3 distance);
        public abstract void DragEnd();
        public abstract void Render3D(Viewport3D viewport);
    }
}
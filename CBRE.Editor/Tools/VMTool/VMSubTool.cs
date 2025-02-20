﻿using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CBRE.Editor.Tools.VMTool
{
    public abstract class VMSubTool : BaseTool
    {
        //public Control Control { get; set; }
        public VMTool MainTool { get; set; }

        protected VMSubTool(VMTool mainTool)
        {
            MainTool = mainTool;
        }

        public override string GetIcon()
        {
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

        public abstract List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport2D viewport);
        public abstract List<VMPoint> GetVerticesAtPoint(int x, int y, Viewport3D viewport);
        public abstract void DragStart(List<VMPoint> clickedPoints);
        public abstract void DragMove(Vector3 distance);
        public abstract void DragEnd();

        public abstract void Render2D(Viewport2D viewport);
        public abstract void Render3D(Viewport3D viewport);
        public abstract void SelectionChanged();
        public abstract bool ShouldDeselect(List<VMPoint> vtxs);
        public abstract bool NoSelection();
        public abstract bool No3DSelection();
        public abstract bool DrawVertices();
    }
}

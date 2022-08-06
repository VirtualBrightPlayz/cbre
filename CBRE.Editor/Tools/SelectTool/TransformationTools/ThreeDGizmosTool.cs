using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Tools.Widgets;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Tools.SelectTool.TransformationTools
{
    /// <summary>
    /// Allows the selected objects to be scaled and translated
    /// </summary>
    class ThreeDGizmosTool : TransformationTool
    {

        public override bool RenderCircleHandles
        {
            get { return false; }
        }

        public override bool FilterHandle(BaseBoxTool.ResizeHandle handle)
        {
            return true;
        }

        public override string GetTransformName()
        {
            return "3D Gizmo";
        }

        public override MouseCursor CursorForHandle(BaseBoxTool.ResizeHandle handle)
        {
            return null;
        }

        public override IEnumerable<Widget> GetWidgets(Document document)
        {
            yield return new ImGuizmoWidget(document);
        }

        public override Matrix GetTransformationMatrix(Viewport2D viewport, ViewportEvent mouseEventArgs, BaseBoxTool.BoxState state, Document doc, IEnumerable<Widget> activeWidgets) {
            return Matrix.Identity;
        }
    }
}

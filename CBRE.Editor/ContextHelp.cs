using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Editor.Tools.SelectTool;
using CBRE.Editor.Tools.TextureTool;
using CBRE.Editor.Tools.VMTool;
using CBRE.Graphics;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        // This can be it's own file for now lol.
        protected virtual void UpdateContextHelp() {
            if (SelectedTool != null) {
                string text = SelectedTool.GetContextualHelp();
                if (!string.IsNullOrEmpty(text)) {
                    ImGui.PushTextWrapPos(200);
                    ImGui.TextWrapped(text);
                    ImGui.PopTextWrapPos();
                }
            }
        }
    }
}
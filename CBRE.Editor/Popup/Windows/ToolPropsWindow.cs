using System;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class ToolPropsWindow : WindowUI
    {
        public ToolPropsWindow() : base("")
        {
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Begin("tool_properties")) {
                ImGui.DockSpaceOverViewport();
                var Window = GameMain.Instance.Window;
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.Right, 47), ImGuiCond.FirstUseEver);
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, Window.ClientBounds.Height - 47 - 60), ImGuiCond.FirstUseEver);
                // if (ImGui.BeginChildFrame(3, new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, Window.ClientBounds.Height - 47 - 60))) {
                    if (ImGui.TreeNode("Tool")) {
                        GameMain.Instance.SelectedTool?.UpdateGui();
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("Contextual Help")) {
                        GameMain.Instance.UpdateContextHelp();
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("Viewport Options")) {
                        for (int i = 0; i < ViewportManager.Viewports.Length; i++) {
                            if (ViewportManager.Viewports[i] is Viewport3D viewport3D) {
                                if (ImGui.BeginCombo("Viewport Render Type", viewport3D.Type.ToString())) {
                                    var evals = Enum.GetValues<Viewport3D.ViewType>();
                                    for (int j = 0; j < evals.Length; j++) {
                                        if (ImGui.Selectable(evals[j].ToString(), viewport3D.Type == evals[j])) {
                                            viewport3D.Type = evals[j];
                                            ViewportManager.MarkForRerender();
                                            DocumentManager.Documents.ForEach(p => p.ObjectRenderer.MarkDirty());
                                        }
                                    }
                                    ImGui.EndCombo();
                                }
                                bool b = viewport3D.ShouldRenderModels;
                                if (ImGui.Checkbox("Should Render 3D Models", ref b)) {
                                    viewport3D.ShouldRenderModels = b;
                                }
                            }
                        }
                        ImGui.TreePop();
                    }

                    // ImGui.EndChildFrame();
                // }
                ImGui.End();
            }
            return true;
        }
    }
}
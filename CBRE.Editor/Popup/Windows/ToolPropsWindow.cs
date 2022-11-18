using System;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class ToolPropsWindow : DockableWindow
    {
        public ToolPropsWindow() : base("tool_properties", ImGuiWindowFlags.None)
        {
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            var window = GameMain.Instance.Window;
            ImGui.SetWindowPos(new Num.Vector2(ViewportManager.VPRect.Right, 47), ImGuiCond.FirstUseEver);
            ImGui.SetWindowSize(new Num.Vector2(window.ClientBounds.Width - ViewportManager.VPRect.Right, window.ClientBounds.Height - 47 - 60), ImGuiCond.FirstUseEver);
            ImGui.SetNextItemOpen(is_open: true, ImGuiCond.FirstUseEver);
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
                        b = viewport3D.ScreenshotRender;
                        if (ImGui.Checkbox("Screenshot mode", ref b)) {
                            viewport3D.ScreenshotRender = b;
                        }
                        float f = viewport3D.Gamma;
                        if (ImGui.InputFloat("Gamma", ref f)) {
                            viewport3D.Gamma = f;
                        }
                    }
                }
                ImGui.TreePop();
            }
        }
    }
}

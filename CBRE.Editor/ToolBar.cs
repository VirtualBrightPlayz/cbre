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
        public List<ToolBarItem> ToolBarItems;

        private int selectedToolIndex = -1;

        public BaseTool SelectedTool {
            get {
                if (selectedToolIndex < 0) { return null; }
                return ToolBarItems[selectedToolIndex].Tool;
            }
            set {
                SelectedTool?.ToolDeselected(false);
                value?.ToolSelected(false);
                selectedToolIndex = ToolBarItems.FindIndex(tbi => tbi.Tool == value);
            }
        }

        public void InitToolBar() {
            ToolBarItems = new List<ToolBarItem>();

            for (int i=0;i<ToolManager.Tools.Count;i++) {
                ToolBarItems.Add(new ToolBarItem(ToolManager.Tools[i]));
            }
        }

        public void UpdateToolBar() {
            if (ImGui.BeginChildFrame(2, new Num.Vector2(47, Window.ClientBounds.Height + 1))) {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Num.Vector2(0, 1));

                ToolBarItems.ForEach(it => it.Draw());

                ImGui.PopStyleVar();

                ImGui.EndChildFrame();
            }
        }

        public class ToolBarItem {
            public string ToolTip;
            public AsyncTexture Texture;
            public BaseTool Tool;

            public ToolBarItem(BaseTool tool) {
                ToolTip = tool.GetName();
                Texture = MenuTextures[tool.GetIcon()];
                Tool = tool;
            }

            public virtual void Draw() {
                bool isSelected = GameMain.Instance.SelectedTool == Tool;
                using var _ = new ColorPush(ImGuiCol.Button, isSelected ? GlobalGraphics.SelectedColors.Button : null);
                using var __ = new ColorPush(ImGuiCol.ButtonActive, isSelected ? GlobalGraphics.SelectedColors.ButtonActive : null);
                using var ___ = new ColorPush(ImGuiCol.ButtonHovered, isSelected ? GlobalGraphics.SelectedColors.ButtonHovered : null);

                bool pressed;
                if (Texture.ImGuiTexture != IntPtr.Zero) {
                    pressed = ImGui.ImageButton(Texture.ImGuiTexture, new Num.Vector2(32, 32));
                } else {
                    pressed = ImGui.Button($"##{Tool}", new Num.Vector2(40, 38));
                }

                if (pressed && GameMain.Instance.SelectedTool != Tool) {
                    GameMain.Instance.SelectedTool = Tool;
                    ViewportManager.MarkForRerender();
                }

                if (ImGui.IsItemHovered()) {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Num.Vector2.One * 4);
                    ImGui.BeginTooltip();
                    ImGui.SetTooltip(ToolTip);
                    ImGui.EndTooltip();
                    ImGui.PopStyleVar();
                }
            }
        }
    }
}

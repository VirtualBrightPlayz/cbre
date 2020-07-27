using System;
using System.Collections.Generic;
using System.Text;
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

        public void InitToolBar() {
            ToolBarItems = new List<ToolBarItem>();

            ToolBarItems.Add(new ToolBarItem(new SelectTool()));
            ToolBarItems.Add(new ToolBarItem(new CameraTool()));
            ToolBarItems.Add(new ToolBarItem(new EntityTool()));
            ToolBarItems.Add(new ToolBarItem(new BrushTool()));
            ToolBarItems.Add(new ToolBarItem(new TextureTool()));
            ToolBarItems.Add(new ToolBarItem(new ClipTool()));
            ToolBarItems.Add(new ToolBarItem(new VMTool()));
        }

        public void UpdateToolBar() {
            ImGui.SetCursorPos(new Num.Vector2(0, 47));

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
                bool pressed;
                if (Texture.ImGuiTexture != IntPtr.Zero) {
                    pressed = ImGui.ImageButton(Texture.ImGuiTexture, new Num.Vector2(32, 32));
                } else {
                    pressed = ImGui.Button("", new Num.Vector2(40, 38));
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

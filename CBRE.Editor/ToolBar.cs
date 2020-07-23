using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Graphics;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<ToolBarItem> ToolBarItems;

        public void InitToolBar() {
            ToolBarItems = new List<ToolBarItem>();

            ToolBarItems.Add(new ToolBarItem("Select Tool", menuTextures["Tool_Select"]));
            ToolBarItems.Add(new ToolBarItem("Camera", menuTextures["Tool_Camera"]));
            ToolBarItems.Add(new ToolBarItem("Entity Tool", menuTextures["Tool_Entity"]));
            ToolBarItems.Add(new ToolBarItem("Brush Tool", menuTextures["Tool_Brush"]));
            ToolBarItems.Add(new ToolBarItem("Texture Application Tool", menuTextures["Tool_Texture"]));
            ToolBarItems.Add(new ToolBarItem("Clip Tool", menuTextures["Tool_Clip"]));
            ToolBarItems.Add(new ToolBarItem("Vertex Manipulation Tool", menuTextures["Tool_VM"]));
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

            public ToolBarItem(string toolTip, AsyncTexture texture) {
                ToolTip = toolTip;
                Texture = texture;
            }

            public virtual void Draw() {
                bool pressed;
                if (Texture.ImGuiTexture != IntPtr.Zero) {
                    pressed = ImGui.ImageButton(Texture.ImGuiTexture, new Num.Vector2(32, 32));
                } else {
                    pressed = ImGui.Button("", new Num.Vector2(40, 38));
                }
            }
        }
    }
}

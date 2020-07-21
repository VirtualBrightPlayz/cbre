using System;
using System.Collections.Generic;
using System.Text;
using CBRE.DataStructures.Models;
using CBRE.Graphics;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<TopBarItem> TopBarItems;

        private void InitTopBar() {
            TopBarItems = new List<TopBarItem>();

            TopBarItems.Add(new TopBarItem("New", menuTextures["Menu_New"]));
            TopBarItems.Add(new TopBarItem("Open", menuTextures["Menu_Open"]));
            TopBarItems.Add(new TopBarItem("Close", menuTextures["Menu_Close"]));
            TopBarItems.Add(new TopBarItem("Save", menuTextures["Menu_Save"]));
            TopBarItems.Add(new TopBarItem("Export / Lightmap", menuTextures["Menu_ExportRmesh"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Undo", menuTextures["Menu_Undo"]));
            TopBarItems.Add(new TopBarItem("Redo", menuTextures["Menu_Redo"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Cut", menuTextures["Menu_Cut"]));
            TopBarItems.Add(new TopBarItem("Copy", menuTextures["Menu_Copy"]));
            TopBarItems.Add(new TopBarItem("Paste", menuTextures["Menu_Paste"]));
            TopBarItems.Add(new TopBarItem("Paste Special", menuTextures["Menu_PasteSpecial"]));
            TopBarItems.Add(new TopBarItem("Delete", menuTextures["Menu_Delete"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Object Properties", menuTextures["Menu_ObjectProperties"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Snap To Grid", menuTextures["Menu_SnapToGrid"]));
            TopBarItems.Add(new TopBarItem("Show 2D Grid", menuTextures["Menu_Show2DGrid"]));
            TopBarItems.Add(new TopBarItem("Show 3D Grid", menuTextures["Menu_Show3DGrid"]));
            TopBarItems.Add(new TopBarItem("Smaller Grid", menuTextures["Menu_SmallerGrid"]));
            TopBarItems.Add(new TopBarItem("Bigger Grid", menuTextures["Menu_LargerGrid"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Ignore Grouping", menuTextures["Menu_IgnoreGrouping"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Texture Lock", menuTextures["Menu_TextureLock"], isToggle: true));
            TopBarItems.Add(new TopBarItem("Texture Scaling Lock", menuTextures["Menu_TextureScalingLock"], isToggle: true));
            TopBarItems.Add(new TopBarItem("Hide Null Textures", menuTextures["Menu_HideNullTextures"], isToggle: true));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Hide Selected Objects", menuTextures["Menu_HideSelected"]));
            TopBarItems.Add(new TopBarItem("Hide Unselected Objects", menuTextures["Menu_HideUnselected"]));
            TopBarItems.Add(new TopBarItem("Show Hidden Objects", menuTextures["Menu_ShowHidden"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Carve", menuTextures["Menu_Carve"]));
            TopBarItems.Add(new TopBarItem("Make Hollow", menuTextures["Menu_Hollow"]));
            TopBarItems.Add(new TopBarItem("Group", menuTextures["Menu_Group"]));
            TopBarItems.Add(new TopBarItem("Ungroup", menuTextures["Menu_Ungroup"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Options", menuTextures["Menu_Options"]));
        }

        private void UpdateTopBar() {
            ImGui.SetCursorPos(new Num.Vector2(0, 19));

            if (ImGui.BeginChildFrame(1, new Num.Vector2(Window.ClientBounds.Width+1, 28))) {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Num.Vector2(1, 0));

                ImGui.Dummy(new Num.Vector2(2,0));
                ImGui.SameLine();

                TopBarItems.ForEach(it => it.Draw());

                ImGui.PopStyleVar();

                ImGui.EndChildFrame();
            }
            ImGui.End();
        }

        public class TopBarItem {
            public string ToolTip;
            public AsyncTexture Texture;
            public Action Action;
            public bool IsToggle;
            public bool Toggled;

            public TopBarItem(string toolTip, AsyncTexture texture, bool isToggle=false, Action action=null) {
                ToolTip = toolTip;
                Texture = texture;
                Action = action;
                IsToggle = isToggle;
            }

            public virtual void Draw() {
                if (Toggled) {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Num.Vector4(0.3f, 0.6f, 0.7f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Num.Vector4(0.15f, 0.3f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Num.Vector4(0.45f, 0.9f, 1.0f, 1.0f));
                }

                bool pressed;
                if (Texture.ImGuiTexture != IntPtr.Zero) {
                    pressed = ImGui.ImageButton(Texture.ImGuiTexture, new Num.Vector2(16, 16));
                } else {
                    pressed = ImGui.Button("", new Num.Vector2(24, 22));
                }
                ImGui.SameLine();

                if (Toggled) {
                    ImGui.PopStyleColor(3);
                }

                if (pressed) {
                    if (IsToggle) { Toggled = !Toggled; }
                    Action?.Invoke();
                }
            }
        }

        public class TopBarSeparator : TopBarItem {
            public TopBarSeparator() : base("", null, false, null) { }

            public override void Draw() {
                var drawList = ImGui.GetWindowDrawList();
                ImGui.Dummy(new Num.Vector2(2, 22));
                ImGui.SameLine();

                var pos = ImGui.GetCursorScreenPos();
                drawList.AddLine(pos, pos+new Num.Vector2(0,22), 0xff444444);

                ImGui.Dummy(new Num.Vector2(3, 22));
                ImGui.SameLine();
            }
        }
    }
}

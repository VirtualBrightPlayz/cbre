using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CBRE.DataStructures.Models;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Graphics;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<TopBarItem> TopBarItems;

        private void InitTopBar() {
            TopBarItems = new List<TopBarItem>();

            TopBarItems.Add(new TopBarItem("New", MenuTextures["Menu_New"]));
            TopBarItems.Add(new TopBarItem("Open", MenuTextures["Menu_Open"]));
            TopBarItems.Add(new TopBarItem("Close", MenuTextures["Menu_Close"], action: Top_Close));
            TopBarItems.Add(new TopBarItem("Save", MenuTextures["Menu_Save"]));
            TopBarItems.Add(new TopBarItem("Export / Lightmap", MenuTextures["Menu_ExportRmesh"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Undo", MenuTextures["Menu_Undo"]));
            TopBarItems.Add(new TopBarItem("Redo", MenuTextures["Menu_Redo"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Cut", MenuTextures["Menu_Cut"]));
            TopBarItems.Add(new TopBarItem("Copy", MenuTextures["Menu_Copy"]));
            TopBarItems.Add(new TopBarItem("Paste", MenuTextures["Menu_Paste"]));
            TopBarItems.Add(new TopBarItem("Paste Special", MenuTextures["Menu_PasteSpecial"]));
            TopBarItems.Add(new TopBarItem("Delete", MenuTextures["Menu_Delete"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Object Properties", MenuTextures["Menu_ObjectProperties"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Snap To Grid", MenuTextures["Menu_SnapToGrid"]));
            TopBarItems.Add(new TopBarItem("Show 2D Grid", MenuTextures["Menu_Show2DGrid"]));
            TopBarItems.Add(new TopBarItem("Show 3D Grid", MenuTextures["Menu_Show3DGrid"]));
            TopBarItems.Add(new TopBarItem("Smaller Grid", MenuTextures["Menu_SmallerGrid"]));
            TopBarItems.Add(new TopBarItem("Bigger Grid", MenuTextures["Menu_LargerGrid"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Ignore Grouping", MenuTextures["Menu_IgnoreGrouping"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Texture Lock", MenuTextures["Menu_TextureLock"], isToggle: true));
            TopBarItems.Add(new TopBarItem("Texture Scaling Lock", MenuTextures["Menu_TextureScalingLock"], isToggle: true));
            TopBarItems.Add(new TopBarItem("Hide Null Textures", MenuTextures["Menu_HideNullTextures"], isToggle: true));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Hide Selected Objects", MenuTextures["Menu_HideSelected"]));
            TopBarItems.Add(new TopBarItem("Hide Unselected Objects", MenuTextures["Menu_HideUnselected"]));
            TopBarItems.Add(new TopBarItem("Show Hidden Objects", MenuTextures["Menu_ShowHidden"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Carve", MenuTextures["Menu_Carve"]));
            TopBarItems.Add(new TopBarItem("Make Hollow", MenuTextures["Menu_Hollow"]));
            TopBarItems.Add(new TopBarItem("Group", MenuTextures["Menu_Group"]));
            TopBarItems.Add(new TopBarItem("Ungroup", MenuTextures["Menu_Ungroup"]));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Options", MenuTextures["Menu_Options"]));
        }
    
        #region Actions

        private void Top_Close() {
            new SavePopup("Warning", $"You have changes. Save {Path.GetFileName(DocumentManager.CurrentDocument.MapFile)}?", Directory.GetCurrentDirectory(), DocumentManager.CurrentDocument.Map);
        }

        #endregion

        private void UpdateTopBar() {
            ImGui.SetCursorPos(new Num.Vector2(0, 19));

            if (ImGui.BeginChildFrame(1, new Num.Vector2(Window.ClientBounds.Width + 1, 28))) {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Num.Vector2(1, 0));

                ImGui.Dummy(new Num.Vector2(2, 0));
                ImGui.SameLine();

                TopBarItems.ForEach(it => it.Draw());

                ImGui.PopStyleVar();

                ImGui.EndChildFrame();
            }
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
                    pressed = ImGui.Button($"##{Texture.Name}", new Num.Vector2(24, 22));
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Graphics;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<Menu> Menus = new List<Menu>();

        private void InitMenus() {
            Menus.Add(new Menu("File",
                new MenuItem("New", "Ctrl+N", menuTextures["Menu_New"]),
                new MenuItem("Open", "Ctrl+O", menuTextures["Menu_Open"]),
                new MenuItem("Close", "", menuTextures["Menu_Close"]),
                new MenuItem("Save", "Ctrl+S", menuTextures["Menu_Save"]),
                new MenuItem("Save as", "Ctrl+Shift+S", menuTextures["Menu_SaveAs"]),
                new MenuSeparator(),
                new MenuItem("Export / Lightmap", "F9", menuTextures["Menu_ExportRmesh"]),
                new MenuSeparator(),
                new MenuItem("Exit", "") { Action = () => { Exit(); } }));
            Menus.Add(new Menu("Edit",
                new MenuItem("Undo", "Ctrl+Z", menuTextures["Menu_Undo"]),
                new MenuItem("Redo", "Ctrl+Y", menuTextures["Menu_Redo"]),
                new MenuSeparator(),
                new MenuItem("Cut", "Ctrl+X", menuTextures["Menu_Cut"]),
                new MenuItem("Copy", "Ctrl+C", menuTextures["Menu_Copy"]),
                new MenuItem("Paste", "Ctrl+V", menuTextures["Menu_Paste"]),
                new MenuItem("Paste Special...", "", menuTextures["Menu_PasteSpecial"]),
                new MenuItem("Delete", "Del", menuTextures["Menu_Delete"]),
                new MenuSeparator(),
                new MenuItem("Clear Selection", "", menuTextures["Menu_ClearSelection"]),
                new MenuItem("Select All", "Ctrl+A", menuTextures["Menu_SelectAll"]),
                new MenuSeparator(),
                new MenuItem("Object Properties", "Alt+Enter", menuTextures["Menu_ObjectProperties"])));
            Menus.Add(new Menu("Map",
                new MenuItem("Snap to Grid", "", menuTextures["Menu_SnapToGrid"]),
                new MenuItem("Show 2D Grid", "", menuTextures["Menu_Show2DGrid"]),
                new MenuItem("Show 3D Grid", "", menuTextures["Menu_Show3DGrid"]),
                new Menu("Grid Settings",
                    new MenuItem("Smaller Grid", "", menuTextures["Menu_SmallerGrid"]),
                    new MenuItem("Bigger Grid", "", menuTextures["Menu_LargerGrid"])),
                new MenuSeparator(),
                new MenuItem("Ignore Grouping", "", menuTextures["Menu_IgnoreGrouping"]),
                new MenuSeparator(),
                new MenuItem("Texture Lock", "", menuTextures["Menu_TextureLock"]),
                new MenuItem("Texture Scaling Lock", "", menuTextures["Menu_TextureScalingLock"]),
                new MenuSeparator(),
                new MenuItem("Hide Null Textures", "", menuTextures["Menu_HideNullTextures"]),
                new MenuSeparator(),
                new MenuItem("Show Information", "", menuTextures["Menu_ShowInformation"]),
                new MenuItem("Show Selected Brush ID", "", menuTextures["Menu_ShowBrushID"]),
                new MenuItem("Entity Report...", "", menuTextures["Menu_EntityReport"]),
                new MenuItem("Check for Problems", "", menuTextures["Menu_CheckForProblems"]),
                new MenuItem("Show Logical Tree", "", menuTextures["Menu_ShowLogicalTree"]),
                new MenuSeparator(),
                new MenuItem("Map Properties...", "", menuTextures["Menu_MapProperties"])));
            Menus.Add(new Menu("View",
                new MenuItem("Autosize All Views", "", menuTextures["Menu_AutosizeViews"]),
                new MenuItem("Center All Views on Selection", "", menuTextures["Menu_CenterSelectionAll"]),
                new MenuItem("Center 2D Views on Selection", "", menuTextures["Menu_CenterSelection2D"]),
                new MenuItem("Center 3D View on Selection", "", menuTextures["Menu_CenterSelection3D"]),
                new MenuSeparator(),
                new MenuItem("Go to Brush ID...", "", menuTextures["Menu_GoToBrushID"]),
                new MenuItem("Go to Coordinates...", "", menuTextures["Menu_GoToCoordinates"]),
                new MenuSeparator(),
                new MenuItem("Hide Selected Objects", "", menuTextures["Menu_HideSelected"]),
                new MenuItem("Hide Unselected Objects", "", menuTextures["Menu_HideUnselected"]),
                new MenuItem("Show Hidden Objects", "", menuTextures["Menu_ShowHidden"])));
            Menus.Add(new Menu("Tools",
                new MenuItem("Carve", "", menuTextures["Menu_Carve"]),
                new MenuItem("Make Hollow", "", menuTextures["Menu_Hollow"]),
                new MenuSeparator(),
                new MenuItem("Group", "", menuTextures["Menu_Group"]),
                new MenuItem("Ungroup", "", menuTextures["Menu_Ungroup"]),
                new MenuSeparator(),
                new MenuItem("Tie to Entity", "", menuTextures["Menu_TieToEntity"]),
                new MenuItem("Move to World", "", menuTextures["Menu_TieToWorld"]),
                new MenuSeparator(),
                new MenuItem("Replace Textures", "", menuTextures["Menu_ReplaceTextures"]),
                new MenuSeparator(),
                new MenuItem("Transform...", "", menuTextures["Menu_Transform"]),
                new MenuItem("Snap Selected to Grid", "", menuTextures["Menu_SnapSelection"]),
                new MenuItem("Snap Selected to Grid Individually", "", menuTextures["Menu_SnapSelectionIndividual"]),
                new Menu("Align Objects", menuTextures["Menu_Align"],
                    new MenuItem("To X Axis Min", ""),
                    new MenuItem("To X Axis Max", ""),
                    new MenuItem("To Y Axis Min", ""),
                    new MenuItem("To Y Axis Max", ""),
                    new MenuItem("To Z Axis Min", ""),
                    new MenuItem("To Z Axis Max", "")),
                new Menu("Flip Objects", menuTextures["Menu_Flip"],
                    new MenuItem("X Axis", ""),
                    new MenuItem("Y Axis", ""),
                    new MenuItem("Z Axis", "")),
                new MenuSeparator(),
                new MenuItem("Options...", "", menuTextures["Menu_Options"])));
            Menus.Add(new Menu("Layout",
                new MenuItem("Create New Layout Window", "", menuTextures["Menu_NewWindow"]),
                new MenuItem("Layout Window Settings...", "", menuTextures["Menu_WindowSettings"])));
            Menus.Add(new Menu("Help",
                new MenuItem("About...", "")));
        }

        private void UpdateMenus() {
            if (ImGui.BeginMenuBar()) {
                for (int i = 0; i < Menus.Count; i++) {
                    Menus[i].Draw(true);
                }

                ImGui.EndMenuBar();
            }
        }

        public class MenuItem {
            public MenuItem(string name, string shortcut, AsyncTexture texture = null) {
                Name = name;
                Shortcut = shortcut;
                Texture = texture;
            }

            public virtual void Draw(bool topLevel) {
                Num.Vector2 pos = ImGui.GetCursorPos() + ImGui.GetWindowPos();
                if (ImGui.MenuItem(GetDrawnText(topLevel), Shortcut)) {
                    Action?.Invoke();
                }
                RenderIcon(pos);
            }

            protected void RenderIcon(Num.Vector2 pos) {
                if (Texture != null && Texture.ImGuiTexture != IntPtr.Zero) {
                    //ImGui.GetForegroundDrawList().AddImage(Texture.ImGuiTexture, pos + new Num.Vector2(0,0), pos + new Num.Vector2(16, 16), Num.Vector2.Zero, Num.Vector2.One, 0x77000000);
                    ImGui.GetForegroundDrawList().AddImage(Texture.ImGuiTexture, pos + new Num.Vector2(-2, -2), pos + new Num.Vector2(14, 14), Num.Vector2.Zero, Num.Vector2.One, 0xffffffff);
                }
            }

            protected string GetDrawnText(bool topLevel) {
                return (topLevel ? "" : "   ") + Name;
            }

            public Action Action;

            public string Name;
            public string Shortcut;
            public AsyncTexture Texture;
        }

        public class Menu : MenuItem {
            public Menu(string name, AsyncTexture texture, params MenuItem[] items) : base(name, "", texture) {
                Items = items.ToList();
            }

            public Menu(string name, params MenuItem[] items) : this(name, null, items) { }

            public override void Draw(bool topLevel) {
                Num.Vector2 pos = ImGui.GetCursorPos() + ImGui.GetWindowPos();
                if (ImGui.BeginMenu(GetDrawnText(topLevel))) {
                    Items.ForEach(it => it.Draw(false));
                    ImGui.EndMenu();
                }
                RenderIcon(pos);
            }
            public List<MenuItem> Items;
        }

        public class MenuSeparator : MenuItem {
            public MenuSeparator() : base("", "", null) { }

            public override void Draw(bool topLevel) {
                ImGui.Separator();
            }
        }
    }
}

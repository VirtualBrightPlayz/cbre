using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Common.Mediator;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<Menu> Menus = new List<Menu>();

        private void InitMenus() {
            Menus.Add(new Menu("File",
                new MenuItem(HotkeysMediator.FileNew.ToString(), MenuTextures["Menu_New"]),
                new MenuItem(HotkeysMediator.FileOpen.ToString(), MenuTextures["Menu_Open"]),
                new MenuItem(HotkeysMediator.FileClose.ToString(), MenuTextures["Menu_Close"]),
                new MenuItem(HotkeysMediator.FileSave.ToString(), MenuTextures["Menu_Save"]),
                new MenuItem(HotkeysMediator.FileSaveAs.ToString(), MenuTextures["Menu_SaveAs"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.FileCompile.ToString(), MenuTextures["Menu_ExportRmesh"]),
                new MenuSeparator(),
                new MenuItem("Exit", "", action: Exit)));
            Menus.Add(new Menu("Edit",
                new MenuItem(HotkeysMediator.HistoryUndo.ToString(), MenuTextures["Menu_Undo"]),
                new MenuItem(HotkeysMediator.HistoryRedo.ToString(), MenuTextures["Menu_Redo"]),
                new MenuSeparator(),
                new MenuItem("Cut", "Ctrl+X", MenuTextures["Menu_Cut"]),
                new MenuItem("Copy", "Ctrl+C", MenuTextures["Menu_Copy"]),
                new MenuItem("Paste", "Ctrl+V", MenuTextures["Menu_Paste"]),
                new MenuItem("Paste Special...", "", MenuTextures["Menu_PasteSpecial"]),
                new MenuItem(HotkeysMediator.OperationsDelete.ToString(), MenuTextures["Menu_Delete"]),
                new MenuSeparator(),
                new MenuItem("Clear Selection", "", MenuTextures["Menu_ClearSelection"]),
                new MenuItem("Select All", "Ctrl+A", MenuTextures["Menu_SelectAll"]),
                new MenuSeparator(),
                new MenuItem("Object Properties", "Alt+Enter", MenuTextures["Menu_ObjectProperties"])));
            Menus.Add(new Menu("Map",
                new MenuItem("Snap to Grid", "", MenuTextures["Menu_SnapToGrid"]),
                new MenuItem("Show 2D Grid", "", MenuTextures["Menu_Show2DGrid"]),
                new MenuItem("Show 3D Grid", "", MenuTextures["Menu_Show3DGrid"]),
                new Menu("Grid Settings",
                    new MenuItem("Smaller Grid", "", MenuTextures["Menu_SmallerGrid"]),
                    new MenuItem("Bigger Grid", "", MenuTextures["Menu_LargerGrid"])),
                new MenuSeparator(),
                new MenuItem("Ignore Grouping", "", MenuTextures["Menu_IgnoreGrouping"]),
                new MenuSeparator(),
                new MenuItem("Texture Lock", "", MenuTextures["Menu_TextureLock"]),
                new MenuItem("Texture Scaling Lock", "", MenuTextures["Menu_TextureScalingLock"]),
                new MenuSeparator(),
                new MenuItem("Hide Null Textures", "", MenuTextures["Menu_HideNullTextures"]),
                new MenuSeparator(),
                new MenuItem("Show Information", "", MenuTextures["Menu_ShowInformation"]),
                new MenuItem("Show Selected Brush ID", "", MenuTextures["Menu_ShowBrushID"]),
                new MenuItem("Entity Report...", "", MenuTextures["Menu_EntityReport"]),
                new MenuItem("Check for Problems", "", MenuTextures["Menu_CheckForProblems"]),
                new MenuItem("Show Logical Tree", "", MenuTextures["Menu_ShowLogicalTree"]),
                new MenuSeparator(),
                new MenuItem("Map Properties...", "", MenuTextures["Menu_MapProperties"], action: MapInformation)));
            Menus.Add(new Menu("View",
                new MenuItem("Autosize All Views", "", MenuTextures["Menu_AutosizeViews"]),
                new MenuItem("Center All Views on Selection", "", MenuTextures["Menu_CenterSelectionAll"]),
                new MenuItem("Center 2D Views on Selection", "", MenuTextures["Menu_CenterSelection2D"]),
                new MenuItem("Center 3D View on Selection", "", MenuTextures["Menu_CenterSelection3D"]),
                new MenuSeparator(),
                new MenuItem("Go to Brush ID...", "", MenuTextures["Menu_GoToBrushID"]),
                new MenuItem("Go to Coordinates...", "", MenuTextures["Menu_GoToCoordinates"]),
                new MenuSeparator(),
                new MenuItem("Hide Selected Objects", "", MenuTextures["Menu_HideSelected"]),
                new MenuItem("Hide Unselected Objects", "", MenuTextures["Menu_HideUnselected"]),
                new MenuItem("Show Hidden Objects", "", MenuTextures["Menu_ShowHidden"])));
            Menus.Add(new Menu("Tools",
                new MenuItem("Carve", "", MenuTextures["Menu_Carve"]),
                new MenuItem("Make Hollow", "", MenuTextures["Menu_Hollow"]),
                new MenuSeparator(),
                new MenuItem("Group", "", MenuTextures["Menu_Group"]),
                new MenuItem("Ungroup", "", MenuTextures["Menu_Ungroup"]),
                new MenuSeparator(),
                new MenuItem("Tie to Entity", "", MenuTextures["Menu_TieToEntity"]),
                new MenuItem("Move to World", "", MenuTextures["Menu_TieToWorld"]),
                new MenuSeparator(),
                new MenuItem("Replace Textures", "", MenuTextures["Menu_ReplaceTextures"]),
                new MenuSeparator(),
                new MenuItem("Transform...", "", MenuTextures["Menu_Transform"]),
                new MenuItem("Snap Selected to Grid", "", MenuTextures["Menu_SnapSelection"]),
                new MenuItem("Snap Selected to Grid Individually", "", MenuTextures["Menu_SnapSelectionIndividual"]),
                new Menu("Align Objects", MenuTextures["Menu_Align"],
                    new MenuItem("To X Axis Min", ""),
                    new MenuItem("To X Axis Max", ""),
                    new MenuItem("To Y Axis Min", ""),
                    new MenuItem("To Y Axis Max", ""),
                    new MenuItem("To Z Axis Min", ""),
                    new MenuItem("To Z Axis Max", "")),
                new Menu("Flip Objects", MenuTextures["Menu_Flip"],
                    new MenuItem("X Axis", ""),
                    new MenuItem("Y Axis", ""),
                    new MenuItem("Z Axis", "")),
                new MenuSeparator(),
                new MenuItem("Options...", "", MenuTextures["Menu_Options"], action: Options)));
            Menus.Add(new Menu("Layout",
                new MenuItem("Create New Layout Window", "", MenuTextures["Menu_NewWindow"]),
                new MenuItem("Layout Window Settings...", "", MenuTextures["Menu_WindowSettings"])));
            Menus.Add(new Menu("Help",
                new MenuItem("About...", "")));
        }

        private void UpdateMenus() {
            if (ImGui.BeginMenuBar()) {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Num.Vector2.One * 8);
                for (int i = 0; i < Menus.Count; i++) {
                    Menus[i].Draw(true);
                }
                ImGui.PopStyleVar();
                ImGui.EndMenuBar();
            }
        }

        public class MenuItem {
            public MenuItem(string name, string shortcut, AsyncTexture texture = null, Action action = null) {
                Name = name;
                Shortcut = shortcut;
                Texture = texture;
                Action = action;
            }

            public MenuItem(string hotkey, AsyncTexture texture = null) {
                var h = Hotkeys.GetHotkeyDefinitions().FirstOrDefault(p => p.ID == hotkey);
                Texture = texture;
                if (h == null) {
                    Name = hotkey;
                    Shortcut = "";
                    return;
                }
                Name = h.Name;
                Shortcut = string.Join("+", h.DefaultHotkeys);
                Action = () => {
                    Mediator.Publish(h.Action, h.Parameter);
                };
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
                    ViewportManager.TopMenuOpen = true;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Common.Mediator;
using CBRE.Editor.Popup;
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
                new MenuItem(HotkeysMediator.OperationsCut.ToString(), MenuTextures["Menu_Cut"]),
                new MenuItem(HotkeysMediator.OperationsCopy.ToString(), MenuTextures["Menu_Copy"]),
                new MenuItem(HotkeysMediator.OperationsPaste.ToString(), MenuTextures["Menu_Paste"]),
                new MenuItem(HotkeysMediator.OperationsPasteSpecial.ToString(), MenuTextures["Menu_PasteSpecial"]),
                new MenuItem(HotkeysMediator.OperationsDelete.ToString(), MenuTextures["Menu_Delete"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.SelectionClear.ToString(), MenuTextures["Menu_ClearSelection"]),
                new MenuItem(HotkeysMediator.SelectAll.ToString(), MenuTextures["Menu_SelectAll"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ObjectProperties.ToString(), MenuTextures["Menu_ObjectProperties"])));
            Menus.Add(new Menu("Map",
                new MenuItem("Snap to Grid", "", MenuTextures["Menu_SnapToGrid"]),
                new MenuItem("Show 2D Grid", "", MenuTextures["Menu_Show2DGrid"]),
                new MenuItem("Show 3D Grid", "", MenuTextures["Menu_Show3DGrid"]),
                new Menu("Grid Settings",
                    new MenuItem(HotkeysMediator.GridDecrease.ToString(), MenuTextures["Menu_SmallerGrid"]),
                    new MenuItem(HotkeysMediator.GridIncrease.ToString(), MenuTextures["Menu_LargerGrid"])),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ToggleIgnoreGrouping.ToString(), MenuTextures["Menu_IgnoreGrouping"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ToggleTextureLock.ToString(), MenuTextures["Menu_TextureLock"]),
                new MenuItem(HotkeysMediator.ToggleTextureScalingLock.ToString(), MenuTextures["Menu_TextureScalingLock"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ToggleHideNullTextures.ToString(), MenuTextures["Menu_HideNullTextures"]),
                new MenuSeparator(),
                new MenuItem("Show Information", "", MenuTextures["Menu_ShowInformation"]),
                new MenuItem(HotkeysMediator.ShowSelectedBrushID.ToString(), MenuTextures["Menu_ShowBrushID"]),
                new MenuItem(HotkeysMediator.ShowEntityReport.ToString(), MenuTextures["Menu_EntityReport"]),
                new MenuItem(HotkeysMediator.CheckForProblems.ToString(), MenuTextures["Menu_CheckForProblems"]),
                new MenuItem(HotkeysMediator.ShowLogicalTree.ToString(), MenuTextures["Menu_ShowLogicalTree"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ShowMapInformation.ToString(), MenuTextures["Menu_MapProperties"])));
            Menus.Add(new Menu("View",
                new MenuItem(HotkeysMediator.ViewportAutosize.ToString(), MenuTextures["Menu_AutosizeViews"]),
                new MenuItem(HotkeysMediator.CenterAllViewsOnSelection.ToString(), MenuTextures["Menu_CenterSelectionAll"]),
                new MenuItem(HotkeysMediator.Center2DViewsOnSelection.ToString(), MenuTextures["Menu_CenterSelection2D"]),
                new MenuItem(HotkeysMediator.Center3DViewsOnSelection.ToString(), MenuTextures["Menu_CenterSelection3D"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.GoToBrushID.ToString(), MenuTextures["Menu_GoToBrushID"]),
                new MenuItem(HotkeysMediator.GoToCoordinates.ToString(), MenuTextures["Menu_GoToCoordinates"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.QuickHideSelected.ToString(), MenuTextures["Menu_HideSelected"]),
                new MenuItem(HotkeysMediator.QuickHideUnselected.ToString(), MenuTextures["Menu_HideUnselected"]),
                new MenuItem(HotkeysMediator.QuickHideShowAll.ToString(), MenuTextures["Menu_ShowHidden"])));
            Menus.Add(new Menu("Tools",
                new MenuItem(HotkeysMediator.Carve.ToString(), MenuTextures["Menu_Carve"]),
                new MenuItem(HotkeysMediator.MakeHollow.ToString(), MenuTextures["Menu_Hollow"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.GroupingGroup.ToString(), MenuTextures["Menu_Group"]),
                new MenuItem(HotkeysMediator.GroupingUngroup.ToString(), MenuTextures["Menu_Ungroup"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.TieToEntity.ToString(), MenuTextures["Menu_TieToEntity"]),
                new MenuItem(HotkeysMediator.TieToWorld.ToString(), MenuTextures["Menu_TieToWorld"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.ReplaceTextures.ToString(), MenuTextures["Menu_ReplaceTextures"]),
                new MenuSeparator(),
                new MenuItem(HotkeysMediator.Transform.ToString(), MenuTextures["Menu_Transform"]),
                new MenuItem(HotkeysMediator.SnapSelectionToGrid.ToString(), MenuTextures["Menu_SnapSelection"]),
                new MenuItem(HotkeysMediator.SnapSelectionToGridIndividually.ToString(), MenuTextures["Menu_SnapSelectionIndividual"]),
                new Menu("Align Objects", MenuTextures["Menu_Align"],
                    new MenuItem(HotkeysMediator.AlignXMin.ToString()),
                    new MenuItem(HotkeysMediator.AlignXMax.ToString()),
                    new MenuItem(HotkeysMediator.AlignYMin.ToString()),
                    new MenuItem(HotkeysMediator.AlignYMax.ToString()),
                    new MenuItem(HotkeysMediator.AlignZMin.ToString()),
                    new MenuItem(HotkeysMediator.AlignZMax.ToString())),
                new Menu("Flip Objects", MenuTextures["Menu_Flip"],
                    new MenuItem(HotkeysMediator.FlipX.ToString()),
                    new MenuItem(HotkeysMediator.FlipY.ToString()),
                    new MenuItem(HotkeysMediator.FlipZ.ToString())),
                new MenuSeparator(),
                new MenuItem("Options...", "", MenuTextures["Menu_Options"], action: Options)));
            Menus.Add(new Menu("Layout",
                new MenuItem("Create New Layout Window", "", MenuTextures["Menu_NewWindow"]),
                new MenuItem("Layout Window Settings...", "", MenuTextures["Menu_WindowSettings"])));
            Menus.Add(new Menu("Window",
                new MenuItem("Document View", "", MenuTextures["Menu_NewWindow"], action: () => new DocumentTabs()),
                new MenuItem("Viewport 0 - 3d", "", MenuTextures["Menu_NewWindow"], action: () => new ViewportWindow(0)),
                new MenuItem("Viewport 1 - Top", "", MenuTextures["Menu_NewWindow"], action: () => new ViewportWindow(1)),
                new MenuItem("Viewport 2 - Side", "", MenuTextures["Menu_NewWindow"], action: () => new ViewportWindow(2)),
                new MenuItem("Viewport 3 - Front", "", MenuTextures["Menu_NewWindow"], action: () => new ViewportWindow(3)),
                new MenuItem("Tool Properties", "", MenuTextures["Menu_NewWindow"], action: () => new ToolPropsWindow()),
                new MenuItem("Stats View", "", MenuTextures["Menu_NewWindow"], action: () => new StatsWindow()),
                new MenuItem("Tools", "", MenuTextures["Menu_NewWindow"], action: () => new ToolsWindow())));
            Menus.Add(new Menu("Help",
                new MenuItem("About...", "", action: About)));
        }

        private void UpdateMenus() {
            // if (ImGui.BeginMenuBar()) {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Num.Vector2.One * 8);
                for (int i = 0; i < Menus.Count; i++) {
                    Menus[i].Draw(true);
                }
                ImGui.PopStyleVar();
                // ImGui.EndMenuBar();
            // }
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

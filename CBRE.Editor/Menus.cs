using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<Menu> Menus = new List<Menu>();

        private void InitMenus() {
            Menus.Add(new Menu("File",
                new MenuItem("New", "Ctrl+N", MenuTextures["Menu_New"], action: Top_New),
                new MenuItem("Open", "Ctrl+O", MenuTextures["Menu_Open"], action: Top_Open),
                new MenuItem("Close", "", MenuTextures["Menu_Close"], action: Top_Close),
                new MenuItem("Save", "Ctrl+S", MenuTextures["Menu_Save"], action: Top_Save),
                new MenuItem("Save as", "Ctrl+Shift+S", MenuTextures["Menu_SaveAs"], action: Top_Save),
                new MenuSeparator(),
                new MenuItem("Export / Lightmap", "F9", MenuTextures["Menu_ExportRmesh"]),
                new MenuSeparator(),
                new MenuItem("Exit", "", action: Exit)));
            Menus.Add(new Menu("Edit",
                new MenuItem("Undo", "Ctrl+Z", MenuTextures["Menu_Undo"]),
                new MenuItem("Redo", "Ctrl+Y", MenuTextures["Menu_Redo"]),
                new MenuSeparator(),
                new MenuItem("Cut", "Ctrl+X", MenuTextures["Menu_Cut"]),
                new MenuItem("Copy", "Ctrl+C", MenuTextures["Menu_Copy"]),
                new MenuItem("Paste", "Ctrl+V", MenuTextures["Menu_Paste"]),
                new MenuItem("Paste Special...", "", MenuTextures["Menu_PasteSpecial"]),
                new MenuItem("Delete", "Del", MenuTextures["Menu_Delete"]),
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
                new MenuItem("Map Properties...", "", MenuTextures["Menu_MapProperties"])));
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
                new MenuItem("Options...", "", MenuTextures["Menu_Options"])));
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
            public MenuItem(string name, Keys shortcut, AsyncTexture texture = null, Action action = null) {
                Name = name;
                ShortcutKey = shortcut;
                Shortcut = string.Join("+", Shortcut.Select(p => p.ToString()));
                Texture = texture;
                Action = action;
            }

            public MenuItem(string name, string shortcut, AsyncTexture texture = null, Action action = null) {
                Name = name;
                string[] keys = shortcut.Split("+");
                for (int i = 0; i < keys.Length; i++) {
                    if (keys[i].ToLower() == "ctrl") {
                        Ctrl = true;
                    }
                    else if (keys[i].ToLower() == "shift") {
                        Shift = true;
                    }
                    else if (keys[i].ToLower() == "alt") {
                        Alt = true;
                    }
                    else {
                        ShortcutKey = Enum.TryParse<Keys>(keys[i], true, out Keys res) ? res : Keys.None;
                        Console.WriteLine(ShortcutKey.ToString());
                    }
                }
                Shortcut = shortcut;
                Texture = texture;
                Action = action;
            }

            public virtual void Draw(bool topLevel) {
                Num.Vector2 pos = ImGui.GetCursorPos() + ImGui.GetWindowPos();
                if (ImGui.MenuItem(GetDrawnText(topLevel), Shortcut)) {
                    Action?.Invoke();
                }
                RenderIcon(pos);
            }

            public virtual void Update() {
                Keys[] keys = Keyboard.GetState().GetPressedKeys();
                List<Keys> pressed = new List<Keys>();
                foreach (var key in keys) {
                    if (!previousKeys.Contains(key)) {
                        Console.WriteLine(key.ToString());
                        pressed.Add(key);
                    }
                }
                bool ctrlpressed = keys.Contains(Keys.LeftControl) || keys.Contains(Keys.RightControl);
                bool shiftpressed = keys.Contains(Keys.LeftShift) || keys.Contains(Keys.RightShift);
                bool altpressed = keys.Contains(Keys.LeftAlt) || keys.Contains(Keys.RightAlt);
                if (pressed.Contains(ShortcutKey) && ctrlpressed == Ctrl && shiftpressed == Shift && altpressed == Alt) {
                    Action?.Invoke();
                }
                previousKeys = keys;
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
            public Keys[] previousKeys = new Keys[0];

            public string Name;
            public string Shortcut;
            public Keys ShortcutKey;
            public bool Ctrl = false;
            public bool Shift = false;
            public bool Alt = false;
            public AsyncTexture Texture;
        }

        public class Menu : MenuItem {
            public Menu(string name, AsyncTexture texture, params MenuItem[] items) : base(name, "", texture) {
                Items = items.ToList();
            }

            public Menu(string name, params MenuItem[] items) : this(name, null, items) { }

            public override void Update() {
                for (int i = 0; i < Items.Count; i++) {
                    Items[i].Update();
                }
            }

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

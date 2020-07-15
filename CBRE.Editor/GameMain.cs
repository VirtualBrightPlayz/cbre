using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Num = System.Numerics;

namespace CBRE.Editor {
    public class GameMain : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        public GameMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;

            Menus.Add(new Menu("File",
                new Menu.Item("New", "Ctrl+N"),
                new Menu.Item("Open", "Ctrl+O"),
                new Menu.Item("Close", ""),
                new Menu.Item("Save", "Ctrl+S"),
                new Menu.Item("Save as", "Ctrl+Shift+S"),
                new Menu.Separator(),
                new Menu.Item("Export / Lightmap", "F9"),
                new Menu.Separator(),
                new Menu.Item("Exit", "")));
            Menus.Add(new Menu("Edit",
                new Menu.Item("Undo", "Ctrl+Z"),
                new Menu.Item("Redo", "Ctrl+Y"),
                new Menu.Separator(),
                new Menu.Item("Cut", "Ctrl+X"),
                new Menu.Item("Copy", "Ctrl+C"),
                new Menu.Item("Paste", "Ctrl+V"),
                new Menu.Item("Paste Special...", ""),
                new Menu.Item("Delete", "Del"),
                new Menu.Separator(),
                new Menu.Item("Clear Selection", ""),
                new Menu.Item("Select All", "Ctrl+A"),
                new Menu.Separator(),
                new Menu.Item("Object Properties", "Alt+Enter")));
            Menus.Add(new Menu("Map",
                new Menu.Item("Snap to Grid", ""),
                new Menu.Item("Show 2D Grid", ""),
                new Menu.Item("Show 3D Grid", ""),
                new Menu.Item("Grid Settings", "",
                    new Menu.Item("Smaller Grid", ""),
                    new Menu.Item("Bigger Grid", "")),
                new Menu.Separator(),
                new Menu.Item("Ignore Grouping", ""),
                new Menu.Separator(),
                new Menu.Item("Texture Lock", ""),
                new Menu.Item("Texture Scaling Lock", ""),
                new Menu.Separator(),
                new Menu.Item("Hide Null Textures", ""),
                new Menu.Separator(),
                new Menu.Item("Show Information", ""),
                new Menu.Item("Show Selected Brush ID", ""),
                new Menu.Item("Entity Report...", ""),
                new Menu.Item("Check for Problems", ""),
                new Menu.Item("Show Logical Tree", ""),
                new Menu.Separator(),
                new Menu.Item("Map Properties...", "")));
            Menus.Add(new Menu("View",
                new Menu.Item("Autosize All Views", ""),
                new Menu.Item("Center All Views on Selection", ""),
                new Menu.Item("Center 2D Views on Selection", ""),
                new Menu.Item("Center 3D View on Selection", ""),
                new Menu.Separator(),
                new Menu.Item("Go to Brush ID...", ""),
                new Menu.Item("Go to Coordinates...", ""),
                new Menu.Separator(),
                new Menu.Item("Hide Selected Objects", ""),
                new Menu.Item("Hide Unselected Objects", ""),
                new Menu.Item("Show Hidden Objects", "")));
            Menus.Add(new Menu("Tools",
                new Menu.Item("Carve", ""),
                new Menu.Item("Make Hollow", ""),
                new Menu.Separator(),
                new Menu.Item("Group", ""),
                new Menu.Item("Ungroup", ""),
                new Menu.Separator(),
                new Menu.Item("Tie to Entity", ""),
                new Menu.Item("Move to World", ""),
                new Menu.Separator(),
                new Menu.Item("Replace Textures", ""),
                new Menu.Separator(),
                new Menu.Item("Transform...", ""),
                new Menu.Item("Snap Selected to Grid", ""),
                new Menu.Item("Snap Selected to Grid Individually", ""),
                new Menu.Item("Align Objects", "",
                    new Menu.Item("To X Axis Min", ""),
                    new Menu.Item("To X Axis Max", ""),
                    new Menu.Item("To Y Axis Min", ""),
                    new Menu.Item("To Y Axis Max", ""),
                    new Menu.Item("To Z Axis Min", ""),
                    new Menu.Item("To Z Axis Max", "")),
                new Menu.Item("Flip Objects", "",
                    new Menu.Item("X Axis", ""),
                    new Menu.Item("Y Axis", ""),
                    new Menu.Item("Z Axis", "")),
                new Menu.Separator(),
                new Menu.Item("Options...", "")));
            Menus.Add(new Menu("Layout",
                new Menu.Item("Create New Layout Window", ""),
                new Menu.Item("Layout Window Settings...", "")));
            Menus.Add(new Menu("Help",
                new Menu.Item("About...", "")));

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            var style = ImGui.GetStyle();
            style.ChildRounding = 0;
            style.FrameRounding = 0;
            style.GrabRounding = 0;
            style.PopupRounding = 0;
            style.ScrollbarRounding = 0;
            style.TabRounding = 0;
            style.WindowRounding = 0;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkSlateGray);

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        public class Menu {
            public Menu(string name, params Item[] items) {
                Name = name;
                Items = items.ToList();
            }

            public string Name;
            public List<Item> Items;

            public class Item {
                public Item(string name, string shortcut, params Item[] subItems) {
                    Name = name;
                    Shortcut = shortcut;
                    if (subItems != null && subItems.Any()) {
                        SubItems = subItems.ToList();
                    } else {
                        SubItems = null;
                    }
                }

                public virtual void Update() {
                    if (SubItems?.Any() ?? false) {
                        if (ImGui.BeginMenu(Name)) {
                            SubItems.ForEach(it => it.Update());
                            ImGui.EndMenu();
                        }
                    } else {
                        if (ImGui.MenuItem(Name, Shortcut)) {
                            Action?.Invoke();
                        }
                    }
                }

                public Action Action;

                public string Name;
                public string Shortcut;

                public List<Item> SubItems;
            }

            public class Separator : Item {
                public Separator() : base("","") { }

                public override void Update() {
                    ImGui.Separator();
                }
            }
        }
        public List<Menu> Menus = new List<Menu>();

        protected virtual void ImGuiLayout()
        {
            if (ImGui.Begin("main", ImGuiWindowFlags.NoBackground |
                                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoDecoration |
                                    ImGuiWindowFlags.MenuBar)) {
                ImGui.SetWindowPos(new Num.Vector2(-1,0));
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width+2, Window.ClientBounds.Height));
                if (ImGui.BeginMenuBar()) {
                    for (int i = 0; i < Menus.Count; i++) {
                        if (ImGui.BeginMenu(Menus[i].Name)) {
                            for (int j = 0; j < Menus[i].Items.Count; j++) {
                                Menus[i].Items[j].Update();
                            }
                            ImGui.EndMenu();
                        }
                    }

                    ImGui.EndMenuBar();
                }
                ImGui.End();
            }

        }
	}
}

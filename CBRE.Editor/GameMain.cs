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
                new MenuItem("New", "Ctrl+N"),
                new MenuItem("Open", "Ctrl+O"),
                new MenuItem("Close", ""),
                new MenuItem("Save", "Ctrl+S"),
                new MenuItem("Save as", "Ctrl+Shift+S"),
                new Separator(),
                new MenuItem("Export / Lightmap", "F9"),
                new Separator(),
                new MenuItem("Exit", "") { Action = () => { Exit(); } }));
            Menus.Add(new Menu("Edit",
                new MenuItem("Undo", "Ctrl+Z"),
                new MenuItem("Redo", "Ctrl+Y"),
                new Separator(),
                new MenuItem("Cut", "Ctrl+X"),
                new MenuItem("Copy", "Ctrl+C"),
                new MenuItem("Paste", "Ctrl+V"),
                new MenuItem("Paste Special...", ""),
                new MenuItem("Delete", "Del"),
                new Separator(),
                new MenuItem("Clear Selection", ""),
                new MenuItem("Select All", "Ctrl+A"),
                new Separator(),
                new MenuItem("Object Properties", "Alt+Enter")));
            Menus.Add(new Menu("Map",
                new MenuItem("Snap to Grid", ""),
                new MenuItem("Show 2D Grid", ""),
                new MenuItem("Show 3D Grid", ""),
                new Menu("Grid Settings",
                    new MenuItem("Smaller Grid", ""),
                    new MenuItem("Bigger Grid", "")),
                new Separator(),
                new MenuItem("Ignore Grouping", ""),
                new Separator(),
                new MenuItem("Texture Lock", ""),
                new MenuItem("Texture Scaling Lock", ""),
                new Separator(),
                new MenuItem("Hide Null Textures", ""),
                new Separator(),
                new MenuItem("Show Information", ""),
                new MenuItem("Show Selected Brush ID", ""),
                new MenuItem("Entity Report...", ""),
                new MenuItem("Check for Problems", ""),
                new MenuItem("Show Logical Tree", ""),
                new Separator(),
                new MenuItem("Map Properties...", "")));
            Menus.Add(new Menu("View",
                new MenuItem("Autosize All Views", ""),
                new MenuItem("Center All Views on Selection", ""),
                new MenuItem("Center 2D Views on Selection", ""),
                new MenuItem("Center 3D View on Selection", ""),
                new Separator(),
                new MenuItem("Go to Brush ID...", ""),
                new MenuItem("Go to Coordinates...", ""),
                new Separator(),
                new MenuItem("Hide Selected Objects", ""),
                new MenuItem("Hide Unselected Objects", ""),
                new MenuItem("Show Hidden Objects", "")));
            Menus.Add(new Menu("Tools",
                new MenuItem("Carve", ""),
                new MenuItem("Make Hollow", ""),
                new Separator(),
                new MenuItem("Group", ""),
                new MenuItem("Ungroup", ""),
                new Separator(),
                new MenuItem("Tie to Entity", ""),
                new MenuItem("Move to World", ""),
                new Separator(),
                new MenuItem("Replace Textures", ""),
                new Separator(),
                new MenuItem("Transform...", ""),
                new MenuItem("Snap Selected to Grid", ""),
                new MenuItem("Snap Selected to Grid Individually", ""),
                new Menu("Align Objects",
                    new MenuItem("To X Axis Min", ""),
                    new MenuItem("To X Axis Max", ""),
                    new MenuItem("To Y Axis Min", ""),
                    new MenuItem("To Y Axis Max", ""),
                    new MenuItem("To Z Axis Min", ""),
                    new MenuItem("To Z Axis Max", "")),
                new Menu("Flip Objects",
                    new MenuItem("X Axis", ""),
                    new MenuItem("Y Axis", ""),
                    new MenuItem("Z Axis", "")),
                new Separator(),
                new MenuItem("Options...", "")));
            Menus.Add(new Menu("Layout",
                new MenuItem("Create New Layout Window", ""),
                new MenuItem("Layout Window Settings...", "")));
            Menus.Add(new Menu("Help",
                new MenuItem("About...", "")));

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

        public class MenuItem {
            public MenuItem(string name, string shortcut) {
                Name = name;
                Shortcut = shortcut;
            }

            public virtual void Update() {
                if (ImGui.MenuItem(Name, Shortcut)) {
                    Action?.Invoke();
                }
            }

            public Action Action;

            public string Name;
            public string Shortcut;
        }

        public class Menu : MenuItem {
            public Menu(string name, params MenuItem[] items) : base(name, "") {
                Items = items.ToList();
            }

            public override void Update() {
                if (ImGui.BeginMenu(Name)) {
                    Items.ForEach(it => it.Update());
                    ImGui.EndMenu();
                }
            }
            public List<MenuItem> Items;
        }

        public class Separator : MenuItem {
            public Separator() : base("", "") { }

            public override void Update() {
                ImGui.Separator();
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
                        Menus[i].Update();
                    }

                    ImGui.EndMenuBar();
                }
                ImGui.End();
            }

        }
	}
}

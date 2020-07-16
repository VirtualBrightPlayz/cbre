using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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

            IsMouseVisible = true;
        }

        Dictionary<string, AsyncTexture> menuTextures;

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

            InitMenus();

            base.Initialize();
        }

        private void InitMenus() {
            menuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                menuTextures.Add(Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            Menus.Add(new Menu("File",
                new MenuItem("New", "Ctrl+N", menuTextures["Menu_New"]),
                new MenuItem("Open", "Ctrl+O", menuTextures["Menu_Open"]),
                new MenuItem("Close", "", menuTextures["Menu_Close"]),
                new MenuItem("Save", "Ctrl+S", menuTextures["Menu_Save"]),
                new MenuItem("Save as", "Ctrl+Shift+S", menuTextures["Menu_SaveAs"]),
                new Separator(),
                new MenuItem("Export / Lightmap", "F9", menuTextures["Menu_ExportRmesh"]),
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
        }

        private AsyncTexture LoadTexture(string filename) {
            return new AsyncTexture(GraphicsDevice, _imGuiRenderer, filename);
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
                    ImGui.GetForegroundDrawList().AddImage(Texture.ImGuiTexture, pos + new Num.Vector2(-2,-2), pos + new Num.Vector2(14, 14), Num.Vector2.Zero, Num.Vector2.One, 0xffffffff);
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

        public class Separator : MenuItem {
            public Separator() : base("", "", null) { }

            public override void Draw(bool topLevel) {
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
                        Menus[i].Draw(true);
                    }

                    ImGui.EndMenuBar();
                }
                ImGui.End();
            }

        }
	}
}

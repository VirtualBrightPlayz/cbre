using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain : Game
    {
        public static GameMain Instance { get; private set; }

        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        private AsyncTexture rotateCursorTexture;
        private MouseCursor rotateCursor;
        public MouseCursor RotateCursor {
            get {
                if (rotateCursor != null) { return rotateCursor; }
                if (rotateCursorTexture?.MonoGameTexture != null) {
                    rotateCursor = MouseCursor.FromTexture2D(rotateCursorTexture.MonoGameTexture, 8, 8);
                    return rotateCursor;
                }
                return MouseCursor.Arrow;
            }
        }

        public bool PopupSelected { get; set; } = false;

        public List<PopupUI> Popups { get; private set; } = new List<PopupUI>();

        public GameMain()
        {
            Instance = this;

            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;

            IsMouseVisible = true;
        }

        public static Dictionary<string, AsyncTexture> MenuTextures;

        ImGuiStylePtr ImGuiStyle;

        protected override void Initialize()
        {
            SettingsManager.Read();
            ToolManager.Init();

            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            GlobalGraphics.Set(GraphicsDevice, Window, _imGuiRenderer);

            ImGuiStyle = ImGui.GetStyle();
            ImGuiStyle.ChildRounding = 0;
            ImGuiStyle.FrameRounding = 0;
            ImGuiStyle.GrabRounding = 0;
            ImGuiStyle.PopupRounding = 0;
            ImGuiStyle.ScrollbarRounding = 0;
            ImGuiStyle.TabRounding = 0;
            ImGuiStyle.WindowRounding = 0;
            ImGuiStyle.FrameBorderSize = 0;
            ImGuiStyle.DisplayWindowPadding = Num.Vector2.Zero;
            ImGuiStyle.WindowPadding = Num.Vector2.Zero;
            ImGuiStyle.IndentSpacing = 0;
            var colors = ImGuiStyle.Colors;
            colors[(int)ImGuiCol.FrameBg] = new Num.Vector4(0.05f, 0.05f, 0.07f, 1.0f);

            MenuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                if (!System.IO.Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase)) { continue; }
                MenuTextures.Add(System.IO.Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            InitMenus();
            InitTopBar();
            InitToolBar();

            TextureProvider.CreatePackages(Directories.GetTextureCategories());

            MapProvider.Register(new VmfProvider());
            MapProvider.Register(new RmfProvider());
            MapProvider.Register(new MapFormatProvider());
            MapProvider.Register(new L3DWProvider());

            DocumentManager.AddAndSwitch(new Document("new", new DataStructures.MapObjects.Map()));
            /*Map map = MapProvider.GetMapFromFile("gateA.3dw");
            Map map2 = MapProvider.GetMapFromFile("room2_2.3dw");
            DocumentManager.AddAndSwitch(new Document("gateA.3dw", map));
            DocumentManager.Add(new Document("room2_2.3dw", map2));*/

            ViewportManager.Init();

            base.Initialize();
        }

        private AsyncTexture LoadTexture(string filename) {
            return new AsyncTexture(filename);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        private Timing timing = new Timing();

        protected override void Update(GameTime gameTime) {
            timing.StartMeasurement();
            base.Update(gameTime);

            if (PopupSelected && Popups.Count == 0)
                PopupSelected = false;

            if (!PopupSelected)
                timing.PerformTicks(ViewportManager.Update);
            timing.EndMeasurement();
        }

        protected override void Draw(GameTime gameTime)
        {
            GlobalGraphics.GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);

            TaskPool.Update();

            GraphicsDevice.Clear(new Color(50, 50, 60));

            ViewportManager.DrawRenderTarget();

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        protected virtual void ImGuiLayout() {
            if (ImGui.Begin("top", ImGuiWindowFlags.NoBackground |
                                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoDecoration |
                                    ImGuiWindowFlags.MenuBar |
                                    ImGuiWindowFlags.NoScrollbar |
                                    ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(-1, 0));
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width + 2, 47));

                ViewportManager.TopMenuOpen = false;
                UpdateMenus();
                UpdateTopBar();

                ImGui.End();
            }

            if (ImGui.Begin("tools", ImGuiWindowFlags.NoBackground |
                                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoDecoration |
                                    ImGuiWindowFlags.MenuBar |
                                    ImGuiWindowFlags.NoScrollbar |
                                    ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(-1, 27));
                ImGui.SetWindowSize(new Num.Vector2(47, Window.ClientBounds.Height - 27));

                UpdateToolBar();

                ImGui.End();
            }
            
            if (ImGui.Begin("tabber", ImGuiWindowFlags.NoBackground |
                                      ImGuiWindowFlags.NoBringToFrontOnFocus |
                                      ImGuiWindowFlags.NoMove |
                                      ImGuiWindowFlags.NoDecoration |
                                      ImGuiWindowFlags.NoScrollbar |
                                      ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(47, 47));
                ImGui.SetWindowSize(new Num.Vector2(ViewportManager.Right - 47, 30));
                ImGui.BeginTabBar("doc_tabber");
                for (int i = 0; i < DocumentManager.Documents.Count; i++) {
                    Document doc = DocumentManager.Documents[i];
                    if (ImGui.BeginTabItem(doc.MapFileName)) {
                        if (DocumentManager.CurrentDocument != doc) {
                            DocumentManager.SwitchTo(doc);
                        }
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }

            if (ImGui.Begin("tool_properties", ImGuiWindowFlags.NoBackground |
                                               ImGuiWindowFlags.NoBringToFrontOnFocus |
                                               ImGuiWindowFlags.NoMove |
                                               ImGuiWindowFlags.NoDecoration |
                                               ImGuiWindowFlags.NoScrollbar |
                                               ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.Right, 47));
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, Window.ClientBounds.Height - 47 - 60));
                if (ImGui.BeginChildFrame(3, new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, Window.ClientBounds.Height - 47 - 60))) {
                    SelectedTool?.UpdateGui();

                    ImGui.EndChildFrame();
                }
                ImGui.End();
            }

            if (ImGui.Begin("stats", ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus |
                                     ImGuiWindowFlags.NoMove |
                                     ImGuiWindowFlags.NoDecoration |
                                     ImGuiWindowFlags.NoScrollbar |
                                     ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(ViewportManager.Right, Window.ClientBounds.Height - 60));
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, 60));
                if (ImGui.BeginChildFrame(3, new Num.Vector2(Window.ClientBounds.Width - ViewportManager.Right, 60))) {
                    Process proc = Process.GetCurrentProcess();

                    proc.Refresh();
                    ImGui.Text($"Working set: {proc.WorkingSet64 / 1024 / 1024} MB");
                    ImGui.Text($"Private mem: {proc.PrivateMemorySize64 / 1024 / 1024} MB");
                    ImGui.Text($"Paged mem: {proc.PagedMemorySize64 / 1024 / 1024} MB");
                }
                ImGui.End();
            }

            for (int i = 0; i < Popups.Count; i++)
            {
                if (!Popups[i].Draw())
                {
                    Popups[i].Close();
                    i--;
                }
            }
        }
	}
}

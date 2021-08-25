using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Model;
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
        public GameTime LastTime { get; set; }

        public List<PopupUI> Popups { get; private set; } = new List<PopupUI>();
        public Queue<Action> PreDrawActions { get; private set; } = new Queue<Action>();

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
            ImGuiStyle.TabMinWidthForCloseButton = 50f;
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

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            MenuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                if (!System.IO.Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase)) { continue; }
                MenuTextures.Add(System.IO.Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            Mediator.MediatorException += MediatorError;
            InitMenus();
            InitTopBar();
            InitToolBar();
            Subscribe();

            TextureProvider.CreatePackages(Directories.GetTextureCategories());

            MapProvider.Register(new VmfProvider());
            MapProvider.Register(new RmfProvider());
            MapProvider.Register(new MapFormatProvider());
            MapProvider.Register(new L3DWProvider());

            ModelProvider.Register(new AssimpProvider());

            /*Map map = MapProvider.GetMapFromFile("gateA.3dw");
            Map map2 = MapProvider.GetMapFromFile("room2_2.3dw");
            DocumentManager.AddAndSwitch(new Document("gateA.3dw", map));
            DocumentManager.Add(new Document("room2_2.3dw", map2));*/

            ViewportManager.Init();
            DocumentManager.AddAndSwitch(new Document(Document.NewDocumentName, new DataStructures.MapObjects.Map()));

            // Initial windows
            new ToolsWindow();
            new DocumentTabWindow();
            new ToolPropsWindow();
            new StatsWindow();
            new ViewportWindow(0);
            new ViewportWindow(1);
            new ViewportWindow(2);
            new ViewportWindow(3);
            new VisgroupsWindow();

            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args) {
            SettingsManager.Write();
            base.OnExiting(sender, args);
        }

        private AsyncTexture LoadTexture(string filename) {
            return new AsyncTexture(filename);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        private Timing timing = new Timing();
        private Keys[] previousKeys = new Keys[0];

        protected override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (PopupSelected && !Popups.Any(p => !(p is WindowUI)))
                PopupSelected = false;

            if (!PopupSelected) {
                // Hotkeys
                {
                    Keys[] keys = Keyboard.GetState().GetPressedKeys();
                    List<Keys> pressed = new List<Keys>();
                    foreach (var key in keys) {
                        if (!previousKeys.Contains(key)) {
                            pressed.Add(key);
                        }
                    }
                    bool ctrlpressed = keys.Contains(Keys.LeftControl) || keys.Contains(Keys.RightControl);
                    bool shiftpressed = keys.Contains(Keys.LeftShift) || keys.Contains(Keys.RightShift);
                    bool altpressed = keys.Contains(Keys.LeftAlt) || keys.Contains(Keys.RightAlt);
                    HotkeyImplementation def = Hotkeys.GetHotkeyFor(pressed.ToArray(), ctrlpressed, shiftpressed, altpressed);
                    if (def != null) {
                        Mediator.Publish(def.Definition.Action, def.Definition.Parameter);
                    }
                    previousKeys = keys;
                }
            }

            timing.StartMeasurement();
            timing.PerformTicks(ViewportManager.Update);
            timing.EndMeasurement();

            LastTime = gameTime;
        }

        protected override void Draw(GameTime gameTime)
        {
            GlobalGraphics.GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);

            TaskPool.Update();

            GraphicsDevice.Clear(new Color(50, 50, 60));

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);

            while (PreDrawActions.Count > 0) {
                try {
                    PreDrawActions.Dequeue()?.Invoke();
                } catch (Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
        }

        protected virtual void ImGuiLayout() {
            uint dockId = ImGui.GetID("Dock");
            ImGuiViewportPtr viewportPtr = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewportPtr.Pos);
            ImGui.SetNextWindowSize(viewportPtr.Size);
            ImGui.Begin("Main Window", ImGuiWindowFlags.NoMove |
                                        ImGuiWindowFlags.NoResize |
                                        ImGuiWindowFlags.NoBringToFrontOnFocus |
                                        ImGuiWindowFlags.NoCollapse);
            ImGui.DockSpace(dockId);
            ImGui.End();
            if (ImGui.BeginMainMenuBar()) {
                ViewportManager.TopMenuOpen = false;
                UpdateMenus();
                // UpdateTopBar();
            }
            ImGui.EndMainMenuBar();

            for (int i = 0; i < Popups.Count; i++)
            {
                ImGui.SetNextWindowDockID(dockId, ImGuiCond.FirstUseEver);
                if (!Popups[i].Draw())
                {
                    Popups[i].Close();
                    i--;
                }
            }
        }
	}
}

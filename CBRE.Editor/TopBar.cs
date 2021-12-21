using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Models;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Tools;
using CBRE.Editor.Tools.SelectTool;
using CBRE.Graphics;
using CBRE.Settings;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain {
        public List<TopBarItem> TopBarItems;

        private void InitTopBar() {
            TopBarItems = new List<TopBarItem>();

            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_New"], HotkeysMediator.FileNew.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Open"], HotkeysMediator.FileOpen.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Close"], HotkeysMediator.FileClose.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Save"], HotkeysMediator.FileSave.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_ExportRmesh"], HotkeysMediator.FileCompile.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Undo"], HotkeysMediator.HistoryUndo.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Redo"], HotkeysMediator.HistoryRedo.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Cut"], HotkeysMediator.OperationsCut.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Copy"], HotkeysMediator.OperationsCopy.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Paste"], HotkeysMediator.OperationsPaste.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_PasteSpecial"], HotkeysMediator.OperationsPasteSpecial.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Delete"], HotkeysMediator.OperationsDelete.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_ObjectProperties"], HotkeysMediator.ObjectProperties.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Snap To Grid", MenuTextures["Menu_SnapToGrid"]));
            TopBarItems.Add(new TopBarItem("Show 2D Grid", MenuTextures["Menu_Show2DGrid"]));
            TopBarItems.Add(new TopBarItem("Show 3D Grid", MenuTextures["Menu_Show3DGrid"]));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_SmallerGrid"], HotkeysMediator.GridDecrease.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_LargerGrid"], HotkeysMediator.GridIncrease.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_IgnoreGrouping"], HotkeysMediator.ToggleIgnoreGrouping.ToString(), isToggle: true));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_TextureLock"], HotkeysMediator.ToggleTextureLock.ToString(), isToggle: true));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_TextureScalingLock"], HotkeysMediator.ToggleTextureScalingLock.ToString(), isToggle: true));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_HideNullTextures"], HotkeysMediator.ToggleHideNullTextures.ToString(), isToggle: true));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_HideSelected"], HotkeysMediator.QuickHideSelected.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_HideUnselected"], HotkeysMediator.QuickHideUnselected.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_ShowHidden"], HotkeysMediator.QuickHideShowAll.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Carve"], HotkeysMediator.Carve.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Hollow"], HotkeysMediator.MakeHollow.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Group"], HotkeysMediator.GroupingGroup.ToString()));
            TopBarItems.Add(new TopBarItem(MenuTextures["Menu_Ungroup"], HotkeysMediator.GroupingUngroup.ToString()));
            TopBarItems.Add(new TopBarSeparator());
            TopBarItems.Add(new TopBarItem("Options", MenuTextures["Menu_Options"], action: Options));
        }

        public const int TopBarHeight = 28;
        private void UpdateTopBar() {
            ImGui.SetCursorPos(new Num.Vector2(0, 19));

            if (ImGui.BeginChildFrame(1, new Num.Vector2(Window.ClientBounds.Width + 1, TopBarHeight))) {
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

            public TopBarItem(AsyncTexture texture, string hotkey, bool isToggle = false) {
                var h = Hotkeys.GetHotkeyDefinitions().FirstOrDefault(p => p.ID == hotkey);
                IsToggle = isToggle;
                Texture = texture;
                if (h == null) {
                    ToolTip = hotkey;
                    return;
                }
                Action = () => {
                    Mediator.Publish(h.Action, h.Parameter);
                };
            }

            public virtual void Draw() {
                using var _ = new ColorPush(ImGuiCol.Button, Toggled ? GlobalGraphics.SelectedColors.Button : null);
                using var __ = new ColorPush(ImGuiCol.ButtonActive, Toggled ? GlobalGraphics.SelectedColors.ButtonActive : null);
                using var ___ = new ColorPush(ImGuiCol.ButtonHovered, Toggled ? GlobalGraphics.SelectedColors.ButtonHovered : null);

                bool pressed;
                if (Texture.ImGuiTexture != IntPtr.Zero) {
                    pressed = ImGui.ImageButton(Texture.ImGuiTexture, new Num.Vector2(16, 16));
                } else {
                    pressed = ImGui.Button($"##{Texture.Name}", new Num.Vector2(24, 22));
                }
                ImGui.SameLine();

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

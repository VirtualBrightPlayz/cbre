using System;
using System.Collections.Generic;
using System.Linq;
using CBRE.Editor.Tools.TextureTool;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class TexturePopupUI : PopupUI {
        protected override bool canBeDefocused => false;
        
        private List<TextureTool.TextureData> textureList = new List<TextureTool.TextureData>();
        private Action<TextureTool.TextureData> _callback;
        private string namefilter = "";
        public TexturePopupUI(Action<TextureTool.TextureData> textureCallback) : base("Texture Application Tool") {
            foreach (var (_, value) in TextureProvider.Packages.SelectMany(item1 => item1.Items)) {
                textureList.Add(new TextureTool.TextureData(value.Texture as AsyncTexture, value));
            }

            GameMain.Instance.PopupSelected = true;
            _callback = textureCallback;
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Button("Close")) {
                return false;
            }
            ImGui.InputText("Search", ref namefilter, 255);
            ImGui.NewLine();
            if (ImGui.BeginChild("TextureSelect")) {
                for (int i = 0; i < textureList.Count; i++) {
                    if (string.IsNullOrWhiteSpace(namefilter) 
                        || textureList[i].Texture.Name.Contains(namefilter, StringComparison.OrdinalIgnoreCase)
                        || namefilter.Contains(textureList[i].Texture.Name, StringComparison.OrdinalIgnoreCase)) {

                        var cursorPos = ImGui.GetCursorScreenPos();
                        if (ImGui.Button($"##TextureBox_{i}", new Num.Vector2(200, 200))) {
                            _callback?.Invoke(textureList[i]);
                            return false;
                        }

                        var drawList = ImGui.GetWindowDrawList();
                        var texture = textureList[i].AsyncTexture.ImGuiTexture;
                        if (texture != IntPtr.Zero) {
                            drawList.AddImage(texture,
                                cursorPos + new Num.Vector2(25, 15),
                                cursorPos + new Num.Vector2(175, 165));
                        }
                        drawList.PushClipRectButItDoesntSuckAss(
                            cursorPos + new Num.Vector2(5, 170),
                            cursorPos + new Num.Vector2(195, 190));
                        drawList.AddText(cursorPos + new Num.Vector2(11, 176), 0xff000000, textureList[i].Texture.Name);
                        drawList.AddText(cursorPos + new Num.Vector2(10, 175), 0xffffffff, textureList[i].Texture.Name);
                        drawList.PopClipRect();
                        
                        ImGui.SameLine();
                        int cursorX = (int)ImGui.GetCursorPosX();
                        int windowWidth = (int)ImGui.GetWindowWidth();
                        if (cursorX + 200 > windowWidth) {
                            ImGui.NewLine();
                        }
                    }
                }

                ImGui.EndChild();
            }

            return true;
        }
    }
}

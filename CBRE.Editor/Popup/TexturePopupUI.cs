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
        protected override bool hasOkButton => false;
        
        private readonly List<TextureTool.TextureData> textureList = new List<TextureTool.TextureData>();
        private readonly Action<TextureTool.TextureData> callback;
        private string nameFilter = "";
        public TexturePopupUI(Action<TextureTool.TextureData> textureCallback) : base("Texture Application Tool") {
            foreach (var (_, value) in TextureProvider.Packages.SelectMany(item1 => item1.Items)) {
                textureList.Add(new TextureTool.TextureData(value.Texture as AsyncTexture, value));
            }

            callback = textureCallback;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            if (ImGui.Button("Close")) {
                shouldBeOpen = false;
                return;
            }
            ImGui.InputText("Search", ref nameFilter, 255);
            ImGui.NewLine();
            if (ImGui.BeginChild("TextureSelect")) {
                for (int i = 0; i < textureList.Count; i++) {
                    if (!string.IsNullOrWhiteSpace(nameFilter) &&
                        !textureList[i].Texture.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) &&
                        !nameFilter.Contains(textureList[i].Texture.Name, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    var cursorPos = ImGui.GetCursorScreenPos();
                    if (ImGui.Button($"##TextureBox_{i}", new Num.Vector2(200, 200))) {
                        callback?.Invoke(textureList[i]);
                        shouldBeOpen = false;
                        return;
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

                ImGui.EndChild();
            }
        }
    }
}

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
        private List<TextureTool.TextureData> textureList = new List<TextureTool.TextureData>();
        private Action<TextureTool.TextureData> _callback;
        private string _namefilter = "";
        public TexturePopupUI(Action<TextureTool.TextureData> textureCallback) : base("Texture Application Tool") {
            foreach (var item1 in TextureProvider.Packages)
                foreach (var item2 in item1.Items)
                    textureList.Add(new TextureTool.TextureData(item2.Value.Texture as AsyncTexture, item2.Value));
            GameMain.Instance.PopupSelected = true;
            _callback = textureCallback;
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Button("Close")) {
                return false;
            }
            ImGui.InputText("Search", ref _namefilter, 255);
            int y = 0;
            ImGui.NewLine();
            if (ImGui.BeginChild("TextureSelect")) {
                for (int i = 0; i < textureList.Count; i++) {
                    if (string.IsNullOrWhiteSpace(_namefilter) || textureList[i].Texture.Name.Contains(_namefilter) ||
                        _namefilter.Contains(textureList[i].Texture.Name)) {
                        if (ImGui.BeginChild($"TextureBox_{i}", new Num.Vector2(200, 200))) {
                            if (textureList[i].AsyncTexture.ImGuiTexture != IntPtr.Zero) {
                                if (ImGui.ImageButton(textureList[i].AsyncTexture.ImGuiTexture,
                                    new Num.Vector2(50, 50))) {
                                    _callback?.Invoke(textureList[i]);
                                    return false;
                                }
                            }

                            ImGui.NewLine();
                            ImGui.Text(textureList[i].Texture.Name);
                            ImGui.NewLine();
                            ImGui.EndChild();
                        }

                        if (y++ < 3) {
                            ImGui.SameLine();
                        } else {
                            y = 0;
                        }
                    }
                }

                ImGui.EndChild();
            }

            return true;
        }
    }
}

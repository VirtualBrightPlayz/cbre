using System;
using System.Collections.Generic;
using System.Linq;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class TexturePopupUI : PopupUI {
        private List<AsyncTexture> textureList = new List<AsyncTexture>();
        private Action<AsyncTexture> _callback;
        public TexturePopupUI(Action<AsyncTexture> textureCallback) : base("Texture Application Tool") {
            foreach (var item in TextureProvider.Packages.First().Items)
            {
                textureList.Add(new AsyncTexture(item.Value.Filename));
            }
            GameMain.Instance.PopupSelected = true;
            _callback = textureCallback;
        }

        protected override bool ImGuiLayout() {
            if (ImGui.Button("Close")) {
                return false;
            }
            for (int i = 0; i < textureList.Count; i++) {
                if (textureList[i].ImGuiTexture != IntPtr.Zero) {
                    if (ImGui.ImageButton(textureList[i].ImGuiTexture, new Num.Vector2(50, 50))) {
                        _callback?.Invoke(textureList[i]);
                        return false;
                    }
                }
                ImGui.NewLine();
                ImGui.Text(textureList[i].Name);
                ImGui.NewLine();
            }
            return true;
        }
    }
}
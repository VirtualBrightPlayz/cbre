using System;
using System.Threading.Tasks;
using CBRE.Editor.Compiling;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using CBRE.Providers.Model;
using CBRE.Settings;
using ImGuiNET;
using NativeFileDialog;

namespace CBRE.Editor.Popup {
    public class ExportPopup : PopupUI {
        public enum LightmapSize : int {
            Tiny = 128,
            Small = 256,
            Normal = 512,
            Big = 1024,
            // VRamEater = 2048,
        }

        private Document document;
        private LightmapSize size = LightmapSize.Normal;
        private float downscaleFactor;

        public ExportPopup(Document document) : base("Export / Compile") {
            this.document = document;
            downscaleFactor = LightmapConfig.DownscaleFactor;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            throw new NotImplementedException();
        }
    }
}

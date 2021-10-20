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

        private Document _document;
        private LightmapSize _size = LightmapSize.Normal;
        private float downscale;

        public ExportPopup(Document document) : base("Export / Compile") {
            _document = document;
            downscale = LightmapConfig.DownscaleFactor;
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            if (Lightmapper.FaceRenderThreads != null && Lightmapper.FaceRenderThreads.Count > 0)
                return base.ImGuiLayout();
            var eval = Enum.GetValues<LightmapSize>();
            if (ImGui.BeginCombo("Size", $"{_size.ToString()} ({(int)_size})")) {
                for (int i = 0; i < eval.Length; i++) {
                    if (ImGui.Selectable($"{eval[i].ToString()} ({(int)eval[i]})", _size == eval[i])) {
                        _size = eval[i];
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.InputFloat("Downscale Factor", ref downscale);
            if (ImGui.Button("Render Lightmaps")) {
                try {
                    LightmapConfig.TextureDims = (int)_size;
                    LightmapConfig.DownscaleFactor = downscale;
                    Task.Run(() => {
                        try {
                        Lightmapper.Render(_document, out var faces, out var lmgroups);
                        } catch (Exception e) {
                            GameMain.Instance.PostDrawActions.Enqueue(() => {
                                Logging.Logger.ShowException(e);
                            });
                        }
                    });
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .rmesh")) {
                try {
                    LightmapConfig.TextureDims = (int)_size;
                    LightmapConfig.DownscaleFactor = downscale;
                    var result = NativeFileDialog.SaveDialog.Open("rmesh", _document.MapFileName, out string outPath);
                    if (result == Result.Okay) {
                        RMeshExport.SaveToFile(outPath, _document);
                    }
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .rm2")) {
                try {
                    LightmapConfig.TextureDims = (int)_size;
                    LightmapConfig.DownscaleFactor = downscale;
                    var result = NativeFileDialog.SaveDialog.Open("rm2", _document.MapFileName, out string outPath);
                    if (result == Result.Okay) {
                        RM2Export.SaveToFile(outPath, _document);
                    }
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .fbx")) {
                try {
                    LightmapConfig.TextureDims = (int)_size;
                    LightmapConfig.DownscaleFactor = downscale;
                    var result = NativeFileDialog.SaveDialog.Open("fbx", _document.MapFileName, out string outPath);
                    if (result == Result.Okay) {
                        AssimpProvider.SaveToFile(outPath, _document.Map, _document.GameData, "fbx");
                    }
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            if (ImGui.Button("Export as .gltf 2.0")) {
                try {
                    LightmapConfig.TextureDims = (int)_size;
                    LightmapConfig.DownscaleFactor = downscale;
                    var result = NativeFileDialog.SaveDialog.Open("gltf2", _document.MapFileName, out string outPath);
                    if (result == Result.Okay) {
                        AssimpProvider.SaveToFile(outPath, _document.Map, _document.GameData, "gltf2");
                    }
                }
                catch (System.Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
            return base.ImGuiLayout();
        }
    }
}

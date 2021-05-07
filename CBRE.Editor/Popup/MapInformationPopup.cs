using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using System.Linq;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class MapInformationPopup : PopupUI {

        public MapInformationPopup() : base("Map Information")
        {
            GameMain.Instance.PopupSelected = true;
        }

        protected override bool ImGuiLayout() {
            var document = DocumentManager.CurrentDocument;
            var all = document.Map.WorldSpawn.FindAll();
            var solids = all.OfType<Solid>().ToList();
            var faces = solids.SelectMany(x => x.Faces).ToList();
            var entities = all.OfType<Entity>().ToList();
            var numSolids = solids.Count;
            var numFaces = faces.Count;
            var numPointEnts = entities.Count(x => !x.HasChildren);
            var numSolidEnts = entities.Count(x => x.HasChildren);
            var uniqueTextures = faces.Select(x => x.Texture.Name).Distinct().ToList();
            var numUniqueTextures = uniqueTextures.Count;
            var textureMemory = faces.Select(x => x.Texture.Texture)
                .Where(x => x != null)
                .Distinct()
                .Sum(x => x.Width * x.Height * 3); // 3 bytes per pixel
            var textureMemoryMb = textureMemory / (1024m * 1024m);

            ImGui.Text($"Solids Count: {numSolids}");
            ImGui.Text($"Faces Count: {numFaces}");
            ImGui.Text($"Point Entities: {numPointEnts}");
            ImGui.Text($"Solid Entities: {numSolidEnts}");
            ImGui.Text($"Unique Textures: {numUniqueTextures}");
            ImGui.Text($"Texture Memory: {textureMemory.ToString("#,##0")} bytes");

            if (ImGui.Button("Close")) {
                return false;
            }

            return true;
        }
    }
}
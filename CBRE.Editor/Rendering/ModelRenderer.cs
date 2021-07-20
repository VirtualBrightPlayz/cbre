using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Models;
using CBRE.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Linq;

namespace CBRE.Editor.Rendering {
    public static class ModelRenderer {
        public static void Render(DataStructures.Models.Model model, Matrix mat, BasicEffect effect) {
            var transforms = model.GetTransforms();

            foreach (var group in model.GetActiveMeshes().GroupBy(x => x.SkinRef)) {
                var texture = model.Textures[group.Key].TextureObject;
                foreach (var mesh in group) {
                    PrimitiveDrawing.Begin(PrimitiveType.TriangleList);
                    PrimitiveDrawing.SetColor(Color.White);
                    if (texture != null) ((AsyncTexture)texture).Bind();
                    foreach (var v in mesh.Vertices) {
                        var transform = transforms[v.BoneWeightings.First().Bone.BoneIndex];
                        Vector3 c = new Vector3(v.Location * transform);
                        // if (texture != null) {
                            // PrimitiveDrawing.TexCoord2(v.TextureU, v.TextureV);
                        // }
                        // if (!float.IsNaN(v.TextureU) && !float.IsNaN(v.TextureV))
                        PrimitiveDrawing.Vertex3(c * mat, v.TextureU, v.TextureV);
                    }
                    /*effect.Texture = PrimitiveDrawing.texture;
                    effect.TextureEnabled = true;
                    effect.VertexColorEnabled = false;
                    effect.CurrentTechnique.Passes[0].Apply();*/
                    PrimitiveDrawing.End();
                    if (texture != null) ((AsyncTexture)texture).Unbind();
                }
            }
            effect.TextureEnabled = false;
            effect.VertexColorEnabled = true;
        }
    }
}
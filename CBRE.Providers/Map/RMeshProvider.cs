using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

using static CBRE.Common.PrimitiveConversion;

namespace CBRE.Providers.Map {
    public class RMeshProvider {
        public static void SaveToFile(string path, DataStructures.MapObjects.Map map, Texture2D[] lightmaps) {
            var visibleMeshes = new List<RMesh.RMesh.VisibleMesh>();
            var invisibleCollisionMeshes = new List<RMesh.RMesh.InvisibleCollisionMesh>();
            var entities = new List<Entity>();

            var vertices = new List<RMesh.RMesh.VisibleMesh.Vertex>();
            var triangles = new List<RMesh.RMesh.Triangle>();
            int indexOffset = 0;
            foreach (var solid in map.WorldSpawn.GetSelfAndAllChildren().OfType<Solid>()) {
                foreach (var face in solid.Faces) {
                    vertices.Clear();
                    triangles.Clear();
                    indexOffset = 0;

                    if (face.Texture?.Texture == null) continue;
                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/remove_face") continue;
                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/block_light") continue;

                    face.CalculateTextureCoordinates(true);

                    vertices.AddRange(face.Vertices.Select(fv => new RMesh.RMesh.VisibleMesh.Vertex(
                        new Vector3F(fv.Location.XZY()),
                        new Vector2F((float)fv.TextureU, (float)fv.TextureV),
                        face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? Vector2F.Zero : new Vector2F((float)fv.LMU, (float)fv.LMV), face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? System.Drawing.Color.Transparent : System.Drawing.Color.White)));
                    triangles.AddRange(face.GetTriangleIndices().Chunk(3).Select(c => new RMesh.RMesh.Triangle(
                        (ushort)(c[0] + indexOffset), (ushort)(c[1] + indexOffset), (ushort)(c[2] + indexOffset))));
                    indexOffset += face.Vertices.Count;

                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/invisible_collision") {
                        var mesh = new RMesh.RMesh.InvisibleCollisionMesh(vertices.ToImmutableArray().Select(x => new RMesh.RMesh.InvisibleCollisionMesh.Vertex(x.Position)).ToImmutableArray(), triangles.ToImmutableArray());
                        invisibleCollisionMeshes.Add(mesh);
                    }
                    else {
                        // var diff = System.IO.Path.GetFileName((face.Texture.Texture as AsyncTexture).Filename);
                        var diff = face.Texture.Name+System.IO.Path.GetExtension((face.Texture.Texture as AsyncTexture).Filename);
                        // diff = string.Empty;
                        var lm = System.IO.Path.GetFileName(path)+"_lm.png";
                        var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? RMesh.RMesh.VisibleMesh.BlendMode.Translucent : RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped);
                        visibleMeshes.Add(mesh);
                    }
                }
            }
            
            foreach (var entity in map.WorldSpawn.GetSelfAndAllChildren().OfType<Entity>()) {
                if (entity.GameData.RMeshDef == null) continue;
                var cond = entity.GameData.RMeshDef?.Conditions.FirstOrDefault();
                if (cond == null || !entity.GameData.RMeshDef.Conditions.Any() || entity.EntityData.GetPropertyValue(cond?.Property)?.ToLowerInvariant() == cond?.Equal.ToLowerInvariant()) {
                    var rmEntity = new RMesh.RMesh.Entity(entity.ClassName, entity.GameData.RMeshDef.Entries.ToImmutableArray());
                    entities.Add(entity);
                }
            }
            // LegacyLightmapper.SaveLightmaps(document, 1, path, false);
            var texture = lightmaps[0];
            FileStream fs = File.OpenWrite(path+"_lm.png");
            texture.SaveAsPng(fs, texture.Width, texture.Height);
            fs.Close();
            // var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), "", DocumentManager.CurrentDocument.MapFileName+"_lm0.png", RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped);
            // visibleMeshes.Add(mesh);
            
            RMesh.RMesh rmesh = new RMesh.RMesh(
                visibleMeshes.ToImmutableArray(),
                invisibleCollisionMeshes.ToImmutableArray(),
                null, null, entities.ToImmutableArray());

            RMesh.RMesh.Saver.ToFile(rmesh, path);
        }
    }
}

using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using Microsoft.Xna.Framework.Graphics;
using NativeFileDialog;
using RMeshDecomp;
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
    public class RMeshProvider : MapProvider {
        public static void SaveToFile(string path, DataStructures.MapObjects.Map map, Texture2D[] lightmaps, Face[] modelFaces, bool modelLightmaps) {
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
                        var diff = System.IO.Path.GetFileName((face.Texture.Texture as AsyncTexture).Filename);
                        // var diff = face.Texture.Name+System.IO.Path.GetExtension((face.Texture.Texture as AsyncTexture).Filename);
                        // diff = string.Empty;
                        var lm = System.IO.Path.GetFileName(path)+"_lm.png";
                        var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? RMesh.RMesh.VisibleMesh.BlendMode.Translucent : RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped);
                        visibleMeshes.Add(mesh);
                    }
                }
            }

            if (modelFaces.Any()) {
                foreach (var face in modelFaces) {
                    vertices.Clear();
                    triangles.Clear();
                    indexOffset = 0;

                    if (face.Texture?.Texture == null) continue;
                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/remove_face") continue;
                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/block_light") continue;

                    // face.CalculateTextureCoordinates(true);

                    vertices.AddRange(face.Vertices.Select(fv => new RMesh.RMesh.VisibleMesh.Vertex(
                        new Vector3F(fv.Location.XZY()),
                        new Vector2F((float)fv.TextureU, (float)fv.TextureV),
                        new Vector2F((float)fv.LMU, (float)fv.LMV), Color.FromArgb(0xff, (byte)(fv.Color.X * 255f), (byte)(fv.Color.Y * 255f), (byte)(fv.Color.Z * 255f)))));
                    triangles.AddRange(face.GetTriangleIndices().Chunk(3).Select(c => new RMesh.RMesh.Triangle(
                        (ushort)(c[0] + indexOffset), (ushort)(c[1] + indexOffset), (ushort)(c[2] + indexOffset))));
                    indexOffset += face.Vertices.Count;

                    if (face.Texture.Name.ToLowerInvariant() == "tooltextures/invisible_collision") {
                        var mesh = new RMesh.RMesh.InvisibleCollisionMesh(vertices.ToImmutableArray().Select(x => new RMesh.RMesh.InvisibleCollisionMesh.Vertex(x.Position)).ToImmutableArray(), triangles.ToImmutableArray());
                        invisibleCollisionMeshes.Add(mesh);
                    }
                    else {
                        var diff = System.IO.Path.GetFileName((face.Texture.Texture as AsyncTexture).Filename);
                        // var diff = System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(path), (face.Texture.Texture as AsyncTexture).Filename).Replace('\\', '/');
                        // var diff = face.Texture.Name+System.IO.Path.GetExtension((face.Texture.Texture as AsyncTexture).Filename);
                        // diff = string.Empty;
                        var lm = System.IO.Path.GetFileName(path)+"_lm.png";
                        // lm = string.Empty;
                        var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, modelLightmaps ? RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped : RMesh.RMesh.VisibleMesh.BlendMode.Opaque);
                        visibleMeshes.Add(mesh);
                    }
                }
            }
            
            foreach (var entity in map.WorldSpawn.GetSelfAndAllChildren().OfType<Entity>()) {
                if (entity.GameData.RMeshDef == null) continue;
                bool shouldBake = entity.EntityData.GetPropertyValue("bake")?.ToLowerInvariant() == "true";
                if (modelFaces.Any() && entity.ClassName.ToLowerInvariant() == "model" && shouldBake) continue;
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

        protected override IEnumerable<MapFeature> GetFormatFeatures() {
            return new[]
            {
                MapFeature.Worldspawn,
                MapFeature.Solids,
                MapFeature.Entities,
                MapFeature.Groups,

                // MapFeature.Displacements,
                // MapFeature.Instances,

                // MapFeature.Colours,
                // MapFeature.SingleVisgroups,
                // MapFeature.MultipleVisgroups,
                // MapFeature.Cameras,
                // MapFeature.CordonBounds,
                // MapFeature.ViewSettings
            };
        }

        protected override DataStructures.MapObjects.Map GetFromStream(Stream stream) {
            var map = new DataStructures.MapObjects.Map();

            var visibleMeshes = new List<RMesh.RMesh.VisibleMesh>();
            var invisibleCollisionMeshes = new List<RMesh.RMesh.InvisibleCollisionMesh>();

            var vertices = new List<RMesh.RMesh.VisibleMesh.Vertex>();
            var triangles = new List<RMesh.RMesh.Triangle>();
            int indexOffset = 0;
            foreach (var solid in map.WorldSpawn.GetSelfAndAllChildren().OfType<Solid>()) {
                foreach (var face in solid.Faces) {
                    vertices.AddRange(face.Vertices.Select(fv => new RMesh.RMesh.VisibleMesh.Vertex(
                        new Vector3F(fv.Location),
                        new Vector2F((float)fv.TextureU, (float)fv.TextureV),
                        Vector2F.Zero, Color.White)));
                    triangles.AddRange(face.GetTriangleIndices().Chunk(3).Select(c => new RMesh.RMesh.Triangle(
                        (ushort)(c[0] + indexOffset), (ushort)(c[1] + indexOffset), (ushort)(c[2] + indexOffset))));
                    indexOffset += face.Vertices.Count;
                }
            }
            
            var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), "", "", RMesh.RMesh.VisibleMesh.BlendMode.Opaque);
            visibleMeshes.Add(mesh);

            RMesh.RMesh rmesh = new RMesh.RMesh(
                visibleMeshes.ToImmutableArray(),
                invisibleCollisionMeshes.ToImmutableArray(),
                null, null, null);

            // var result = NativeFileDialog.OpenDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string outPath);
            // if (result == Result.Okay) {
                rmesh = RMesh.RMesh.Loader.FromStream(stream);

                var idGenerator = map.IDGenerator;

                var rng = new Random();
                foreach (var subMesh in rmesh.VisibleMeshes) {
                    if (subMesh.TextureBlendMode != RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped) { continue; }
                    
                    var newFaces = new HashSet<Face>();
                    ExtractFaces.Invoke(subMesh, newFaces);

                    if (!newFaces.Any()) { continue; }
                    
                    var newSolid = new Solid(idGenerator.GetNextObjectID());
                    newSolid.Colour = Color.Chartreuse;
                    //newSolid.Faces.AddRange(newFaces);
                    foreach (var newFace in newFaces) {
                        newSolid.Faces.Add(newFace);
                        newFace.Parent = newSolid;
                        newFace.Texture = new TextureReference();
                        string tex = subMesh.DiffuseTexture.Replace(".jpg", "").Replace(".jpeg", "").Replace(".png", "");
                        TextureItem item = TextureProvider.GetItem(tex);
                        newFace.Texture.Name = item?.Name;
                        newFace.Texture.Texture = item?.Texture as AsyncTexture;
                        newFace.Colour = Color.FromArgb(255,
                            rng.Next()%256,
                            newFace.IsConvex(0.001m) && !newFace.HasColinearEdges(0.001m) ? 255 : 0,
                            newFace.IsConvex(0.001m) && !newFace.HasColinearEdges(0.001m) ? 0 : 255);
                        newFace.AlignTextureToFace();
                        newFace.CalculateTextureCoordinates(true);
                    }

                    if (newSolid.Faces.Any()) {
                        // TODO: textures
#if false
                        foreach (var newFace in newSolid.Faces.GroupBy(x => x.Texture, x => x).ToDictionary(x => x.Key, x => x.ToList())) {
                            var axis = newFace.Value.SelectMany(x => x.Vertices).OrderByDescending(x => x.TextureU + x.TextureV);
                            var uaxis = newFace.Value.SelectMany(x => x.Vertices).OrderByDescending(x => x.TextureU);
                            var vaxis = newFace.Value.SelectMany(x => x.Vertices).OrderByDescending(x => x.TextureV);
                            // int i = 0;
                            var item = newFace.Key;
                            if (item != null) {
                                foreach (var f in newFace.Value) {
                                    var us = f.Vertices.Select(x => x.TextureU);
                                    var vs = f.Vertices.Select(x => x.TextureV);
                                    int tileX = 1, tileY = 1;
                                    decimal minU = us.Min();
                                    decimal minV = vs.Min();
                                    decimal maxU = us.Max();
                                    decimal maxV = vs.Max();
                                    var XScale = (maxU - minU) / (item.Texture.Width * tileX);
                                    var YScale = (maxV - minV) / (item.Texture.Height * tileY);
                                    // var XShift = -minU / XScale;
                                    // var YShift = -minV / YScale;
                                    var XShift = -minU / ((item.Texture.Width * tileX));
                                    var YShift = -minV / ((item.Texture.Height * tileY));
                                    f.Texture.XScale = XScale * item.Texture.Width;
                                    f.Texture.YScale = YScale * item.Texture.Height;
                                    f.Texture.XShift = XShift * item.Texture.Width;
                                    f.Texture.YShift = YShift * item.Texture.Height;
                                    f.AlignTextureToFace();
                                }

                                // var XScale = (uaxis.First().TextureU - uaxis.Last().TextureU);
                                // var YScale = (vaxis.First().TextureV - vaxis.Last().TextureV);
                                // var XShift = (axis.Last().TextureU) * item.Texture.Width;// 2 - item.Texture.Width / 2;// * newFace.Texture.XScale;
                                // var YShift = (axis.Last().TextureV) * item.Texture.Height;// 2 - item.Texture.Height / 2;// * newFace.Texture.YScale;
                                var locs = newFace.Value.SelectMany(x => x.Vertices).Select(x => x.Location);//.Take(3);
                                var cloud = new Cloud(locs);
                                // newFace.Value.ForEach(x => x.Texture.XScale = XScale);
                                // newFace.Value.ForEach(x => x.Texture.YScale = YScale);
                                // newFace.Value.ForEach(x => x.Texture.XShift = XShift);
                                // newFace.Value.ForEach(x => x.Texture.YShift = YShift);
                                // newFace.Value.ForEach(x => x.AlignTextureWithFace(x));
                                // newFace.Value.ForEach(x => x.FitTextureToPointCloud(new Cloud(x.Vertices.Select(y => y.Location)), 0, 0));
                                // newFace.Value.ForEach(x => x.AlignTextureWithPointCloud(new Cloud(x.Vertices.Select(y => y.Location)), Face.BoxAlignMode.Center));
                                // newFace.Value.ForEach(x => x.FitTextureToPointCloud(cloud, 0, 0));
                                // newFace.Value.ForEach(x => x.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Center));
                            }
                        }
#endif
                        newSolid.SetParent(map.WorldSpawn);
                        // _document.ObjectRenderer.AddMapObject(newSolid);
                    }
                }
            // }

            return map;
        }

        protected override bool IsValidForFileName(string filename) {
            return filename.EndsWith(".rmesh", StringComparison.OrdinalIgnoreCase);
        }

        protected override void SaveToStream(Stream stream, DataStructures.MapObjects.Map map) {
            throw new NotImplementedException();
        }
    }
}

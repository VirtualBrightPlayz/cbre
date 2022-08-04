using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Transformations;
using CBRE.Extensions;
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
                if (entity.GameData?.RMeshDef == null || string.IsNullOrWhiteSpace(entity.ClassName) || string.IsNullOrWhiteSpace(entity.GameData?.RMeshDef?.ClassName)) continue;
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
                        // newFace.AlignTextureToFace();
                        // newFace.CalculateTextureCoordinates(true);
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
                                    if (item.Name.ToLowerInvariant().StartsWith("slh_miscsigns"))
                                        Debugger.Break();
                                    var v3s = f.Vertices.Select(x => x.Location);
                                    var us = f.Vertices.Select(x => x.TextureU);
                                    var vs = f.Vertices.Select(x => x.TextureV);
                                    // int tileX = 1, tileY = 1;
                                    f.Plane = new Plane(f.Vertices[0].Location, f.Vertices[1].Location, f.Vertices[2].Location);
                                    f.UpdateBoundingBox();
                                    // f.AlignTextureToFace();
                                    // continue;
                                    // var m2 = Microsoft.Xna.Framework.Matrix.CreateLookAt(f.BoundingBox.Center.ToXna(), (f.BoundingBox.Center + f.Plane.Normal).ToXna(), f.Texture.UAxis.ToXna());
                                    var m2 = Microsoft.Xna.Framework.Matrix.CreateLookAt(Vector3.Zero.ToXna(), (f.Plane.Normal).ToXna(), f.Texture.UAxis.ToXna());
                                    // m2 = Microsoft.Xna.Framework.Matrix.Invert(m2);
                                    // var m = m2.ToCbre();
                                    // Matrix m = Matrix.Translation(f.BoundingBox.Center) * Matrix.Rotation(f.Plane.Normal, 0);// * Matrix.Scale(f.BoundingBox.Dimensions * 2);
                                    var sclMin = v3s.ElementAt(0);
                                    var sclMax = v3s.ElementAt(2);
                                    decimal minU = us.ElementAt(0);
                                    decimal minV = vs.ElementAt(0);
                                    decimal maxU = us.ElementAt(2);
                                    decimal maxV = vs.ElementAt(2);

                                    // var item0 = minU * item.Texture.Width;

                                    // var item2 = (sclMin.X / f.Texture.UAxis.X) - (sclMin.Y / f.Texture.UAxis.Y) - (sclMin.Z / f.Texture.UAxis.Z);

                                    // decimal minU = us.Min();
                                    // decimal minV = vs.Min();
                                    // decimal maxU = us.Max();
                                    // decimal maxV = vs.Max();
                                    // var XScale = (decimal)Math.Sqrt((double)Math.Abs(maxU - minU));
                                    // var YScale = (decimal)Math.Sqrt((double)Math.Abs(maxV - minV));

                                    f.AlignTextureToFace();

                                    var XShift = minU * (item.Texture.Width);
                                    var YShift = minV * (item.Texture.Height);

                                    // f.AlignTextureToFace();

                                    // var vdiv = Texture.Texture.Height * Texture.YScale;
                                    // var vadd = Texture.YShift / Texture.Texture.Height;
                                    // v.TextureV = (v.Location.Dot(Texture.VAxis) / vdiv) + vadd;

                                    var transform = new UnitRotate(0, new Line(Vector3.Zero, f.Plane.Normal));
                                    var XScale = (maxU - minU) * transform.Transform(f.Texture.UAxis).Dot(Vector3.UnitX);
                                    var YScale = (maxV - minV) * transform.Transform(f.Texture.VAxis).Dot(Vector3.UnitZ);
                                    // var XScale = (minU - XShift / item.Texture.Width) * item.Texture.Width; // Math.Max(sclMin.Dot(f.Texture.UAxis), (decimal)0.0001) / item.Texture.Width;
                                    // var YScale = (minV - YShift / item.Texture.Height) * item.Texture.Height; // Math.Max(sclMin.Dot(f.Texture.VAxis), (decimal)0.0001) / item.Texture.Height;
                                    f.Texture.XScale = XScale;
                                    f.Texture.YScale = YScale;
                                    // f.Texture.XShift = (XScale * XShift);
                                    // f.Texture.YShift = (YScale * YShift);
                                    f.Texture.XShift = XShift;
                                    f.Texture.YShift = YShift;
                                    // f.AlignTextureToWorld();
                                    f.AlignTextureToFace();
                                    if (item.Name.ToLowerInvariant().StartsWith("slh_miscsigns"))
                                        f.SetTextureRotation(90);
                                        // Debugger.Break();
                                }
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

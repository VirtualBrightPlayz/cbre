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
            bool hasLightmaps = lightmaps != null && lightmaps.Length > 0;

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
                        new Vector2F((float)fv.LMU, (float)fv.LMV), System.Drawing.Color.White)));
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
                        var lm = System.IO.Path.GetFileName(path)+face.LmIndex.ToString()+"_lm.png";
                        var blendMode = RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped;
                        if (face.Texture.Texture.HasTransparency()) {
                            blendMode = RMesh.RMesh.VisibleMesh.BlendMode.Translucent;
                            lm = string.Empty;
                        } else if (!hasLightmaps) {
                            blendMode = RMesh.RMesh.VisibleMesh.BlendMode.Opaque;
                            lm = string.Empty;
                        }
                        var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, blendMode);
                        visibleMeshes.Add(mesh);
                    }
                }
            }

            if (modelFaces != null && modelFaces.Any()) {
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
                    /*
                    for (int i = 0; i < vertices.Count; i+=3) {
                        triangles.Add(new RMesh.RMesh.Triangle((ushort)i, (ushort)(i+1), (ushort)(i+2)));
                    }
                    */
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
                        var lm = System.IO.Path.GetFileName(path)+face.LmIndex.ToString()+"_lm.png";
                        // lm = string.Empty;
                        var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, modelLightmaps ? RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped : RMesh.RMesh.VisibleMesh.BlendMode.Opaque);
                        visibleMeshes.Add(mesh);
                    }
                }
            }
            
            foreach (var entity in map.WorldSpawn.GetSelfAndAllChildren().OfType<Entity>()) {
                if (entity.GameData?.RMeshDef == null || string.IsNullOrWhiteSpace(entity.ClassName) || string.IsNullOrWhiteSpace(entity.GameData?.RMeshDef?.ClassName)) continue;
                bool shouldBake = entity.EntityData.GetPropertyValue("bake")?.ToLowerInvariant() != "false";
                if (modelFaces != null && modelFaces.Any() && entity.ClassName.ToLowerInvariant() == "model" && shouldBake) continue;
                var cond = entity.GameData.RMeshDef?.Conditions.FirstOrDefault();
                if (cond == null || !entity.GameData.RMeshDef.Conditions.Any() || entity.EntityData.GetPropertyValue(cond?.Property)?.ToLowerInvariant() == cond?.Equal.ToLowerInvariant()) {
                    // var rmEntity = new RMesh.RMesh.Entity(entity.ClassName, entity.GameData.RMeshDef.Entries.ToImmutableArray());
                    entities.Add(entity);
                }
            }

            void saveTexture(string filePath, Texture2D texture) {
                if (texture.Format != SurfaceFormat.Vector4) {
                    string fname = System.IO.Path.Combine(typeof(RMeshProvider).Assembly.Location, "..", filePath);
                    using var fileSaveStream = File.Open(fname, FileMode.Create);
                    texture.SaveAsPng(fileSaveStream, texture.Width, texture.Height);
                } else {
                    using RenderTarget2D rt = new RenderTarget2D(GlobalGraphics.GraphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    GlobalGraphics.GraphicsDevice.SetRenderTarget(rt);
                    using var effect = new BasicEffect(GlobalGraphics.GraphicsDevice);
                    effect.TextureEnabled = true;
                    effect.Texture = texture;
                    effect.CurrentTechnique.Passes[0].Apply();
                    PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                    PrimitiveDrawing.Vertex2(-1f, -1f, 0f, 1f);
                    PrimitiveDrawing.Vertex2(1f, -1f, 1f, 1f);
                    PrimitiveDrawing.Vertex2(1f, 1f, 1f, 0f);
                    PrimitiveDrawing.Vertex2(-1f, 1f, 0f, 0f);
                    PrimitiveDrawing.End();
                    GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
                    string fname = System.IO.Path.Combine(typeof(RMeshProvider).Assembly.Location, "..", filePath);
                    using var fileSaveStream = File.Open(fname, FileMode.Create);
                    rt.SaveAsPng(fileSaveStream, rt.Width, rt.Height);
                }
            }
            // LegacyLightmapper.SaveLightmaps(document, 1, path, false);
            if (hasLightmaps) {
                for (int i = 0; i < lightmaps.Length; i++) {
                    var texture = lightmaps[i];
                    saveTexture(path+i+"_lm.png", texture);
                }
            }
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

            RMesh.RMesh rmesh = RMesh.RMesh.Loader.FromStream(stream, new DataStructures.GameData.GameData());

            var idGenerator = map.IDGenerator;

            foreach (var ent in rmesh.Entities) {
                ent.ID = idGenerator.GetNextObjectID();
                ent.SetParent(map.WorldSpawn);
            }

            var rng = new Random();
            foreach (var subMesh in rmesh.VisibleMeshes) {
                if (subMesh.TextureBlendMode != RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped) { continue; }
                
                var newFaces = new HashSet<Face>();
                ExtractFaces.Invoke(subMesh, newFaces);

                if (!newFaces.Any()) { continue; }

                var newSolids = new HashSet<Solid>();

                var newSolid = new Solid(idGenerator.GetNextObjectID());
                newSolid.Colour = Color.Chartreuse;
                //newSolid.Faces.AddRange(newFaces);

                newSolids.Add(newSolid);

                foreach (var newFace in newFaces) {
                    newFace.Plane = new Plane(newFace.Vertices[0].Location, newFace.Vertices[1].Location, newFace.Vertices[2].Location);
                    newFace.UpdateBoundingBox();
                    /*
                    newFace.Parent = null;
                    foreach (var solid in newSolids) {
                        solid.Faces.Add(newFace);
                        if (solid.Faces.Any() && !solid.IsValid()) {
                            solid.Faces.Remove(newFace);
                        } else {
                            // solid.Faces.Remove(newFace);
                            newFace.Parent = solid;
                            break;
                        }
                    }
                    if (newFace.Parent == null) {
                        newSolid.SetParent(map.WorldSpawn);
                        newSolid = new Solid(idGenerator.GetNextObjectID());
                        newSolid.Colour = Color.Chartreuse;
                        newSolids.Add(newSolid);
                        newSolid.Faces.Add(newFace);
                        newFace.Parent = newSolid;
                    }
                    */
                    newSolid.Faces.Add(newFace);
                    if (newSolid.Faces.Any() && !newSolid.IsValid(0.001m)) {
                        newSolid.Faces.Remove(newFace);
                        newSolid.SetParent(map.WorldSpawn);
                        newSolid = new Solid(idGenerator.GetNextObjectID());
                        newSolid.Colour = Color.Chartreuse;
                        newSolids.Add(newSolid);
                        newSolid.Faces.Add(newFace);
                    }
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
                    // newFace.AlignTextureToWorld();

                    // var h = Quaternion.AxisAngle(newFace.Plane.Normal, 0).GetMatrix().Inverse();
                    // newFace.BoundingBox.Dimensions.VectorMagnitude();

                    var lst = new List<Vector3>();
                    var tris = newFace.GetTriangles().ToList();

                    for (int i = 0; i < tris.Count; i++) {
                        for (int j = 0; j < tris[i].Length; j++) {
                            var u = tris[i][j].TextureU;
                            var v = tris[i][j].TextureV;
#if false
                            while (u <= 0m) {
                                u += 1m;
                            }
                            while (v <= 0m) {
                                v += 1m;
                            }
                            u = u % 1m;
                            v = v % 1m;
                            u *= -1m;
                            v *= -1m;
#endif
                            v *= -1m;
                            if (u < 0 || u > 1 || v < 0 || v > 1) {
                                continue;
                            }
                            var bary = Vector4d.BaryCentric(new Vector4d(tris[i][0].Location), new Vector4d(tris[i][1].Location), new Vector4d(tris[i][2].Location), u, v);
                            lst.Add(new Vector3(bary.X, bary.Y, bary.Z));
                        }
                        // break;
                    }

#if false
                    if (lst.Count > 0) {
                        var cloud = new Cloud(lst);
                        var plMin = newFace.Plane.Project(newFace.BoundingBox.Start);
                        var plMax = newFace.Plane.Project(newFace.BoundingBox.End);
                        var verts = newFace.Vertices.Select(x => x.Clone()).ToList();
                        // var cloud = new Cloud(newFace.Vertices.Select(x => new Vector3(x.TextureU, x.TextureV, 0m)));
                        // var cloud = new Cloud(newFace.BoundingBox.GetBoxPoints());
                        // newFace.AlignTextureToWorld();
                        try {
                            newFace.AlignTextureToFace();
                            // newFace.AlignTextureToWorld();
                            Vertex getVertexForUAxis(Vertex y) {
                                return verts.OrderByDescending(x => (x.Location - y.Location).Dot(newFace.Texture.UAxis)).First();
                            }
                            Vertex getVertexForVAxis(Vertex y) {
                                return verts.OrderByDescending(x => (x.Location - y.Location).Dot(newFace.Texture.VAxis)).First();
                            }
                            var matrix = Microsoft.Xna.Framework.Matrix.CreateLookAt(Vector3.Zero.ToXna(), newFace.Texture.GetNormal().ToXna(), newFace.Texture.VAxis.ToXna()).ToCbre();
                            decimal getUAxisSpace(Vertex x, Vertex u) {
                                return (u.Location * matrix - x.Location * matrix).X;// * (u.TextureU + x.TextureU);
                            }
                            decimal getVAxisSpace(Vertex x, Vertex v) {
                                return (v.Location * matrix - x.Location * matrix).Y;// * (v.TextureV + x.TextureV);
                            }

                            var matrix2 = Microsoft.Xna.Framework.Matrix.CreateLookAt(Vector3.Zero.ToXna(), newFace.Texture.GetNormal().ToXna(), newFace.Texture.VAxis.ToXna()).ToCbre();
                            // matrix2 *= Matrix.Scale(new Vector3(1 / newFace.BoundingBox.Width, 1 / newFace.BoundingBox.Length, 1 / newFace.BoundingBox.Height));
                            matrix2 *= Matrix.Scale(newFace.BoundingBox.Dimensions);
                            // var cloud = new Cloud(verts.Select(x => new Vector3(x.TextureU, x.TextureV, 0m) * matrix2));
                            newFace.FitTextureToPointCloud(cloud, 1, 1);
                            // newFace.Texture.XScale *= newFace.Plane.Normal.Dot(newFace.Texture.UAxis);
                            // newFace.Texture.YScale *= newFace.Plane.Normal.Dot(newFace.Texture.VAxis);
                            // newFace.Texture.XScale *= newFace.BoundingBox.Dimensions.VectorMagnitude();
                            // newFace.Texture.YScale *= newFace.Texture.Texture.Height;
                            // newFace.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Bottom);
                            // newFace.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Center);
                            // newFace.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Left);
                            // newFace.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Right);
                            // newFace.AlignTextureWithPointCloud(cloud, Face.BoxAlignMode.Top);
                            // newFace.Texture.XScale *= newFace.Texture.Texture.Width;
                            // newFace.Texture.YScale *= newFace.Texture.Texture.Height;
                        } catch (DivideByZeroException) {
                        }
                    }
#endif

#if true
                    var verts = newFace.Vertices.Select(x => x.Clone()).ToList();
                    newFace.AlignTextureToFace();
                    // newFace.AlignTextureToWorld();

                    var v0u = verts[0].Location.Dot(newFace.Texture.UAxis);
                    var v0v = verts[0].Location.Dot(newFace.Texture.VAxis);
                    var v1u = verts[1].Location.Dot(newFace.Texture.UAxis);
                    var v1v = verts[1].Location.Dot(newFace.Texture.VAxis);
                    var v2u = verts[2].Location.Dot(newFace.Texture.UAxis);
                    var v2v = verts[2].Location.Dot(newFace.Texture.VAxis);
                    if (v0u == 0) v0u = 1;
                    if (v0v == 0) v0v = 1;
                    if (v1u == 0) v1u = 1;
                    if (v1v == 0) v1v = 1;
                    if (v2u == 0) v2u = 1;
                    if (v2v == 0) v2v = 1;
                    var minU = verts.MinBy(x => (x.TextureU));
                    var minV = verts.MinBy(x => (x.TextureV));
                    var maxU = verts.MaxBy(x => (x.TextureU));
                    var maxV = verts.MaxBy(x => (x.TextureV));
                    var dimU = newFace.BoundingBox.Dimensions.Dot(newFace.Texture.UAxis);
                    var dimV = newFace.BoundingBox.Dimensions.Dot(newFace.Texture.VAxis);
                    // dimU = verts[0].Location.Dot(newFace.Texture.UAxis);
                    // dimV = verts[0].Location.Dot(newFace.Texture.VAxis);
                    if (dimU == 0) dimU = 1;
                    if (dimV == 0) dimV = 1;

                    var v0uv = new Vector2d(verts[0].TextureU, verts[0].TextureV);
                    var v1uv = new Vector2d(verts[1].TextureU, verts[1].TextureV);
                    // var uvUpDir = (v1uv - v0uv);
                    // var uvUpDirN = uvUpDir.Normalized();
                    // var uDir = Vector2d.Dot(uvUpDirN, Vector2d.UnitX);
                    // var vDir = Vector2d.Dot(uvUpDirN, Vector2d.UnitY);
                    // minU = verts[0];
                    // minV = verts[0];
                    // maxU = verts[1];
                    // maxV = verts[1];

                    var xscl = (maxU.TextureU - minU.TextureU);// * uDir / uvUpDir.Length;
                    var yscl = (maxV.TextureV - minV.TextureV);// * vDir / uvUpDir.Length;
                    if (xscl == 0) xscl = 1;
                    if (yscl == 0) yscl = 1;

                    newFace.Texture.XScale = 1 / (xscl / (dimU) * newFace.Texture.Texture.Width);
                    newFace.Texture.YScale = -1 / (yscl / (dimV) * newFace.Texture.Texture.Height);
                    newFace.Texture.XShift = -(minU.Location.Dot(newFace.Texture.UAxis) / (newFace.Texture.XScale * newFace.Texture.Texture.Width) - minU.TextureU) * newFace.Texture.Texture.Width;
                    newFace.Texture.YShift = -(minV.Location.Dot(newFace.Texture.VAxis) / (newFace.Texture.YScale * newFace.Texture.Texture.Height) - minV.TextureV) * newFace.Texture.Texture.Height;

#if false
                    if (newFace.Texture.Texture.Width > 0 && newFace.Texture.Texture.Height > 0 && verts.Count > 0) {
                        newFace.Texture.XScale = 0;
                        newFace.Texture.YScale = 0;
                        newFace.Texture.XShift = 0;
                        newFace.Texture.YShift = 0;

                        int j = 0;
                        var loc = Vector3.Zero;

                        for (int i = 0; i < verts.Count; i++) {
                            var v0 = verts[i];


                            loc += v0.Location;
                            var testU = Math.Abs(v0.TextureU + v0.Location.Dot(newFace.Texture.UAxis));
                            var testV = Math.Abs(v0.TextureV + v0.Location.Dot(newFace.Texture.VAxis));
                            var oldXShift = newFace.Texture.XShift;
                            var oldYShift = newFace.Texture.YShift;
                            newFace.Texture.XShift = newFace.Texture.Texture.Width;
                            newFace.Texture.YShift = newFace.Texture.Texture.Height;
                            newFace.Texture.XShift = 0;
                            newFace.Texture.YShift = 0;

                            for (int x = 1; x < newFace.Texture.Texture.Width; x++) {
                                var testU2 = testU * ((decimal)x / newFace.Texture.Texture.Width);
                                testU2 = Math.Abs(testU2);
                                if (testU2 > newFace.Texture.XShift) {
                                    newFace.Texture.XShift = testU2;
                                }
                            }
                            for (int y = 1; y < newFace.Texture.Texture.Height; y++) {
                                var testV2 = testV * ((decimal)y / newFace.Texture.Texture.Height);
                                testV2 = Math.Abs(testV2);
                                if (testV2 > newFace.Texture.YShift) {
                                    newFace.Texture.YShift = testV2;
                                }
                            }

                            var testU3 = testU;
                            var testV3 = testV;
                            if (Math.Abs(newFace.Texture.XShift) > 0.001m) {
                                // testU3 /= newFace.Texture.XShift;
                            }
                            if (Math.Abs(newFace.Texture.YShift) > 0.001m) {
                                // testV3 /= newFace.Texture.YShift;
                            }
                            newFace.Texture.XScale += testU3 / newFace.Texture.Texture.Width;
                            newFace.Texture.YScale += testV3 / newFace.Texture.Texture.Height;
                            newFace.Texture.XShift += oldXShift;
                            newFace.Texture.YShift += oldYShift;
                            j++;
                            // break;
                        }

                        newFace.Texture.XScale /= j;
                        newFace.Texture.YScale /= j;
                        // loc /= j;
                        // if (loc.VectorMagnitude() != 0) {
                        //     newFace.Texture.XScale /= (loc.VectorMagnitude());
                        //     newFace.Texture.YScale /= (loc.VectorMagnitude());
                        // }
                        newFace.Texture.XShift /= j;
                        newFace.Texture.YShift /= j;
                        newFace.Texture.XShift = 0;
                        newFace.Texture.YShift = 0;

                        // newFace.Texture.XScale *= newFace.Texture.Texture.Width;
                        // newFace.Texture.YScale *= newFace.Texture.Texture.Height;
                    }
#endif

                    /*
                    xscale = texUMax - texUMin
                    -xshift = dotU / xscale - texU
                    */

                    /*
                    newFace.Texture.XScale = (maxU - minU) / dimU;
                    newFace.Texture.YScale = (maxV - minV) / dimV;
                    if (newFace.Texture.XScale == 0) newFace.Texture.XScale = 1;
                    if (newFace.Texture.YScale == 0) newFace.Texture.YScale = 1;
                    newFace.Texture.XShift = -((v0u / newFace.Texture.XScale) - minU) * newFace.Texture.Texture.Width;
                    newFace.Texture.YShift = -((v0v / newFace.Texture.YScale) - minV) * newFace.Texture.Texture.Height;
                    // newFace.Texture.XScale = -1 - newFace.Texture.XScale;
                    // newFace.Texture.YScale = -1 - newFace.Texture.YScale;
                    */

                    /*
                    // newFace.Texture.XScale = (minU - (newFace.Texture.XShift / newFace.Texture.Texture.Width)) * (v0u == 0 ? 0 : v0u) / newFace.Texture.Texture.Width;
                    // newFace.Texture.YScale = (minV - (newFace.Texture.YShift / newFace.Texture.Texture.Height)) * (v0v == 0 ? 0 : v0v) / newFace.Texture.Texture.Height;
                    newFace.Texture.XScale = 1-((maxU - minU) * v0u / newFace.Texture.Texture.Width);
                    newFace.Texture.YScale = 1+((maxV - minV) * v0v / newFace.Texture.Texture.Height);
                    // newFace.Texture.XScale *= dimU / newFace.Texture.Texture.Width;
                    // newFace.Texture.YScale *= dimV / newFace.Texture.Texture.Height;
                    // newFace.Texture.XScale *= v2u == v0u ? 1 : v2u - v0u;
                    // newFace.Texture.YScale *= v2v == v0v ? 1 : v2v - v0v;
                    if (newFace.Texture.XScale == 0) newFace.Texture.XScale = 1;
                    if (newFace.Texture.YScale == 0) newFace.Texture.YScale = 1;
                    newFace.Texture.XShift = -((v0u / (newFace.Texture.Texture.Width * newFace.Texture.XScale)) - minU) * newFace.Texture.Texture.Width;
                    newFace.Texture.YShift = -((v0v / (newFace.Texture.Texture.Height * newFace.Texture.YScale)) - minV) * newFace.Texture.Texture.Height;
                    newFace.Texture.XShift += newFace.Texture.Texture.Width * newFace.Texture.XScale;
                    newFace.Texture.YShift -= newFace.Texture.Texture.Height * newFace.Texture.YScale;
                    */
                    
                    // newFace.AlignTextureToFace();
                    newFace.CalculateTextureCoordinates(true);
#endif
                }
                if (newSolid.Faces.Any()) {
                    newSolid.SetParent(map.WorldSpawn);
                }

#if false
                if (newSolid.Faces.Any()) {
                    // TODO: textures
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
                                // f.Plane = new Plane(f.Vertices[0].Location, f.Vertices[1].Location, f.Vertices[2].Location);
                                // f.UpdateBoundingBox();
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

                                // f.AlignTextureToFace();

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
                    newSolid.SetParent(map.WorldSpawn);
                    // _document.ObjectRenderer.AddMapObject(newSolid);
                }
#endif
            }

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

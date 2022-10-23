#nullable enable
using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using CBRE.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Rectangle = System.Drawing.Rectangle;
using CBRE.Extensions;
using CBRE.DataStructures.Transformations;
using CBRE.FileSystem;
using CBRE.Providers.Model;
using CBRE.Common.Mediator;
using Vector3 = System.Numerics.Vector3;

namespace CBRE.Editor.Compiling.Lightmap {
    sealed partial class Lightmapper {
        public readonly Document Document;
        public readonly ImmutableArray<LMFace> OpaqueFaces;
        public readonly ImmutableArray<LMFace> TranslucentFaces;
        public readonly ImmutableArray<LMFace> ToolFaces;
        public readonly ImmutableArray<LMFace> UnclassifiedFaces;
        public readonly ImmutableArray<LightmapGroup> Groups;
        public readonly ImmutableArray<LMFace> ModelFaces;
        public ProgressPopup? progressPopup = null;

        public void UpdateProgress(string msg, float progress) {
            GameMain.Instance.PostDrawActions.Enqueue(() => {
                if (progressPopup == null || !GameMain.Instance.Popups.Contains(progressPopup)) {
                    progressPopup = new ProgressPopup("Lightmap Progress");
                    GameMain.Instance.Popups.Add(progressPopup);
                }
                progressPopup.message = msg;
                progressPopup.progress = progress;
            });
        }

        public Lightmapper(Document document) {
            Document = document;

            UpdateProgress("Gathering faces... (Step 1/3)", 0);

            var flattenedObjectList = Document.Map.WorldSpawn
                .GetSelfAndAllChildren();
            var solids = flattenedObjectList
                .OfType<Solid>();
            var models = flattenedObjectList
                .OfType<Entity>().Where(x => x.GameData != null && x.GameData.Behaviours.Any(p => p.Name == "model"));
            var allFaces = solids.SelectMany(s => s.Faces);
            
            List<LMFace> opaqueFaces = new();
            List<LMFace> translucentFaces = new();
            List<LMFace> toolFaces = new();
            List<LMFace> unclassifiedFaces = new();
            List<LMFace> modelFaces = new();
            foreach (var face in allFaces) {
                face.UpdateBoundingBox();
                
                LMFace lmFace = new LMFace(face);

                if (lmFace.Texture.Name.StartsWith("ToolTextures/", StringComparison.OrdinalIgnoreCase)) {
                    toolFaces.Add(lmFace);
                } else if (lmFace.Texture.Texture is { } texture) {
                    (texture.HasTransparency() ? translucentFaces : opaqueFaces)
                        .Add(lmFace);
                } else {
                    unclassifiedFaces.Add(lmFace);
                }
            }

            var modelsRefs = new Dictionary<string, ModelReference>();
            if (LightmapConfig.BakeModelLightmaps || LightmapConfig.BakeModels) {
                foreach (var model in models) {
                    DataStructures.Geometric.Vector3 euler = model.EntityData.GetPropertyVector3("angles", DataStructures.Geometric.Vector3.Zero);
                    DataStructures.Geometric.Vector3 scale = model.EntityData.GetPropertyVector3("scale", DataStructures.Geometric.Vector3.One);
                    bool shouldBake = model.EntityData.GetPropertyValue("bake")?.ToLowerInvariant() != "false";
                    DataStructures.Geometric.Matrix modelMat = model.LeftHandedWorldMatrix;
                                        /*DataStructures.Geometric.Matrix.Translation(model.Origin)
                                        * DataStructures.Geometric.Matrix.RotationX(DMath.DegreesToRadians(360-euler.X))
                                        * DataStructures.Geometric.Matrix.RotationY(DMath.DegreesToRadians(360-euler.Y))
                                        * DataStructures.Geometric.Matrix.RotationZ(DMath.DegreesToRadians(euler.Z))
                                        * DataStructures.Geometric.Matrix.Scale(scale.XZY());*/
                    string key = model.GameData.Behaviours.FirstOrDefault(p => p.Name == "model").Values.FirstOrDefault();
                    string path = Directories.GetModelPath(model.EntityData.GetPropertyValue(key));
                    if (string.IsNullOrWhiteSpace(path))
                        continue;
                    if (!shouldBake)
                        continue;
                    NativeFile file = new NativeFile(path);
                    if (ModelProvider.CanLoad(file) && !modelsRefs.ContainsKey(path)) {
                        ModelReference mref = ModelProvider.CreateModelReference(file);
                        modelsRefs.Add(path, mref);
                    }
                    if (modelsRefs.ContainsKey(path)) {
                        var transforms = modelsRefs[path].Model.GetTransforms();
                        foreach (var meshGroup in modelsRefs[path].Model.GetActiveMeshes().GroupBy(x => x.SkinRef)) {
                            var tex = modelsRefs[path].Model.Textures[meshGroup.Key];
                            foreach (var mesh in meshGroup) {
                                Face tFace = new Face(0);
                                tFace.Texture.Name = System.IO.Path.GetFileNameWithoutExtension(tex.Name);
                                tFace.Texture.Texture = tex.TextureObject;
                                tFace.Plane = new DataStructures.Geometric.Plane(DataStructures.Geometric.Vector3.UnitY, (decimal)1.0);
                                tFace.BoundingBox = Box.Empty;
                                Face mFace = tFace.Clone();
                                var verts = mesh.Vertices.Select(x => new DataStructures.MapObjects.Vertex(new DataStructures.Geometric.Vector3(x.Location * transforms[x.BoneWeightings.First().Bone.BoneIndex]) * modelMat, mFace) {
                                    TextureU = (decimal)(x.TextureU),
                                    TextureV = (decimal)(x.TextureV),
                                });
                                tFace.Vertices.Clear();
                                tFace.Vertices.AddRange(verts);
                                /*for (int i = 0; i < mesh.Vertices.Count - 1; i+=2) {
                                    tFace.Vertices.Add(verts.ElementAt(i+0));
                                    tFace.Vertices.Add(verts.ElementAt(i+1));
                                }*/
                                tFace.Plane = new DataStructures.Geometric.Plane(tFace.Vertices[0].Location, tFace.Vertices[1].Location, tFace.Vertices[2].Location);
                                tFace.Vertices.ForEach(v => { v.LMU = -500.0f; v.LMV = -500.0f; });
                                tFace.UpdateBoundingBox();
                                LMFace face = new LMFace(tFace.Clone());
                                BoxF faceBox = new BoxF(face.BoundingBox.Start - new Vector3F(3.0f, 3.0f, 3.0f), face.BoundingBox.End + new Vector3F(3.0f, 3.0f, 3.0f));
                                // opaqueFaces.Add(face);
                                modelFaces.Add(face);
                            }
                        }
                    }
                }
            }

            OpaqueFaces = opaqueFaces.ToImmutableArray();
            TranslucentFaces = translucentFaces.ToImmutableArray();
            ToolFaces = toolFaces.ToImmutableArray();
            UnclassifiedFaces = unclassifiedFaces.ToImmutableArray();
            ModelFaces = modelFaces.ToImmutableArray();

            List<LightmapGroup> groups = new();

            foreach (var face in OpaqueFaces) {
                LightmapGroup group = LightmapGroup.FindCoplanar(groups, face);
                if (group is null) {
                    group = new LightmapGroup();
                    groups.Add(group);
                }
                group.AddFace(face);
            }

            foreach (var face in ToolFaces) {
                if (face.Texture.Name.ToLowerInvariant() == "tooltextures/block_light") {
                    LightmapGroup group = LightmapGroup.FindCoplanar(groups, face);
                    if (group is null) {
                        group = new LightmapGroup();
                        groups.Add(group);
                    }
                    group.AddFace(face);
                }
            }

            List<LightmapGroup> modelGroups = new();

            foreach (var face in modelFaces) {
                LightmapGroup group = LightmapGroup.FindCoplanar(modelGroups, face);
                if (group is null) {
                    group = new LightmapGroup();
                    modelGroups.Add(group);
                }
                group.AddFace(face);
            }

            Groups = groups.Union(modelGroups).ToImmutableArray();
        }

        private async Task WaitForRender(string name, Action? action, CancellationToken token) {
            bool signal = false;
            TaskPool.Add(name, Task.Delay(100), (t) => {
                try {
                    action?.Invoke();
                }
                catch (Exception e) {
                    Mediator.Publish(EditorMediator.CompileFailed, Document);
                    Logging.Logger.ShowException(e);
                    GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
                }
                signal = true;
            });
            while (!signal) {
                await Task.Yield();
                token.ThrowIfCancellationRequested();
            }
        }

        public class Atlas : IDisposable {
            public readonly ImmutableHashSet<LightmapGroup> Groups;

            public VertexBuffer GroupVertices { get; private set; }
            public IndexBuffer GroupIndices { get; private set; }
            
            public VertexBuffer GeomVertices { get; private set; }
            public IndexBuffer GeomIndices { get; private set; }

            private int indexCount;

            public Atlas(IEnumerable<LightmapGroup> groups, int lmIndex) {
                Groups = groups.ToImmutableHashSet();
                foreach (var group in Groups) {
                    foreach (var face in group.Faces) {
                        face.UpdateLmUv(group, lmIndex);
                    }
                }
            }

            public void InitGpuBuffers() {
                
                var faces = Groups
                    .SelectMany(g => g.Faces)
                    .ToImmutableArray();
                var vertices = faces
                    .SelectMany(f => f.Vertices)
                    .ToImmutableArray();
                var indices = new List<uint>();
                long indexOffset = 0;
                foreach (var f in faces) {
                    indices.AddRange(
                        f.GetTriangleIndices()
                            .Select(i => (uint)(i+indexOffset)));
                    indexOffset += f.Vertices.Length;
                }
                
                var gd = GlobalGraphics.GraphicsDevice;

                GroupVertices = new VertexBuffer(
                    gd,
                    ObjectRenderer.BrushVertex.VertexDeclaration,
                    Groups.Count * 4,
                    BufferUsage.None);
                GroupIndices = new IndexBuffer(
                    gd,
                    IndexElementSize.ThirtyTwoBits,
                    Groups.Count * 6,
                    BufferUsage.None);
                
                GroupVertices.SetData(Groups
                    .SelectMany(g => g.GenQuadVerts())
                    .ToArray());
                GroupIndices.SetData(Enumerable.Range(0, Groups.Count)
                    .SelectMany(i => new[] {
                        i*4+0, i*4+1, i*4+2,
                        i*4+2, i*4+3, i*4+1
                    })
                    .Select(i => (uint)i)
                    .ToArray());
                
                GeomVertices = new VertexBuffer(
                    gd,
                    ObjectRenderer.BrushVertex.VertexDeclaration,
                    vertices.Length,
                    BufferUsage.None);
                GeomIndices = new IndexBuffer(
                    gd,
                    IndexElementSize.ThirtyTwoBits,
                    indices.Count,
                    BufferUsage.None);
                
                GeomVertices.SetData(vertices
                    .Select(v => new ObjectRenderer.BrushVertex(v.OriginalVertex))
                    .ToArray());
                GeomIndices.SetData(indices.ToArray());
                indexCount = indices.Count;
            }

            public void RenderGeom() {
                var gd = GlobalGraphics.GraphicsDevice;
                
                gd.SetVertexBuffer(GeomVertices);
                gd.Indices = GeomIndices;
                gd.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    0, 0, indexCount / 3);
            }

            public void RenderGroups() {
                var gd = GlobalGraphics.GraphicsDevice;

                gd.SetVertexBuffer(GroupVertices);
                gd.Indices = GroupIndices;
                gd.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    0, 0, Groups.Count * 2);
            }

            public void Dispose()
            {
                GeomVertices?.Dispose(); GeomVertices = null;
                GeomIndices?.Dispose(); GeomIndices = null;
                GroupVertices?.Dispose(); GroupVertices = null;
                GroupIndices?.Dispose(); GroupIndices = null;
            }

            ~Atlas() {
                Dispose();
            }
        }

        private record PointLight(Vector3 Location, float Range, Vector3 Color) {
            public PointLight(MapObject lightEntity) : this(default, default, default) {
                Location = lightEntity.BoundingBox.Center.ToXna();
                
                var data = lightEntity.GetEntityData();
                float getPropertyFloat(string key)
                    => float.TryParse(data.GetPropertyValue(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out float v)
                        ? v
                        : 0.0f;
                
                Range = getPropertyFloat("range");
                Color = data.GetPropertyVector3("color").ToXna() * getPropertyFloat("intensity") / 255.0f;
            }

            public Box BoundingBox => new(
                (Location - new Vector3(Range, Range, Range)).ToCbre(),
                (Location + new Vector3(Range, Range, Range)).ToCbre());
        }

        private record SpotLight(Vector3 Location, float Range, Vector3 Color, Vector3 Direction, float InnerConeAngle, float OuterConeAngle) {
            public SpotLight(MapObject lightEntity) : this(default, default, default, default, default, default) {
                Location = lightEntity.BoundingBox.Center.ToXna();
                var data = lightEntity.GetEntityData();
                DataStructures.Geometric.Vector3 angles = data.GetPropertyVector3("angles");
                var pitch = DataStructures.Geometric.Matrix.Rotation(DataStructures.Geometric.Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
                var yaw = DataStructures.Geometric.Matrix.Rotation(DataStructures.Geometric.Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
                var roll = DataStructures.Geometric.Matrix.Rotation(DataStructures.Geometric.Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));
                var m = new UnitMatrixMult(yaw * roll * pitch);
                Direction = m.Transform(DataStructures.Geometric.Vector3.UnitY).Normalise().ToXna();
                float getPropertyFloat(string key)
                    => float.TryParse(data.GetPropertyValue(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out float v)
                        ? v
                        : 0.0f;
                
                Range = getPropertyFloat("range");
                Color = data.GetPropertyVector3("color").ToXna() * getPropertyFloat("intensity") / 255.0f;
                InnerConeAngle = (float)Math.Cos(getPropertyFloat("innerconeangle") * (float)Math.PI / 180.0f);
                OuterConeAngle = (float)Math.Cos(getPropertyFloat("outerconeangle") * (float)Math.PI / 180.0f);
            }

            public Box BoundingBox => new(
                (Location - new Vector3(Range, Range, Range)).ToCbre(),
                (Location + new Vector3(Range, Range, Range)).ToCbre());
        }

        private ImmutableArray<PointLight> ExtractPointLights()
            => Document.Map.WorldSpawn.Find(
                    e => string.Equals(e.GetEntityData()?.Name, "light", StringComparison.OrdinalIgnoreCase))
                .Select(e => new PointLight(e))
                .ToImmutableArray();

        private ImmutableArray<SpotLight> ExtractSpotLights()
            => Document.Map.WorldSpawn.Find(
                    e => string.Equals(e.GetEntityData()?.Name, "spotlight", StringComparison.OrdinalIgnoreCase))
                .Select(e => new SpotLight(e))
                .ToImmutableArray();

        private ImmutableArray<Atlas> PrepareAtlases() {
            List<LightmapGroup> remainingGroups = Groups
                .OrderByDescending(g => g.WorldSpaceWidth * g.WorldSpaceHeight)
                .ThenByDescending(g => g.WorldSpaceWidth)
                .ThenByDescending(g => g.WorldSpaceHeight)
                .ToList();
            int maxGroups = remainingGroups.Count;

            List<Atlas> atlases = new();

            while (remainingGroups.Any()) {
                UpdateProgress("Calculating lightmap atlases... (Step 2/3)", 1f - ((float)remainingGroups.Count / maxGroups));
                int prevCount = remainingGroups.Count;
                
                var prevGroups = remainingGroups.ToArray();
                CalculateUv(
                    remainingGroups,
                    new Rectangle(
                        1,
                        1,
                        LightmapConfig.TextureDims-2,
                        LightmapConfig.TextureDims-2),
                    out _,
                    out _);

                if (prevCount == remainingGroups.Count) {
                    throw new Exception(
                        $"{prevCount} lightmap groups do not fit within the given resolution and downscale factor");
                }

                var newAtlas = new Atlas(prevGroups.Where(g => !remainingGroups.Contains(g)), atlases.Count);
                atlases.Add(newAtlas);
            }
            
            return atlases.ToImmutableArray();
        }
        
        

        private static void CalculateUv(
            List<LightmapGroup> lmGroups,
            Rectangle area,
            out int usedWidth,
            out int usedHeight
        ) {
            usedWidth = 0;
            usedHeight = 0;
            if (lmGroups.Count <= 0) { return; }

            for (int i = 0; i < lmGroups.Count; i++) {
                LightmapGroup lmGroup = lmGroups[i];

                //Make the aspect ratio of the group
                //closer to the aspect ratio of the
                //available area, since this gives
                //better odds of the group fitting
                if ((area.Width <= area.Height) != (lmGroup.WorldSpaceWidth <= lmGroup.WorldSpaceHeight)) {
                    lmGroup.SwapUv();
                }

                int downscaledWidth = 0;
                int downscaledHeight = 0;
                bool fits = false;
                
                for (int attempts = 0; attempts < 2; attempts++) {
                    downscaledWidth = (int)Math.Ceiling(lmGroup.UvSpaceWidth);
                    downscaledHeight = (int)Math.Ceiling(lmGroup.UvSpaceHeight);

                    if (downscaledWidth > area.Width || downscaledHeight > area.Height) {
                        //The group did not fit, try flipping the group
                        //because it might be able to fit that way
                        lmGroup.SwapUv();
                    } else {
                        fits = true;
                        break;
                    }
                }

                if (!fits) { continue; } //The given group simply does not fit in the given area, try the next one

                lmGroups.RemoveAt(i); //Remove the current group from the list of pending groups
                
                lmGroup.StartWriteUV.U = area.Left;
                lmGroup.StartWriteUV.V = area.Top;
                usedWidth += downscaledWidth;
                usedHeight += downscaledHeight;
                
                //There are now four regions that are considered to introduce more groups:
                //  XXXXXXXX | AAAAAAAA
                //  XXXXXXXX | AAAAAAAA
                //  XXXXXXXX | AAAAAAAA
                //  -------------------
                //  BBBBBBBB | CCCCCCCC
                //  BBBBBBBB | CCCCCCCC
                //  BBBBBBBB | CCCCCCCC
                //
                //Region X is completely taken up by the current group.
                //Regions A, B and C are extra space that should be filled,
                //their dimensions are based on the size of region X

                //Try to fill region A
                if (downscaledWidth < area.Width) {
                    int subWidth = -1;
                    usedWidth += LightmapConfig.PlaneMargin;
                    while (subWidth != 0) {
                        CalculateUv(lmGroups, new Rectangle(
                                area.Left + usedWidth,
                                area.Top,
                                area.Width - usedWidth,
                                downscaledHeight),
                            out subWidth, out _);
                        usedWidth += subWidth + LightmapConfig.PlaneMargin;
                    }
                }

                //Try to fill region B
                if (downscaledHeight < area.Height) {
                    int subHeight = -1;
                    usedHeight += LightmapConfig.PlaneMargin;
                    while (subHeight != 0) {
                        CalculateUv(lmGroups, new Rectangle(
                                area.Left,
                                area.Top + usedHeight,
                                downscaledWidth,
                                area.Height - usedHeight),
                            out _, out subHeight);
                        usedHeight += subHeight + LightmapConfig.PlaneMargin;
                    }
                }

                //Try to fill region C
                if (downscaledWidth < area.Width && downscaledHeight < area.Height) {
                    Rectangle remainder = new Rectangle(
                        area.Left + downscaledWidth + LightmapConfig.PlaneMargin,
                        area.Top + downscaledHeight + LightmapConfig.PlaneMargin,
                        area.Width - downscaledWidth - LightmapConfig.PlaneMargin,
                        area.Height - downscaledHeight - LightmapConfig.PlaneMargin);

                    CalculateUv(lmGroups, remainder,
                        out int subWidth, out int subHeight);

                    usedWidth = Math.Max(usedWidth, downscaledWidth + LightmapConfig.PlaneMargin + subWidth);
                    usedHeight = Math.Max(usedHeight, downscaledHeight + LightmapConfig.PlaneMargin + subHeight);
                }

                //We managed to fit at least one group, we're done here!
                return;
            }
        }
    }
}

using Assimp;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Models;
using CBRE.FileSystem;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Face = CBRE.DataStructures.MapObjects.Face;
using Mesh = Assimp.Mesh;
using Path = System.IO.Path;

namespace CBRE.Providers.Model {
    public class AssimpProvider : ModelProvider {
        protected static AssimpContext importer = null;

        protected override bool IsValidForFile(IFile file) {
            return file.Extension.ToLowerInvariant() == "b3d" ||
                   file.Extension.ToLowerInvariant() == "fbx" ||
                   file.Extension.ToLowerInvariant() == "x";
        }

        protected static void AddNode(Scene scene, Node node, DataStructures.Models.Model model, DataStructures.Models.Texture tex, Matrix4x4 parentMatrix) {
            Matrix4x4 selfMatrix = node.Transform * parentMatrix;
            foreach (var meshIndex in node.MeshIndices) {
                DataStructures.Models.Mesh sledgeMesh = AddMesh(model, scene.Meshes[meshIndex], selfMatrix);
                foreach (var v in sledgeMesh.Vertices) {
                    // This breaks model UVs
                    // v.TextureU *= tex.Width;
                    // v.TextureV *= tex.Height;
                }
                model.AddMesh("mesh", 0, sledgeMesh);
            }

            foreach (var subNode in node.Children) {
                AddNode(scene, subNode, model, tex, selfMatrix);
            }
        }

        protected static DataStructures.Models.Mesh AddMesh(DataStructures.Models.Model sledgeModel, Assimp.Mesh assimpMesh, Matrix4x4 selfMatrix) {
            var sledgeMesh = new DataStructures.Models.Mesh(0);
            List<MeshVertex> vertices = new List<MeshVertex>();

            for (int i = 0; i < assimpMesh.VertexCount; i++) {
                var assimpVertex = assimpMesh.Vertices[i];
                assimpVertex = selfMatrix * assimpVertex;
                var assimpNormal = assimpMesh.Normals[i];
                assimpNormal = selfMatrix * assimpNormal;
                var assimpUv = assimpMesh.TextureCoordinateChannels[0][i];

                vertices.Add(new MeshVertex(new Vector3F(assimpVertex.X, -assimpVertex.Z, assimpVertex.Y),
                                            new Vector3F(assimpNormal.X, -assimpNormal.Z, assimpNormal.Y),
                                            sledgeModel.Bones[0], assimpUv.X, -assimpUv.Y));
            }

            foreach (var face in assimpMesh.Faces) {
                var triInds = face.Indices;
                for (var i = 1; i < triInds.Count - 1; i++) {
                    sledgeMesh.Vertices.Add(vertices[triInds[0]]);
                    sledgeMesh.Vertices.Add(vertices[triInds[i + 1]]);
                    sledgeMesh.Vertices.Add(vertices[triInds[i]]);
                    continue;
                    sledgeMesh.Vertices.Add(new MeshVertex(vertices[triInds[0]].Location, vertices[triInds[0]].Normal, vertices[triInds[0]].BoneWeightings, vertices[triInds[0]].TextureU, vertices[triInds[0]].TextureV));
                    sledgeMesh.Vertices.Add(new MeshVertex(vertices[triInds[i + 1]].Location, vertices[triInds[i + 1]].Normal, vertices[triInds[i + 1]].BoneWeightings, vertices[triInds[i + 1]].TextureU, vertices[triInds[i + 1]].TextureV));
                    sledgeMesh.Vertices.Add(new MeshVertex(vertices[triInds[i]].Location, vertices[triInds[i]].Normal, vertices[triInds[i]].BoneWeightings, vertices[triInds[i]].TextureU, vertices[triInds[i]].TextureV));
                }
            }

            return sledgeMesh;
        }

        protected override DataStructures.Models.Model LoadFromFile(IFile file) {
            if (importer == null) {
                importer = new AssimpContext();
                //importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
            }

            DataStructures.Models.Model model = new DataStructures.Models.Model();
            DataStructures.Models.Bone bone = new DataStructures.Models.Bone(0, -1, null, "rootBone", Vector3F.Zero, Vector3F.Zero, Vector3F.One, Vector3F.One);
            model.Bones.Add(bone);

            Scene scene = importer.ImportFile(file.FullPathName);

            DataStructures.Models.Texture tex = null;

            if (scene.MaterialCount > 0) {
                //TODO: handle several textures
                for (int i = 0; i < scene.MaterialCount; i++) {
                    if (string.IsNullOrEmpty(scene.Materials[i].TextureDiffuse.FilePath)) { continue; }
                    string path = Path.Combine(Path.GetDirectoryName(file.FullPathName), scene.Materials[i].TextureDiffuse.FilePath);
                    if (!File.Exists(path)) { path = scene.Materials[i].TextureDiffuse.FilePath; }
                    if (File.Exists(path)) {
                        var titem = TextureProvider.GetItem(path);
                        AsyncTexture _tex = null;
                        if (titem == null) {
                            TexturePackage package = new TexturePackage(Path.GetDirectoryName(path), "");
                            var t = new TextureItem(package, Path.GetFileNameWithoutExtension(path), path);
                            package.AddTexture(t);
                            TextureProvider.Packages.Add(package);
                            _tex = t.Texture as AsyncTexture;
                        } else {
                            _tex = titem.Texture as AsyncTexture;
                        }
                        // AsyncTexture _tex = new AsyncTexture(path);
                        tex = new DataStructures.Models.Texture {
                            Name = path,
                            Index = 0,
                            Flags = 0,
                            TextureObject = _tex
                        };
                    }
                    break;
                }
            }

            if (tex == null) {
                AsyncTexture _tex = new AsyncTexture("___", Task.Run(() => {
                    return new AsyncTexture.Data {
                        Bytes = Enumerable.Repeat(0xff777777, 64 * 64).SelectMany(i => BitConverter.GetBytes(i)).ToArray(),
                        Width = 64,
                        Height = 64,
                        Compressed = false
                    };
                }));
                tex = new DataStructures.Models.Texture {
                    Name = "____",
                    Index = 0,
                    Flags = 0,
                    TextureObject = _tex
                };
            }

            model.Textures.Add(tex);

            AddNode(scene, scene.RootNode, model, tex, Matrix4x4.Identity);

            return model;
        }

        public static void SaveToFile(string filename, DataStructures.MapObjects.Map map, DataStructures.GameData.GameData gameData, string format) {
            Scene scene = new Scene();

            Node rootNode = new Node();
            rootNode.Name = "root";
            scene.RootNode = rootNode;

            Node newNode = new Node();

            Mesh mesh;
            int vertOffset;
            string[] textures = map.GetAllTextures().ToArray();
            foreach (string texture in textures) {
                if (texture == "tooltextures/remove_face") { continue; }

                Material material = new Material();
                material.Name = texture;
                TextureItem tex = TextureProvider.GetItem(texture);
                string texPath = Path.Combine(tex.Package.PackageRoot, Path.GetFileName(tex.Filename));
                TextureSlot textureSlot = new TextureSlot(Path.GetFileName(tex.Filename),
                    TextureType.Diffuse,
                    0,
                    TextureMapping.Plane,
                    0,
                    1.0f,
                    TextureOperation.Multiply,
                    Assimp.TextureWrapMode.Wrap,
                    Assimp.TextureWrapMode.Wrap,
                    0);
                material.AddMaterialTexture(textureSlot);
                string path = Path.Combine(Path.GetDirectoryName(typeof(AssimpProvider).Assembly.Location), textureSlot.FilePath);
                if (!File.Exists(path))
                    File.Copy(texPath, path);
                scene.Materials.Add(material);

                mesh = new Mesh();
                if (format != "obj") // .obj files should have no mesh names so they are one proper mesh
                {
                    mesh.Name = texture + "_mesh";
                }
                mesh.MaterialIndex = scene.MaterialCount - 1;
                vertOffset = 0;

                List<int> indices = new List<int>();

                IEnumerable<Face> faces = map.WorldSpawn.Find(x => x is Solid).
                    OfType<Solid>().
                    SelectMany(x => x.Faces).
                    Where(x => x.Texture.Name == texture);

                foreach (Face face in faces) {
                    foreach (Vertex v in face.Vertices.Reverse<Vertex>()) {
                        mesh.Vertices.Add(new Vector3D(-(float)v.Location.X, (float)v.Location.Z, (float)v.Location.Y));
                        mesh.Normals.Add(new Vector3D((float)face.Plane.Normal.X, (float)face.Plane.Normal.Z, (float)face.Plane.Normal.Y));
                        mesh.TextureCoordinateChannels[0].Add(new Vector3D((float)v.TextureU, -(float)v.TextureV, 0));
                    }
                    mesh.UVComponentCount[0] = 2;
                    foreach (uint ind in face.GetTriangleIndices()) {
                        indices.Add((int)ind + vertOffset);
                    }

                    vertOffset += face.Vertices.Count;
                }

                mesh.SetIndices(indices.ToArray(), 3);
                scene.Meshes.Add(mesh);

                newNode.MeshIndices.Add(scene.MeshCount - 1);
            }

            foreach (MapObject mapObject in map.WorldSpawn.GetSelfAndAllChildren()) {
                DataStructures.GameData.GameDataObject data = gameData.Classes.FirstOrDefault(p => p.Name == mapObject.ClassName);
                if (data == null) {
                    continue;
                }
                if (data.Name == "light" && mapObject is Entity ent) {
                    Vector3D vec = new Vector3D(-(float)ent.Origin.X, (float)ent.Origin.Z, (float)ent.Origin.Y);
                    Node node = new Node();
                    node.Name = "Light" + scene.LightCount;
                    node.Transform = Matrix4x4.FromTranslation(vec);
                    rootNode.Children.Add(node);
                    Light lightNode = new Light();
                    lightNode.LightType = LightSourceType.Point;
                    lightNode.Position = vec;
                    lightNode.AngleInnerCone = MathF.PI * 2f;
                    lightNode.AngleOuterCone = MathF.PI * 2f;
                    Vector3 color = ent.EntityData.GetPropertyVector3("color");
                    lightNode.ColorDiffuse = new Color3D((float)color.X, (float)color.Y, (float)color.Z) / 255f;
                    lightNode.ColorAmbient = new Color3D(0f, 0f, 0f);
                    lightNode.ColorSpecular = new Color3D(1f, 1f, 1f);
                    lightNode.AttenuationConstant = 1f;
                    lightNode.AttenuationLinear = float.Parse(ent.EntityData.GetPropertyValue("range"));
                    lightNode.AttenuationQuadratic = 1f;
                    lightNode.Name = "Light" + scene.LightCount;
                    scene.Lights.Add(lightNode);
                    Console.WriteLine(scene.LightCount.ToString());
                }
            }

            Console.WriteLine(scene.HasLights.ToString());

            rootNode.Children.Add(newNode);

            AssimpContext ctx = new AssimpContext();
            ctx.ExportFile(scene, filename, format);
        }
    }
}

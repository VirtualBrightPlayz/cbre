using CBRE.DataStructures.Geometric;
using System.Collections.Generic;

namespace CBRE.DataStructures.Models {
    public class MeshVertex {
        public Vector3F Location { get; set; }
        public Vector3F Normal { get; set; }
        public IEnumerable<BoneWeighting> BoneWeightings { get; private set; }
        public float TextureU { get; set; }
        public float TextureV { get; set; }

        public MeshVertex(Vector3F location, Vector3F normal, IEnumerable<BoneWeighting> boneWeightings, float textureU, float textureV) {
            Location = location;
            Normal = normal;
            BoneWeightings = boneWeightings;
            TextureU = textureU;
            TextureV = textureV;
        }

        public MeshVertex(Vector3F location, Vector3F normal, Bone bone, float textureU, float textureV) {
            Location = location;
            Normal = normal;
            BoneWeightings = new List<BoneWeighting> { new BoneWeighting(bone, 1) };
            TextureU = textureU;
            TextureV = textureV;
        }
    }
}
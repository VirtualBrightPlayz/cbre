using CBRE.DataStructures.Geometric;

namespace CBRE.DataStructures.Models {
    public class Bone {
        public int BoneIndex { get; private set; }
        public int ParentIndex { get; private set; }
        public Bone Parent { get; private set; }
        public string Name { get; private set; }
        public Vector3F DefaultPosition { get; private set; }
        public Vector3F DefaultAngles { get; private set; }
        public Vector3F DefaultPositionScale { get; private set; }
        public Vector3F DefaultAnglesScale { get; private set; }
        public MatrixF Transform { get; private set; }

        public Bone(int boneIndex, int parentIndex, Bone parent, string name,
                    Vector3F defaultPosition, Vector3F defaultAngles,
                    Vector3F defaultPositionScale, Vector3F defaultAnglesScale) {
            BoneIndex = boneIndex;
            ParentIndex = parentIndex;
            Parent = parent;
            Name = name;
            DefaultPosition = defaultPosition;
            DefaultAngles = defaultAngles;
            DefaultPositionScale = defaultPositionScale;
            DefaultAnglesScale = defaultAnglesScale;
            Transform = QuaternionF.EulerAngles(DefaultAngles).GetMatrix().Translate(defaultPosition);
            if (parent != null) Transform *= parent.Transform;
        }
    }
}
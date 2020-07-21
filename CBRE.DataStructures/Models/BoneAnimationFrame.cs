using CBRE.DataStructures.Geometric;

namespace CBRE.DataStructures.Models {
    public class BoneAnimationFrame {
        public Bone Bone { get; private set; }
        public Vector3F Position { get; private set; }
        public QuaternionF Angles { get; private set; }

        public BoneAnimationFrame(Bone bone, Vector3F position, QuaternionF angles) {
            Bone = bone;
            Position = position;
            Angles = angles;
        }
    }
}
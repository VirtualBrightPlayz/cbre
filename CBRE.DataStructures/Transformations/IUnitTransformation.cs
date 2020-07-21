using CBRE.DataStructures.Geometric;
using System.Runtime.Serialization;

namespace CBRE.DataStructures.Transformations {
    public interface IUnitTransformation : ISerializable {
        Vector3 Transform(Vector3 c);
        Vector3F Transform(Vector3F c);
    }
}

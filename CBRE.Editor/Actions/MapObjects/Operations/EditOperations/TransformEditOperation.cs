using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Transformations;

namespace CBRE.Editor.Actions.MapObjects.Operations.EditOperations {
    public class TransformEditOperation : IEditOperation {
        private readonly IUnitTransformation _transformation;
        private readonly TransformFlags _transformFlags;

        public bool ClearVisgroups { get; set; }

        public TransformEditOperation(IUnitTransformation transformation, TransformFlags transformFlags) {
            _transformation = transformation;
            _transformFlags = transformFlags;
        }

        public void PerformOperation(MapObject mo) {
            mo.Transform(_transformation, _transformFlags);

            if (ClearVisgroups) {
                foreach (var o in mo.FindAll()) o.Visgroups.Clear();
            }
        }
    }
}
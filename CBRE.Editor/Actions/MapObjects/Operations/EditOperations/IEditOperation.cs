using CBRE.DataStructures.MapObjects;

namespace CBRE.Editor.Actions.MapObjects.Operations.EditOperations {
    public interface IEditOperation {
        void PerformOperation(MapObject mo);
    }
}

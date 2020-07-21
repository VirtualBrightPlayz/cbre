using CBRE.DataStructures.Geometric;

namespace CBRE.UI {
    public interface IViewport2DEventListener : IViewportEventListener {
        void ZoomChanged(decimal oldZoom, decimal newZoom);
        void PositionChanged(Vector3 oldPosition, Vector3 newPosition);
    }
}

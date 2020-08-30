namespace CBRE.Editor.Rendering {
    public class FrameInfo {
        public long Milliseconds { get; private set; }

        public FrameInfo(long milliseconds) {
            Milliseconds = milliseconds;
        }
    }
}

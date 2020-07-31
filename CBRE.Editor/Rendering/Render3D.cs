using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Rendering {
    public class Render3D {
        private static VertexPositionNormalTexture[] Vertices = null;
        private static UInt16[] Indices = null;

        public BasicEffect BasicEffect;
        public Document Document;

        private class BrushGeometry {
            public VertexBuffer VertexBuffer = null;
            public IndexBuffer IndexBuffer = null;

            public List<Face> Faces = new List<Face>();
        }

        private Dictionary<ITexture, BrushGeometry> brushGeom = new Dictionary<ITexture, BrushGeometry>();
        private Dictionary<Face, ITexture> currentFaceTextures = new Dictionary<Face, ITexture>();

        public Render3D(Document doc) {
            Document = doc;
        }

        public void AddFace(Face face) {
            
        }
    }
}

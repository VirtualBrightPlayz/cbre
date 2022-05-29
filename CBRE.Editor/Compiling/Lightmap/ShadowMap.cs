#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CBRE.Graphics;
using CBRE.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Compiling.Lightmap;

sealed partial class Lightmapper {
    private class ShadowMap : IDisposable {
        private Matrix projectionMatrix;
        private float lightRange;

        public readonly Matrix[] ProjectionViewMatrices;

        private readonly ImmutableArray<Matrix> baseViewMatrices;

        private Effect depthEffect;

        public readonly RenderTarget2D[] RenderTargets;

        public ShadowMap(int rtResolution) {
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathF.Tau / 4.0f, 1.0f, 0.001f, 1.0f);

            var lookAt = new Vector3[] {
                new(0, 1, 0),
                new(1, 0, 0),
                new(0, -1, 0),
                new(-1, 0, 0),
                new(0, 0, 1),
                new(0, 0, -1)
            };
            ProjectionViewMatrices = new Matrix[6];
            for (int i = 0; i < 6; i++) {
                ProjectionViewMatrices[i] = Matrix.CreateLookAt(
                    Vector3.Zero,
                    lookAt[i],
                    Math.Abs(lookAt[i].Z) > 0.01f ? Vector3.UnitX : Vector3.UnitZ);
            }

            baseViewMatrices = ProjectionViewMatrices.ToImmutableArray();

            depthEffect = GlobalGraphics.LoadEffect("Shaders/depth.mgfx");

            RenderTargets = new RenderTarget2D[6];
            for (int i = 0; i < RenderTargets.Length; i++) {
                RenderTargets[i] = new RenderTarget2D(
                    GlobalGraphics.GraphicsDevice,
                    rtResolution,
                    rtResolution,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Single,
                    preferredDepthFormat: DepthFormat.Depth24Stencil8);
            }
        }

        public void Prepare(int viewMatrixIndex) {
            depthEffect.Parameters["ProjectionView"].SetValue(ProjectionViewMatrices[viewMatrixIndex]);
            depthEffect.Parameters["World"].SetValue(Matrix.Identity);
            depthEffect.Parameters["maxDepth"].SetValue(lightRange);

            depthEffect.CurrentTechnique.Passes[0].Apply();

            GlobalGraphics.GraphicsDevice.SetRenderTarget(RenderTargets[viewMatrixIndex]);
            GlobalGraphics.GraphicsDevice.Clear(
                ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil,
                new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                1.0f, 0);
        }

        public void SetLight(in PointLight light) {
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathF.Tau / 4.0f, 1.0f, 1.0f, light.Range);
            lightRange = light.Range;

            Vector3 lightXzy = light.Location.ToCbre().XYZ().ToXna();
            var translation = Matrix.CreateTranslation(-lightXzy);
            for (int i = 0; i < 6; i++) {
                ProjectionViewMatrices[i] = (translation * baseViewMatrices[i]) * projectionMatrix;
            }
        }

        public void Dispose() {
            for (int i = 0; i < 6; i++) {
                RenderTargets[i].Dispose();
            }
        }
    }

    public void RenderShadowMapped() {
        var pointLights = ExtractPointLights();
        
        using Effect lmLightCalc = GlobalGraphics.LoadEffect("Shaders/lmLightCalc.mgfx");

        var gd = GlobalGraphics.GraphicsDevice;

        if (Document.MGLightmaps is not null) {
            foreach (var lm in Document.MGLightmaps) {
                lm.Dispose();
            }
            Document.MGLightmaps = null;
        }
        
        var atlases = PrepareAtlases();
        foreach (var atlas in atlases)
        {
            atlas.InitGpuBuffers();
        }

        void renderAllAtlases() {
            foreach (var atlas in atlases) {
                atlas.RenderGeom();
            }
        }

        void saveTexture(string filePath, Texture2D texture) {
            using var fileSaveStream = File.Open(filePath, FileMode.Create);
            texture.SaveAsPng(fileSaveStream, texture.Width, texture.Height);
        }
        
        for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++) {
            var atlas = atlases[atlasIndex];

            RenderTarget2D atlasTexture = new RenderTarget2D(
                gd,
                LightmapConfig.TextureDims,
                LightmapConfig.TextureDims,
                mipMap: false,
                preferredFormat: SurfaceFormat.Color, /* TODO: HDR */
                preferredDepthFormat: DepthFormat.None,
                preferredMultiSampleCount: 0,
                usage: RenderTargetUsage.PreserveContents);
            Document.MGLightmaps ??= new List<Texture2D>();
            Document.MGLightmaps.Add(atlasTexture);
            
            gd.SetRenderTarget(atlasTexture);
            gd.Clear(Color.Black);

            using var shadowMap = new ShadowMap(LightmapConfig.TextureDims);
            
            for (int i = 0; i < pointLights.Length; i++) {
                var pointLight = pointLights[i];

                shadowMap.SetLight(pointLight);
                GlobalGraphics.GraphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
                GlobalGraphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                for (int j = 0; j < 6; j++) {
                    shadowMap.Prepare(j);
                    GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    renderAllAtlases();
                    //saveTexture($"shadowMap_{i}_{j}.png", shadowMap.RenderTargets[j]);
                }
                
                gd.SetRenderTarget(atlasTexture);

                gd.BlendState = i == 0 ? BlendState.NonPremultiplied : BlendState.Additive;

                lmLightCalc.Parameters["lightPos"].SetValue(pointLight.Location);
                lmLightCalc.Parameters["lightRange"].SetValue(pointLight.Range);
                lmLightCalc.Parameters["lightColor"].SetValue(new Vector4(pointLight.Color, 1.0f));
                lmLightCalc.Parameters["shadowMapTexelSize"].SetValue(1.0f / LightmapConfig.TextureDims);
                for (int j = 0; j < 6; j++) {
                    lmLightCalc.Parameters[$"lightProjView{j}"].SetValue(shadowMap.ProjectionViewMatrices[j]);
                    lmLightCalc.Parameters[$"lightShadowMap{j}"].SetValue(shadowMap.RenderTargets[j]);
                }

                lmLightCalc.CurrentTechnique.Passes[0].Apply();

                GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                atlas.RenderGroups();
            }
            
            saveTexture($"atlas_{atlasIndex}.png", atlasTexture);
            
            gd.SetRenderTarget(null);
            gd.BlendState = BlendState.NonPremultiplied;
        }
        
        Document.ObjectRenderer.MarkDirty();
    }
        
}

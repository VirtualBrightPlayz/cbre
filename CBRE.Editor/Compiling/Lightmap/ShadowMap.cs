﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CBRE.Common;
using CBRE.Common.Mediator;
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

        public void SetLight(in SpotLight light) {
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

    public async Task RenderShadowMapped(bool debug = false) {
        CancellationToken token = new CancellationToken();

        await WaitForRender("ShadowMap Init", null, token);

        var pointLights = ExtractPointLights();
        var spotLights = ExtractSpotLights();

        Effect? lmLightCalc = null;
        Effect? lmBlur = null;
        ImmutableArray<Atlas> atlases = new ImmutableArray<Atlas>();
        var gd = GlobalGraphics.GraphicsDevice;

        atlases = PrepareAtlases();

        await WaitForRender("ShadowMap UV coords", () => {
            lmLightCalc = GlobalGraphics.LoadEffect("Shaders/lmLightCalc.mgfx");
            lmBlur = GlobalGraphics.LoadEffect("Shaders/lmBlur.mgfx");

            if (Document.MGLightmaps is not null) {
                foreach (var lm in Document.MGLightmaps) {
                    lm.Dispose();
                }
                Document.MGLightmaps = null;
            }
            foreach (var face in Document.BakedFaces) {
                Document.ObjectRenderer.RemoveFace(face);
            }
            Document.BakedFaces.Clear();
            
            foreach (var atlas in atlases)
            {
                atlas.InitGpuBuffers();
            }

            gd.SetRenderTarget(null);
        }, token);

        void renderAllAtlases() {
            foreach (var atlas in atlases) {
                atlas.RenderGeom();
            }
        }

        void saveTexture(string filePath, Texture2D texture) {
            if (texture.Format != SurfaceFormat.Vector4) {
                string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", filePath);
                using var fileSaveStream = File.Open(fname, FileMode.Create);
                texture.SaveAsPng(fileSaveStream, texture.Width, texture.Height);
            } else {
                using RenderTarget2D rt = new RenderTarget2D(GlobalGraphics.GraphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                GlobalGraphics.GraphicsDevice.SetRenderTarget(rt);
                using var effect = new BasicEffect(GlobalGraphics.GraphicsDevice);
                effect.TextureEnabled = true;
                effect.Texture = texture;
                effect.CurrentTechnique.Passes[0].Apply();
                PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                PrimitiveDrawing.Vertex2(-1f, -1f, 0f, 1f);
                PrimitiveDrawing.Vertex2(1f, -1f, 1f, 1f);
                PrimitiveDrawing.Vertex2(1f, 1f, 1f, 0f);
                PrimitiveDrawing.Vertex2(-1f, 1f, 0f, 0f);
                PrimitiveDrawing.End();
                GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
                string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", filePath);
                using var fileSaveStream = File.Open(fname, FileMode.Create);
                rt.SaveAsPng(fileSaveStream, rt.Width, rt.Height);
            }
        }

        async Task saveTextureAsync(string filePath, Texture2D texture) {
            await WaitForRender("ShadowMap save texture", () => {
                saveTexture(filePath, texture);
            }, token);
        }
        
        UpdateProgress("Calculating brightness levels... (Step 3/3)", 0);
        int progressCount = 0;
        int progressMax = atlases.Length * (pointLights.Length + spotLights.Length);
        for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++) {
            var atlas = atlases[atlasIndex];

            RenderTarget2D atlasTexture = null;
            ShadowMap shadowMap = null;
            await WaitForRender("ShadowMap prepare atlasTexture", () => {
                atlasTexture = new RenderTarget2D(
                    gd,
                    LightmapConfig.TextureDims,
                    LightmapConfig.TextureDims,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Vector4,
                    preferredDepthFormat: DepthFormat.None,
                    preferredMultiSampleCount: 0,
                    usage: RenderTargetUsage.PreserveContents);
                Document.MGLightmaps ??= new List<Texture2D>();
                // Document.MGLightmaps.Add(atlasTexture);
                
                gd.SetRenderTarget(atlasTexture);
                gd.Clear(Color.Black);
                gd.SetRenderTarget(null);
                
                shadowMap = new ShadowMap(LightmapConfig.ShadowTextureDims);
                gd.SetRenderTarget(null);
            }, token);

            bool hasRun = true;

            for (int i = 0; i < pointLights.Length; i++) {
                await WaitForRender($"Render point light {i}", () => {
                    var pointLight = pointLights[i];

                    shadowMap.SetLight(pointLight);
                    GlobalGraphics.GraphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
                    GlobalGraphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    for (int j = 0; j < 6; j++) {
                        shadowMap.Prepare(j);
                        GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                        renderAllAtlases();
                        if (debug)
                            saveTexture($"shadowMap_0_{i}_{j}.png", shadowMap.RenderTargets[j]);
                    }
                    
                    gd.SetRenderTarget(atlasTexture);

                    gd.BlendState = hasRun ? BlendState.NonPremultiplied : BlendState.Additive;
                    hasRun = false;

                    lmLightCalc.Parameters["lightType"].SetValue(0);
                    lmLightCalc.Parameters["lightPos"].SetValue(pointLight.Location);
                    lmLightCalc.Parameters["lightRange"].SetValue(pointLight.Range);
                    lmLightCalc.Parameters["lightColor"].SetValue(new Vector4(pointLight.Color, 1.0f));
                    lmLightCalc.Parameters["lightDirection"].SetValue(Vector3.Zero);
                    lmLightCalc.Parameters["lightConeAngles"].SetValue(Vector2.Zero);
                    lmLightCalc.Parameters["shadowMapTexelSize"].SetValue(1.0f / LightmapConfig.TextureDims);
                    // lmLightCalc.Parameters["blurRadius"].SetValue(LightmapConfig.BlurRadius);
                    for (int j = 0; j < 6; j++) {
                        lmLightCalc.Parameters[$"lightProjView{j}"].SetValue(shadowMap.ProjectionViewMatrices[j]);
                        lmLightCalc.Parameters[$"lightShadowMap{j}"].SetValue(shadowMap.RenderTargets[j]);
                    }

                    lmLightCalc.CurrentTechnique.Passes[0].Apply();

                    GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                    atlas.RenderGroups();

                    gd.SetRenderTarget(null);
                    gd.BlendState = BlendState.NonPremultiplied;
                }, token);
                progressCount++;
                UpdateProgress("Calculating brightness levels... (Step 3/3)", (float)progressCount / progressMax);
                // UpdateProgress(progressCount.ToString() + "/" + progressMax.ToString() + " complete", 0.05f + ((float)progressCount / (float)progressMax) * 0.85f);
            }
            
            for (int i = 0; i < spotLights.Length; i++) {
                await WaitForRender($"Render spot light {i}", () => {
                    var spotLight = spotLights[i];

                    shadowMap.SetLight(spotLight);
                    GlobalGraphics.GraphicsDevice.BlendFactor = Microsoft.Xna.Framework.Color.White;
                    GlobalGraphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    for (int j = 0; j < 6; j++) {
                        shadowMap.Prepare(j);
                        GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                        renderAllAtlases();
                        if (debug)
                            saveTexture($"shadowMap_1_{i}_{j}.png", shadowMap.RenderTargets[j]);
                    }
                    
                    gd.SetRenderTarget(atlasTexture);

                    gd.BlendState = hasRun ? BlendState.NonPremultiplied : BlendState.Additive;
                    hasRun = false;

                    lmLightCalc.Parameters["lightType"].SetValue(1);
                    lmLightCalc.Parameters["lightPos"].SetValue(spotLight.Location);
                    lmLightCalc.Parameters["lightRange"].SetValue(spotLight.Range);
                    lmLightCalc.Parameters["lightColor"].SetValue(new Vector4(spotLight.Color, 1.0f));
                    lmLightCalc.Parameters["lightDirection"].SetValue(spotLight.Direction);
                    lmLightCalc.Parameters["lightConeAngles"].SetValue(new Vector2(spotLight.InnerConeAngle, spotLight.OuterConeAngle));
                    lmLightCalc.Parameters["shadowMapTexelSize"].SetValue(1.0f / LightmapConfig.TextureDims);
                    for (int j = 0; j < 6; j++) {
                        lmLightCalc.Parameters[$"lightProjView{j}"].SetValue(shadowMap.ProjectionViewMatrices[j]);
                        lmLightCalc.Parameters[$"lightShadowMap{j}"].SetValue(shadowMap.RenderTargets[j]);
                    }

                    lmLightCalc.CurrentTechnique.Passes[0].Apply();

                    GlobalGraphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                    atlas.RenderGroups();

                    gd.SetRenderTarget(null);
                    gd.BlendState = BlendState.NonPremultiplied;
                }, token);
                progressCount++;
                UpdateProgress("Calculating brightness levels... (Step 3/3)", (float)progressCount / progressMax);
                // UpdateProgress(progressCount.ToString() + "/" + progressMax.ToString() + " complete", 0.05f + ((float)progressCount / (float)progressMax) * 0.85f);
            }

            if (debug)
                await saveTextureAsync($"atlas_{atlasIndex}.png", atlasTexture);
            await WaitForRender("ShadowMap blur texture", () => {
                RenderTarget2D rt = new RenderTarget2D(GlobalGraphics.GraphicsDevice, atlasTexture.Width, atlasTexture.Height, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                gd.SetRenderTarget(rt);
                lmBlur.Parameters["shadowMapTexelSize"].SetValue(1.0f / LightmapConfig.TextureDims);
                lmBlur.Parameters["blurRadius"].SetValue(LightmapConfig.BlurRadius);
                lmBlur.Parameters["xTexture"].SetValue(atlasTexture);
                lmBlur.CurrentTechnique.Passes[0].Apply();
                PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                PrimitiveDrawing.Vertex2(-1f, -1f, 0f, 1f);
                PrimitiveDrawing.Vertex2(1f, -1f, 1f, 1f);
                PrimitiveDrawing.Vertex2(1f, 1f, 1f, 0f);
                PrimitiveDrawing.Vertex2(-1f, 1f, 0f, 0f);
                PrimitiveDrawing.End();
                gd.SetRenderTarget(null);
                gd.BlendState = BlendState.NonPremultiplied;
                saveTexture($"atlas_blur_{atlasIndex}.png", rt);
                Document.MGLightmaps.Add(rt);
                atlasTexture.Dispose();
            }, token);
            // await saveTextureAsync($"atlas_blur_{atlasIndex}.png", atlasTexture);

            await WaitForRender("Cleanup", () => {
                gd.SetRenderTarget(null);
                gd.BlendState = BlendState.NonPremultiplied;
            }, token);
        }
        
        UpdateProgress("Lightmapping complete!", 1.0f);
        await WaitForRender("Cleanup", () => {
            foreach (var face in ModelFaces) {
                Document.BakedFaces.Add(face.OriginalFace);
                Document.ObjectRenderer.AddFace(face.OriginalFace);
            }
            Document.ObjectRenderer.MarkDirty();
            foreach (var atlas in atlases) {
                atlas?.Dispose();
            }
        }, token);
    }
        
}

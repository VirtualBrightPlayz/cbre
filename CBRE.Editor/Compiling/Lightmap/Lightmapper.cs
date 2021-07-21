﻿using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using CBRE.Settings;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CBRE.Editor.Compiling.Lightmap {
    static class Lightmapper {
        struct LMThreadException {
            public LMThreadException(Exception e) {
                Message = e.Message;
                StackTrace = e.StackTrace;
            }

            public string Message;
            public string StackTrace;
        }

        public static List<Thread> FaceRenderThreads { get; private set; }
        private static List<LMThreadException> threadExceptions;
        private static ProgressPopup progressPopup = null;

        private static void UpdateProgress(string msg, float progress) {
            GameMain.Instance.PreDrawActions.Enqueue(() => {
                if (progressPopup == null || !GameMain.Instance.Popups.Contains(progressPopup)) {
                    progressPopup = new ProgressPopup("Lightmap Progress");
                    // progressPopup.Run();
                    // Task.Run(progressPopup.Run);
                }
                progressPopup.message = msg;
                progressPopup.progress = progress;
                // progressPopup.RunOneFrame();
                // progressPopup.ResetElapsedTime();
                /*if (altGUIRenderer == null) {
                    altGUIRenderer = new ImGuiRenderer(GameMain.Instance);
                }*/
                // GameMain.Instance.GraphicsDevice.Clear(new Microsoft.Xna.Framework.Color(50, 50, 60));
                // altGUIRenderer.BeforeLayout(new Microsoft.Xna.Framework.GameTime(default, TimeSpan.FromTicks(0)));
                // progressPopup.Draw();
                // altGUIRenderer.AfterLayout();
                // GameMain.Instance.LimitedRedraw = progress < 1f;
                // if (GameMain.Instance.LimitedRedraw)
                    // GameMain.Instance.Tick();
                // if (GameMain.Instance.LimitedRedraw)
                    // GameMain.Instance.RunOneFrame();
            });
        }

        private static void CalculateUV(List<LightmapGroup> lmGroups, Rectangle area, out int usedWidth, out int usedHeight) {
            usedWidth = 0;
            usedHeight = 0;
            if (lmGroups.Count <= 0) { return; }

            for (int i = 0; i < lmGroups.Count; i++) {
                LightmapGroup lmGroup = lmGroups[i];

                if ((area.Width <= area.Height) != (lmGroup.Width <= lmGroup.Height)) {
                    lmGroup.SwapUV();
                }

                for (int j = 0; j < 2; j++) {
                    int downscaledWidth = (int)Math.Ceiling(lmGroup.Width / LightmapConfig.DownscaleFactor);
                    int downscaledHeight = (int)Math.Ceiling(lmGroup.Height / LightmapConfig.DownscaleFactor);

                    if (downscaledWidth <= area.Width && downscaledHeight <= area.Height) {
                        usedWidth += downscaledWidth;
                        usedHeight += downscaledHeight;
                        lmGroups.RemoveAt(i);
                        lmGroup.writeX = area.Left;
                        lmGroup.writeY = area.Top;

                        int subWidth = -1; int subHeight = -1;
                        if (downscaledWidth < area.Width) {
                            int subUsedWidth = 0;
                            while (subWidth != 0) {
                                CalculateUV(lmGroups, new Rectangle(area.Left + subUsedWidth + downscaledWidth + LightmapConfig.PlaneMargin,
                                                                    area.Top,
                                                                    area.Width - subUsedWidth - downscaledWidth - LightmapConfig.PlaneMargin,
                                                                    downscaledHeight),
                                            out subWidth, out subHeight);
                                subUsedWidth += subWidth + LightmapConfig.PlaneMargin;
                            }

                            usedWidth += subUsedWidth;
                            subWidth = -1; subHeight = -1;
                        }

                        if (downscaledHeight < area.Height) {
                            int subUsedHeight = 0;
                            while (subHeight != 0) {
                                CalculateUV(lmGroups, new Rectangle(area.Left,
                                                                    area.Top + subUsedHeight + downscaledHeight + LightmapConfig.PlaneMargin,
                                                                    downscaledWidth,
                                                                    area.Height - subUsedHeight - downscaledHeight - LightmapConfig.PlaneMargin),
                                            out subWidth, out subHeight);
                                subUsedHeight += subHeight + LightmapConfig.PlaneMargin;
                            }

                            usedHeight += subUsedHeight;
                        }

                        if (downscaledWidth < area.Width && downscaledHeight < area.Height) {
                            Rectangle remainder = new Rectangle(area.Left + downscaledWidth + LightmapConfig.PlaneMargin,
                                                            area.Top + downscaledHeight + LightmapConfig.PlaneMargin,
                                                            area.Width - downscaledWidth - LightmapConfig.PlaneMargin,
                                                            area.Height - downscaledHeight - LightmapConfig.PlaneMargin);

                            CalculateUV(lmGroups, remainder,
                                            out subWidth, out subHeight);

                            usedWidth += subWidth;
                            usedHeight += subHeight;
                        }

                        return;
                    }

                    lmGroup.SwapUV();
                }
            }
        }

        public static readonly RenderTarget2D[] Lightmaps = new RenderTarget2D[4];

        public static void Render(Document document, out List<LMFace> faces, out int lmCount) {
            var map = document.Map;

            faces = new List<LMFace>();
            var lightEntities = new List<Light>();

            threadExceptions = new List<LMThreadException>();

            List<LightmapGroup> lmGroups = new List<LightmapGroup>();
            List<LMFace> exclusiveBlockers = new List<LMFace>();

            //get faces
            UpdateProgress("Determining UV coordinates...", 0);
            LMFace.FindFacesAndGroups(map, out faces, out lmGroups);

            if (!lmGroups.Any()) { throw new Exception("No lightmap groups!"); }

            foreach (LMFace lmface in faces) {
                lmface.OriginalFace.LmIndex = lmface.LmIndex;
            }

            foreach (Solid solid in map.WorldSpawn.Find(x => x is Solid).OfType<Solid>()) {
                foreach (Face tface in solid.Faces) {
                    LMFace face = new LMFace(tface, solid);
                    if (tface.Texture.Name.ToLower() != "tooltextures/block_light") continue;
                    exclusiveBlockers.Add(face);
                }
            }

            for (int i = 0; i < lmGroups.Count; i++) {
                for (int j = i + 1; j < lmGroups.Count; j++) {
                    if ((lmGroups[i].Plane.Normal - lmGroups[j].Plane.Normal).LengthSquared() < 0.001f &&
                        lmGroups[i].BoundingBox.IntersectsWith(lmGroups[j].BoundingBox)) {
                        lmGroups[i].Faces.AddRange(lmGroups[j].Faces);
                        lmGroups[i].BoundingBox = new BoxF(new BoxF[] { lmGroups[i].BoundingBox, lmGroups[j].BoundingBox });
                        lmGroups.RemoveAt(j);
                        j = i + 1;
                    }
                }
            }

            //put the faces into the bitmap
            lmGroups.Sort((x, y) => {
                if (x.Width == y.Width) {
                    if (x.Height == y.Height) { return 0; }
                    if (x.Height < y.Height) { return 1; }
                    return -1;
                }

                if (x.Width < y.Width) { return 1; }
                return -1;
            });

            FaceRenderThreads = new List<Thread>();

            Light.FindLights(map, out lightEntities);

            List<LMFace> allBlockers = lmGroups.Select(q => q.Faces).SelectMany(q => q).Where(f => f.CastsShadows).Union(exclusiveBlockers).ToList();
            int faceCount = 0;

            List<LightmapGroup> uvCalcFaces = new List<LightmapGroup>(lmGroups);

            int totalTextureDims = LightmapConfig.TextureDims;
            lmCount = 0;
            for (int i = 0; i < 4; i++) {
                int x = 1 + ((i % 2) * LightmapConfig.TextureDims);
                int y = 1 + ((i / 2) * LightmapConfig.TextureDims);
                CalculateUV(uvCalcFaces, new Rectangle(x, y, LightmapConfig.TextureDims - 2, LightmapConfig.TextureDims - 2), out _, out _);
                lmCount++;
                if (uvCalcFaces.Count == 0) { break; }
                totalTextureDims = LightmapConfig.TextureDims * 2;
            }

            if (uvCalcFaces.Count > 0) {
                throw new Exception("Could not fit lightmap into four textures; try increasing texture dimensions or downscale factor");
            }

            float[][] buffers = new float[4][];
            lock (Lightmaps) {
                for (int i = 0; i < 4; i++) {
                    Lightmaps[i]?.Dispose();
                    Lightmaps[i] = new RenderTarget2D(GlobalGraphics.GraphicsDevice, totalTextureDims, totalTextureDims);
                    buffers[i] = new float[totalTextureDims * totalTextureDims * 4];
                }
            }

            foreach (LightmapGroup group in lmGroups) {
                foreach (LMFace face in group.Faces) {
                    faceCount++;
                    Thread newThread = CreateLightmapRenderThread(document, buffers, lightEntities, group, face, allBlockers);
                    FaceRenderThreads.Add(newThread);
                }
            }

            int faceNum = 0;
            UpdateProgress("Started calculating brightness levels...", 0.05f);
            while (FaceRenderThreads.Count > 0) {
                for (int i = 0; i < 8; i++) {
                    if (i >= FaceRenderThreads.Count) break;
                    if (FaceRenderThreads[i].ThreadState == ThreadState.Unstarted) {
                        FaceRenderThreads[i].Start();
                    } else if (!FaceRenderThreads[i].IsAlive) {
                        FaceRenderThreads.RemoveAt(i);
                        i--;
                        faceNum++;
                        UpdateProgress(faceNum.ToString() + "/" + faceCount.ToString() + " faces complete", 0.05f + ((float)faceNum / (float)faceCount) * 0.85f);
                    }
                }

                if (threadExceptions.Count > 0) {
                    for (int i = 0; i < FaceRenderThreads.Count; i++) {
                        if (FaceRenderThreads[i].IsAlive) {
                            FaceRenderThreads[i].Abort();
                        }
                    }
                    throw new Exception(threadExceptions[0].Message + "\n" + threadExceptions[0].StackTrace);
                }
                Thread.Yield();
            }

            //blur the lightmap so it doesn't look too pixellated
            UpdateProgress("Blurring lightmap...", 0.95f);
            float[] blurBuffer = new float[buffers[0].Length];
            for (int k = 0; k < 4; k++) {
                foreach (LightmapGroup group in lmGroups) {
                    int downscaledWidth = (int)Math.Ceiling(group.Width / LightmapConfig.DownscaleFactor);
                    int downscaledHeight = (int)Math.Ceiling(group.Height / LightmapConfig.DownscaleFactor);

                    Vector3F ambientNormal = new Vector3F(LightmapConfig.AmbientNormalX,
                                                                LightmapConfig.AmbientNormalY,
                                                                LightmapConfig.AmbientNormalZ).Normalise();
                    float ambientMultiplier = (group.Plane.Normal.Dot(ambientNormal) + 1.5f) * 0.4f;
                    Vector3F mAmbientColor = new Vector3F((LightmapConfig.AmbientColorB * ambientMultiplier / 255.0f),
                                                            (LightmapConfig.AmbientColorG * ambientMultiplier / 255.0f),
                                                            (LightmapConfig.AmbientColorR * ambientMultiplier / 255.0f));
                    for (int y = group.writeY; y < group.writeY + downscaledHeight; y++) {
                        if (y < 0 || y >= totalTextureDims) continue;
                        for (int x = group.writeX; x < group.writeX + downscaledWidth; x++) {
                            if (x < 0 || x >= totalTextureDims) continue;
                            int offset = (x + y * totalTextureDims) * 4;

                            float accumRed = 0;
                            float accumGreen = 0;
                            float accumBlue = 0;
                            int sampleCount = 0;
                            for (int j = -LightmapConfig.BlurRadius; j <= LightmapConfig.BlurRadius; j++) {
                                if (y + j < 0 || y + j >= totalTextureDims) continue;
                                if (y + j < group.writeY || y + j >= group.writeY + downscaledHeight) continue;
                                for (int i = -LightmapConfig.BlurRadius; i <= LightmapConfig.BlurRadius; i++) {
                                    if (i * i + j * j > LightmapConfig.BlurRadius * LightmapConfig.BlurRadius) continue;
                                    if (x + i < 0 || x + i >= totalTextureDims) continue;
                                    if (x + i < group.writeX || x + i >= group.writeX + downscaledWidth) continue;
                                    int sampleOffset = ((x + i) + (y + j) * totalTextureDims) * 4;
                                    if (buffers[k][sampleOffset + 3] < 1.0f) continue;
                                    sampleCount++;
                                    accumRed += buffers[k][sampleOffset + 0];
                                    accumGreen += buffers[k][sampleOffset + 1];
                                    accumBlue += buffers[k][sampleOffset + 2];
                                }
                            }

                            if (sampleCount < 1) sampleCount = 1;
                            accumRed /= sampleCount;
                            accumGreen /= sampleCount;
                            accumBlue /= sampleCount;

                            accumRed = mAmbientColor.X + (accumRed * (1.0f - mAmbientColor.X));
                            accumGreen = mAmbientColor.Y + (accumGreen * (1.0f - mAmbientColor.Y));
                            accumBlue = mAmbientColor.Z + (accumBlue * (1.0f - mAmbientColor.Z));

                            if (accumRed > 1.0f) accumRed = 1.0f;
                            if (accumGreen > 1.0f) accumGreen = 1.0f;
                            if (accumBlue > 1.0f) accumBlue = 1.0f;

                            blurBuffer[offset + 0] = accumRed;
                            blurBuffer[offset + 1] = accumGreen;
                            blurBuffer[offset + 2] = accumBlue;
                            blurBuffer[offset + 3] = 1.0f;
                        }
                    }
                }

                blurBuffer.CopyTo(buffers[k], 0);
            }

            for (int i = 0; i < buffers[0].Length; i++) {
                if (i % 4 == 3) {
                    buffers[0][i] = 1.0f;
                    buffers[1][i] = 1.0f;
                    buffers[2][i] = 1.0f;
                    buffers[3][i] = 1.0f;
                } else {
                    float brightnessAdd = (buffers[0][i] + buffers[1][i] + buffers[2][i]) / (float)Math.Sqrt(3.0);
                    if (brightnessAdd > 0.0f) //normalize brightness to remove artifacts when adding together
                    {
                        buffers[0][i] *= buffers[3][i] / brightnessAdd;
                        buffers[1][i] *= buffers[3][i] / brightnessAdd;
                        buffers[2][i] *= buffers[3][i] / brightnessAdd;
                    }
                }
            }

            UpdateProgress("Copying bitmap data...", 0.99f);
            for (int k = 0; k < 4; k++) {
                byte[] byteBuffer = new byte[buffers[k].Length];
                // Color[] pixels = new Color[buffers[k].Length / 4];
                for (int i = 0; i < buffers[k].Length; i++) {
                    byteBuffer[i] = (byte)Math.Max(Math.Min(buffers[k][i] * 255.0f, 255.0f), 0.0f);
                    if (i + 3 < buffers[k].Length) {
                        // pixels[i/4] = Color.FromArgb(byteBuffer[i+0], byteBuffer[i+1], byteBuffer[i+2], byteBuffer[i+3]);
                    }
                }
                lock (Lightmaps) {
                    int j = k;
                    GameMain.Instance.PreDrawActions.Enqueue(() => {
                        Texture2D tex = new Texture2D(GameMain.Instance.GraphicsDevice, totalTextureDims, totalTextureDims);
                        tex.SetData(byteBuffer);
                        string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"lm_{j}.png");
                        FileStream fs = File.OpenWrite(fname);
                        tex.SaveAsPng(fs, totalTextureDims, totalTextureDims);
                        fs.Close();
                        document.Lightmaps[j] = new AsyncTexture(fname);
                        document.MGLightmaps[j] = tex;
                    });
                }
            }

            faces.Clear();
            faces.AddRange(lmGroups.SelectMany(g => g.Faces));

            lock (Lightmaps) {
                document.LightmapTextureOutdated = true;
                ViewportManager.MarkForRerender();
            }

            UpdateProgress("Lightmapping complete!", 1.0f);
        }

    public static void SaveLightmaps(Document document, int lmCount, string path, bool threeBasisModel) {
        lock (Lightmaps) {
            GameMain.Instance.PreDrawActions.Enqueue(() => {
                for (int i = (threeBasisModel ? 0 : 3); i < (threeBasisModel ? 3 : 4); i++) {
                    string iPath = path + (threeBasisModel ? i.ToString() : "");
                    var texture = document.MGLightmaps[i];
                    if (lmCount == 1) {
                        FileStream fs = File.OpenWrite(iPath + ".png");
                        texture.SaveAsPng(fs, texture.Width, texture.Height);
                        fs.Close();
                    } else {
                        for (int j = 0; j < lmCount; j++) {
                            
                            FileStream fs = File.OpenWrite(iPath + "_" + j.ToString() + ".png");
                            texture.SaveAsPng(fs, texture.Width, texture.Height);
                            fs.Close();
                            /*int x = ((j % 2) * LightmapConfig.TextureDims);
                            int y = ((j / 2) * LightmapConfig.TextureDims);

                            byte[] clone = new byte[texture.Width * texture.Height];
                            texture.GetData(clone);
                            Texture2D texture2 = new Texture2D(texture.GraphicsDevice, LightmapConfig.TextureDims, LightmapConfig.TextureDims);
                            byte[] tmp = new byte[LightmapConfig.TextureDims * LightmapConfig.TextureDims];
                            Array.Copy(clone, x + y * LightmapConfig.TextureDims, tmp, 0, tmp.Length);
                            texture2.SetData(tmp);
                            FileStream fs = File.OpenWrite(iPath + "_" + j.ToString() + ".png");
                            texture2.SaveAsPng(fs, texture2.Width, texture2.Height);
                            fs.Close();*/
                        }
                    }
                }
            });
        }
    }

    private static Thread CreateLightmapRenderThread(Document doc, float[][] bitmaps, List<Light> lights, LightmapGroup group, LMFace targetFace, IEnumerable<LMFace> blockerFaces) {
        return new Thread(() => {
            try {
                RenderLightOntoFace(doc, bitmaps, lights, group, targetFace, blockerFaces);
            } catch (ThreadAbortException) {
                //do nothing
            } catch (Exception e) {
                threadExceptions.Add(new LMThreadException(e));
            }
        }) { CurrentCulture = CultureInfo.InvariantCulture };
    }

    private static void RenderLightOntoFace(Document doc, float[][] bitmaps, List<Light> lights, LightmapGroup group, LMFace targetFace, IEnumerable<LMFace> blockerFaces) {
        Random rand = new Random();

        int writeX = group.writeX;
        int writeY = group.writeY;

        int textureDims;
        lock (Lightmaps) {
            textureDims = Lightmaps[0].Width;
        }

        lights = lights.FindAll(x => {
            float range = x.Range;
            BoxF lightBox = new BoxF(x.Origin - new Vector3F(range, range, range), x.Origin + new Vector3F(range, range, range));
            return lightBox.IntersectsWith(targetFace.BoundingBox);
        });

        float? minX = null; float? maxX = null;
        float? minY = null; float? maxY = null;

        foreach (Vector3F coord in targetFace.Vertices.Select(x => x.Location)) {
            float x = coord.Dot(group.uAxis);
            float y = coord.Dot(group.vAxis);

            if (minX == null || x < minX) minX = x;
            if (minY == null || y < minY) minY = y;
            if (maxX == null || x > maxX) maxX = x;
            if (maxY == null || y > maxY) maxY = y;
        }

        Vector3F leewayPoint = group.Plane.PointOnPlane + (group.Plane.Normal * Math.Max(LightmapConfig.DownscaleFactor * 0.25f, 1.5f));

        minX -= LightmapConfig.DownscaleFactor; minY -= LightmapConfig.DownscaleFactor;
        maxX += LightmapConfig.DownscaleFactor; maxY += LightmapConfig.DownscaleFactor;

        minX /= LightmapConfig.DownscaleFactor; minX = (float)Math.Ceiling(minX.Value); minX *= LightmapConfig.DownscaleFactor;
        minY /= LightmapConfig.DownscaleFactor; minY = (float)Math.Ceiling(minY.Value); minY *= LightmapConfig.DownscaleFactor;
        maxX /= LightmapConfig.DownscaleFactor; maxX = (float)Math.Ceiling(maxX.Value); maxX *= LightmapConfig.DownscaleFactor;
        maxY /= LightmapConfig.DownscaleFactor; maxY = (float)Math.Ceiling(maxY.Value); maxY *= LightmapConfig.DownscaleFactor;

        foreach (LMFace.Vertex vert in targetFace.Vertices) {
            float x = vert.Location.Dot(group.uAxis);
            float y = vert.Location.Dot(group.vAxis);

            float u = (writeX + 0.5f + (x - group.minTotalX.Value) / LightmapConfig.DownscaleFactor);
            float v = (writeY + 0.5f + (y - group.minTotalY.Value) / LightmapConfig.DownscaleFactor);

            targetFace.LmIndex = (u >= LightmapConfig.TextureDims ? 1 : 0) + (v >= LightmapConfig.TextureDims ? 2 : 0);

            u /= (float)textureDims;
            v /= (float)textureDims;

            vert.LMU = u; vert.LMV = v;
            vert.OriginalVertex.LMU = u; vert.OriginalVertex.LMV = v;
        }

        float centerX = (maxX.Value + minX.Value) / 2;
        float centerY = (maxY.Value + minY.Value) / 2;

        int iterX = (int)Math.Ceiling((maxX.Value - minX.Value) / LightmapConfig.DownscaleFactor);
        int iterY = (int)Math.Ceiling((maxY.Value - minY.Value) / LightmapConfig.DownscaleFactor);

        float[][,] r = new float[4][,];
        r[0] = new float[iterX, iterY];
        r[1] = new float[iterX, iterY];
        r[2] = new float[iterX, iterY];
        r[3] = new float[iterX, iterY];
        float[][,] g = new float[4][,];
        g[0] = new float[iterX, iterY];
        g[1] = new float[iterX, iterY];
        g[2] = new float[iterX, iterY];
        g[3] = new float[iterX, iterY];
        float[][,] b = new float[4][,];
        b[0] = new float[iterX, iterY];
        b[1] = new float[iterX, iterY];
        b[2] = new float[iterX, iterY];
        b[3] = new float[iterX, iterY];

        foreach (Light light in lights) {
            Vector3F lightPos = light.Origin;
            float lightRange = light.Range;
            Vector3F lightColor = light.Color * (1.0f / 255.0f) * light.Intensity;

            BoxF lightBox = new BoxF(new BoxF[] { targetFace.BoundingBox, new BoxF(light.Origin - new Vector3F(30.0f, 30.0f, 30.0f), light.Origin + new Vector3F(30.0f, 30.0f, 30.0f)) });
            List<LMFace> applicableBlockerFaces = blockerFaces.Where(x => {
                if (x == targetFace) return false;
                if (group.Faces.Contains(x)) return false;
                //return true;
                if (lightBox.IntersectsWith(x.BoundingBox)) return true;
                return false;
            }).ToList();

            bool[,] illuminated = new bool[iterX, iterY];

            for (int y = 0; y < iterY; y++) {
                for (int x = 0; x < iterX; x++) {
                    illuminated[x, y] = true;
                }
            }

            for (int y = 0; y < iterY; y++) {
                for (int x = 0; x < iterX; x++) {
                    int tX = (int)(writeX + x + (int)(minX - group.minTotalX) / LightmapConfig.DownscaleFactor);
                    int tY = (int)(writeY + y + (int)(minY - group.minTotalY) / LightmapConfig.DownscaleFactor);

                    if (tX >= 0 && tY >= 0 && tX < textureDims && tY < textureDims) {
                        int offset = (tX + tY * textureDims) * 4;
                        bitmaps[0][offset + 3] = 1.0f;
                        bitmaps[1][offset + 3] = 1.0f;
                        bitmaps[2][offset + 3] = 1.0f;
                        bitmaps[3][offset + 3] = 1.0f;
                    }
                }
            }

            for (int y = 0; y < iterY; y++) {
                for (int x = 0; x < iterX; x++) {
                    float ttX = minX.Value + (x * LightmapConfig.DownscaleFactor);
                    float ttY = minY.Value + (y * LightmapConfig.DownscaleFactor);
                    Vector3F pointOnPlane = (ttX - centerX) * group.uAxis + (ttY - centerY) * group.vAxis + targetFace.BoundingBox.Center;

                    /*Entity entity = new Entity(map.IDGenerator.GetNextObjectID());
                    entity.Colour = Color.Pink;
                    entity.Origin = new Coordinate(pointOnPlane);
                    entity.UpdateBoundingBox();
                    entity.SetParent(map.WorldSpawn);*/

                int tX = (int)(writeX + x + (int)(minX - group.minTotalX) / LightmapConfig.DownscaleFactor);
                        int tY = (int)(writeY + y + (int)(minY - group.minTotalY) / LightmapConfig.DownscaleFactor);

                        Vector3F luxelColor0 = new Vector3F(r[0][x, y], g[0][x, y], b[0][x, y]);
                        Vector3F luxelColor1 = new Vector3F(r[1][x, y], g[1][x, y], b[1][x, y]);
                        Vector3F luxelColor2 = new Vector3F(r[2][x, y], g[2][x, y], b[2][x, y]);
                        Vector3F luxelColorNorm = new Vector3F(r[3][x, y], g[3][x, y], b[3][x, y]);

                        float dotToLight0 = Math.Max((lightPos - pointOnPlane).Normalise().Dot(targetFace.LightBasis0), 0.0f);
                        float dotToLight1 = Math.Max((lightPos - pointOnPlane).Normalise().Dot(targetFace.LightBasis1), 0.0f);
                        float dotToLight2 = Math.Max((lightPos - pointOnPlane).Normalise().Dot(targetFace.LightBasis2), 0.0f);
                        float dotToLightNorm = Math.Max((lightPos - pointOnPlane).Normalise().Dot(targetFace.Normal), 0.0f);

                        if (illuminated[x, y] && (pointOnPlane - lightPos).LengthSquared() < lightRange * lightRange) {
#if TRUE
                            LineF lineTester = new LineF(lightPos, pointOnPlane);
                            for (int i = 0; i < applicableBlockerFaces.Count; i++) {
                                LMFace otherFace = applicableBlockerFaces[i];
                                Vector3F hit = otherFace.GetIntersectionPoint(lineTester);
                                if (hit != null && ((hit - leewayPoint).Dot(group.Plane.Normal) > 0.0f || (hit - pointOnPlane).LengthSquared() > LightmapConfig.DownscaleFactor * 2f)) {
                                    applicableBlockerFaces.RemoveAt(i);
                                    applicableBlockerFaces.Insert(0, otherFace);
                                    illuminated[x, y] = false;
                                    i++;
                                    break;
                                }
                            }
#endif
                        } else {
                            illuminated[x, y] = false;
                        }

                        if (illuminated[x, y]) {
                            float brightness = (lightRange - (pointOnPlane - lightPos).VectorMagnitude()) / lightRange;

                            if (light.Direction != null) {
                                float directionDot = light.Direction.Dot((pointOnPlane - lightPos).Normalise());

                                if (directionDot < light.innerCos) {
                                    if (directionDot < light.outerCos) {
                                        brightness = 0.0f;
                                    } else {
                                        brightness *= (directionDot - light.outerCos.Value) / (light.innerCos.Value - light.outerCos.Value);
                                    }
                                }
                            }

                            float brightness0 = dotToLight0 * brightness * brightness;
                            float brightness1 = dotToLight1 * brightness * brightness;
                            float brightness2 = dotToLight2 * brightness * brightness;
                            float brightnessNorm = dotToLightNorm * brightness * brightness;

                            brightness0 += ((float)rand.NextDouble() - 0.5f) * 0.005f;
                            brightness1 += ((float)rand.NextDouble() - 0.5f) * 0.005f;
                            brightness2 += ((float)rand.NextDouble() - 0.5f) * 0.005f;
                            brightnessNorm += ((float)rand.NextDouble() - 0.5f) * 0.005f;

                            r[0][x, y] += lightColor.X * brightness0; if (r[0][x, y] > 1.0f) r[0][x, y] = 1.0f; if (r[0][x, y] < 0) r[0][x, y] = 0;
                            g[0][x, y] += lightColor.Y * brightness0; if (g[0][x, y] > 1.0f) g[0][x, y] = 1.0f; if (g[0][x, y] < 0) g[0][x, y] = 0;
                            b[0][x, y] += lightColor.Z * brightness0; if (b[0][x, y] > 1.0f) b[0][x, y] = 1.0f; if (b[0][x, y] < 0) b[0][x, y] = 0;

                            r[1][x, y] += lightColor.X * brightness1; if (r[1][x, y] > 1.0f) r[1][x, y] = 1.0f; if (r[1][x, y] < 0) r[1][x, y] = 0;
                            g[1][x, y] += lightColor.Y * brightness1; if (g[1][x, y] > 1.0f) g[1][x, y] = 1.0f; if (g[1][x, y] < 0) g[1][x, y] = 0;
                            b[1][x, y] += lightColor.Z * brightness1; if (b[1][x, y] > 1.0f) b[1][x, y] = 1.0f; if (b[1][x, y] < 0) b[1][x, y] = 0;

                            r[2][x, y] += lightColor.X * brightness2; if (r[2][x, y] > 1.0f) r[2][x, y] = 1.0f; if (r[2][x, y] < 0) r[2][x, y] = 0;
                            g[2][x, y] += lightColor.Y * brightness2; if (g[2][x, y] > 1.0f) g[2][x, y] = 1.0f; if (g[2][x, y] < 0) g[2][x, y] = 0;
                            b[2][x, y] += lightColor.Z * brightness2; if (b[2][x, y] > 1.0f) b[2][x, y] = 1.0f; if (b[2][x, y] < 0) b[2][x, y] = 0;

                            r[3][x, y] += lightColor.X * brightnessNorm; if (r[3][x, y] > 1.0f) r[3][x, y] = 1.0f; if (r[3][x, y] < 0) r[3][x, y] = 0;
                            g[3][x, y] += lightColor.Y * brightnessNorm; if (g[3][x, y] > 1.0f) g[3][x, y] = 1.0f; if (g[3][x, y] < 0) g[3][x, y] = 0;
                            b[3][x, y] += lightColor.Z * brightnessNorm; if (b[3][x, y] > 1.0f) b[3][x, y] = 1.0f; if (b[3][x, y] < 0) b[3][x, y] = 0;

                            luxelColor0 = new Vector3F(r[0][x, y], g[0][x, y], b[0][x, y]);
                            luxelColor1 = new Vector3F(r[1][x, y], g[1][x, y], b[1][x, y]);
                            luxelColor2 = new Vector3F(r[2][x, y], g[2][x, y], b[2][x, y]);
                            luxelColorNorm = new Vector3F(r[3][x, y], g[3][x, y], b[3][x, y]);

                            if (tX >= 0 && tY >= 0 && tX < textureDims && tY < textureDims) {
                                int offset = (tX + tY * textureDims) * 4;
                                if (luxelColor0.X + luxelColor0.Y + luxelColor0.Z > bitmaps[0][offset + 2] + bitmaps[0][offset + 1] + bitmaps[0][offset + 0]) {
                                    bitmaps[0][offset + 0] = luxelColor0.X;
                                    bitmaps[0][offset + 1] = luxelColor0.Y;
                                    bitmaps[0][offset + 2] = luxelColor0.Z;
                                }
                                if (luxelColor1.X + luxelColor1.Y + luxelColor1.Z > bitmaps[1][offset + 2] + bitmaps[1][offset + 1] + bitmaps[1][offset + 0]) {
                                    bitmaps[1][offset + 0] = luxelColor1.X;
                                    bitmaps[1][offset + 1] = luxelColor1.Y;
                                    bitmaps[1][offset + 2] = luxelColor1.Z;
                                }
                                if (luxelColor2.X + luxelColor2.Y + luxelColor2.Z > bitmaps[2][offset + 2] + bitmaps[2][offset + 1] + bitmaps[2][offset + 0]) {
                                    bitmaps[2][offset + 0] = luxelColor2.X;
                                    bitmaps[2][offset + 1] = luxelColor2.Y;
                                    bitmaps[2][offset + 2] = luxelColor2.Z;
                                }
                                if (luxelColorNorm.X + luxelColorNorm.Y + luxelColorNorm.Z > bitmaps[3][offset + 2] + bitmaps[3][offset + 1] + bitmaps[3][offset + 0]) {
                                    bitmaps[3][offset + 0] = luxelColorNorm.X;
                                    bitmaps[3][offset + 1] = luxelColorNorm.Y;
                                    bitmaps[3][offset + 2] = luxelColorNorm.Z;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

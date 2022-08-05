using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CBRE.Common;
using Microsoft.Xna.Framework.Graphics;
using MetadataExtractor;
using System.Collections.Generic;

namespace CBRE.Graphics {
    public class AsyncTexture : ITexture {
        static AsyncTexture() {
            highThroughputSemaphore = new Semaphore(0, 5);
            highThroughputSemaphore.Release(5);
            lowThroughputSemaphore = new Semaphore(0, 1);
            lowThroughputSemaphore.Release(1);
        }

        public struct Data {
            public byte[] Bytes;
            public int Width; public int Height;
            public bool Compressed;
            public bool Transparent;
        }

        public static List<AsyncTexture> AllTextures { get; private set; } = new List<AsyncTexture>();
        public static Action<string> LoadCallback = null;
        public static int MaxDimensions { get; private set; } = 1024;

        private static Semaphore highThroughputSemaphore;
        private static Semaphore lowThroughputSemaphore;

        private Task<Data> task;

        public Texture2D MonoGameTexture { get; private set; }

        public IntPtr ImGuiTexture { get; private set; }

        public double DownscaleFactor { get; private set; } = 1;
        public int DataBufferSize { get; private set; } = 0;
        public int LoadingBufferSize { get; private set; } = 0;

        public TextureFlags Flags { get; private set; } = TextureFlags.None;

        public string Name => Path.GetFileNameWithoutExtension(Filename);

        public int Width => (int)((MonoGameTexture?.Width ?? 0) * DownscaleFactor);

        public int Height => (int)((MonoGameTexture?.Height ?? 0) * DownscaleFactor);

        public readonly string Filename;

        public AsyncTexture(string filename, Task<Data> tsk=null) {
            AllTextures.Add(this);
            MonoGameTexture = null;
            ImGuiTexture = IntPtr.Zero;

            Filename = filename;
            task = tsk ?? Load();
            TaskPool.Add("AsyncTexture task", task, (t) => {
                CheckTaskStatus();
                LoadCallback?.Invoke(Path.GetFileNameWithoutExtension(Filename));
            });
        }

        ~AsyncTexture() {
            AllTextures.Remove(this);
        }

        private async Task<Data> Load(Semaphore semaphore = null) {
            await Task.Delay(200);
            semaphore ??= highThroughputSemaphore;
            while (!semaphore.WaitOne(20)) {
                await Task.Delay(200);
            }
            try {
                byte[] bytes;
                int width;
                int height;
                var fileData = await File.ReadAllBytesAsync(Filename);
                LoadingBufferSize += fileData.Length;
                using (var stream = new MemoryStream(fileData)) {
                    var metadata = ImageMetadataReader.ReadMetadata(stream);
                    switch (metadata[0]) {
                        case MetadataExtractor.Formats.Png.PngDirectory pngDirectory:
                            width = pngDirectory.GetInt32(MetadataExtractor.Formats.Png.PngDirectory.TagImageWidth);
                            height = pngDirectory.GetInt32(MetadataExtractor.Formats.Png.PngDirectory.TagImageHeight);
                            break;
                        case MetadataExtractor.Formats.Jpeg.JpegDirectory jpegDirectory:
                            width = jpegDirectory.GetInt32(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth);
                            height = jpegDirectory.GetInt32(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight);
                            break;
                        default:
                            throw new Exception($"Unsupported format: {metadata[0].GetType().Name}");
                    }
                }

                if (semaphore == highThroughputSemaphore && (width > MaxDimensions || height > MaxDimensions)) {
                    //do not allow this task to run concurrently with other
                    //load tasks, as there might not be enough memory for this
                    semaphore.Release();
                    semaphore = null;
                    var data = await Load(lowThroughputSemaphore);
                    return data;
                }

                using (var stream = new MemoryStream(fileData)) {
                    bytes = Texture2D.TextureDataFromStream(stream, out width, out height, out _);
                    LoadingBufferSize += bytes.Length;
                }

                DownscaleFactor = Math.Max(Math.Min(width / MaxDimensions, height / MaxDimensions), 1f);

                if (DownscaleFactor > 1 && width > DownscaleFactor && height > DownscaleFactor) {
                    double downscaleSqr = DownscaleFactor * DownscaleFactor;
                    byte[] oldBytes = bytes;

                    width = (int)(width / DownscaleFactor);
                    height = (int)(height / DownscaleFactor);
                    bytes = new byte[width * height * 4];
                    LoadingBufferSize += bytes.Length;
                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++) {
                            void copyByte(int chn) {
                                bytes[(x * 4) + (y * 4 * width) + chn]
                                    = oldBytes[((int)(x * 4 * DownscaleFactor)) + ((int)(y * 4 * width * downscaleSqr)) + chn];
                            }
                            copyByte(0);
                            copyByte(1);
                            copyByte(2);
                            copyByte(3);
                        }
                    }

                    LoadingBufferSize -= oldBytes.Length;
                } else {
                    DownscaleFactor = 1;
                }

                bool compressed = false;
                bool transparent = false;
                for (int i = 0; i < bytes.Length; i+=4) {
                    if (bytes[i+3] != 255) {
                        transparent = true;
                        break;
                    }
                }
                if ((width > 64 || height > 64) &&
                    (width & 0x03) == 0 && (height & 0x03) == 0) {
                    var prevBytes = bytes;
                    bytes = CompressDxt5(bytes, width, height);
                    LoadingBufferSize += bytes.Length;
                    LoadingBufferSize -= prevBytes.Length;
                    compressed = true;
                }

                return new Data { Bytes = bytes, Width = width, Height = height, Compressed = compressed, Transparent = transparent };
            } catch (Exception e) {
                if (semaphore == lowThroughputSemaphore) { throw; }
                throw new Exception($"Failed to load \"{Name}\": {e.Message} ({e.GetType().Name}) {e.StackTrace}", e);
            } finally {
                if (semaphore == lowThroughputSemaphore) { GC.Collect(); }

                semaphore?.Release();
            }
        }

        private void CheckTaskStatus() {
            if (task != null) {
                if (!task.IsCompleted) { return; }
                if (task.Exception is null) {
                    Data data = task.Result;
                    MonoGameTexture = new Texture2D(GlobalGraphics.GraphicsDevice, data.Width, data.Height, false, data.Compressed ? SurfaceFormat.Dxt5 : SurfaceFormat.Color);
                    MonoGameTexture.SetData(data.Bytes);

                    DataBufferSize = data.Width * data.Height * (data.Compressed ? 1 : 4);

                    ImGuiTexture = GlobalGraphics.ImGuiRenderer.BindTexture(MonoGameTexture);

                    Flags = data.Transparent ? TextureFlags.Transparent : TextureFlags.None;
                        
                    LoadingBufferSize = 0;

                    task.Dispose();
                } else {
                    Console.WriteLine($"{task.Exception.InnerException.Message}\n{task.Exception.InnerException.StackTrace}");
                    //TODO: log exceptions
                }

                task = null;
            }
        }

        public void Dispose() {
            if (task != null) {
                task.Wait();
                task.Dispose();
                task = null;
            }

            if (ImGuiTexture != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(ImGuiTexture);
                ImGuiTexture = IntPtr.Zero;
            }
            MonoGameTexture?.Dispose(); MonoGameTexture = null;
        }

        private static byte[] CompressDxt5(byte[] data, int width, int height) {
            using (System.IO.MemoryStream mstream = new System.IO.MemoryStream()) {
                for (int y = 0; y < height; y += 4) {
                    for (int x = 0; x < width; x += 4) {
                        int offset = x * 4 + y * 4 * width;
                        CompressDxt5Block(data, offset, width, mstream);
                    }
                }
                return mstream.ToArray();
            }
        }

        private static void CompressDxt5Block(byte[] data, int offset, int width, System.IO.Stream output) {
            int r1 = 255, g1 = 255, b1 = 255, a1 = 255;
            int r2 = 0, g2 = 0, b2 = 0, a2 = 0;

            //determine the two colors to interpolate between:
            //color 1 represents lowest luma, color 2 represents highest luma
            for (int i = 0; i < 16; i++) {
                int pixelOffset = offset + (4 * ((i % 4) + (width * (i >> 2))));
                int r, g, b, a;
                r = data[pixelOffset + 0];
                g = data[pixelOffset + 1];
                b = data[pixelOffset + 2];
                a = data[pixelOffset + 3];
                if (r * 299 + g * 587 + b * 114 < r1 * 299 + g1 * 587 + b1 * 114) {
                    r1 = r; g1 = g; b1 = b;
                }
                if (r * 299 + g * 587 + b * 114 > r2 * 299 + g2 * 587 + b2 * 114) {
                    r2 = r; g2 = g; b2 = b;
                }
                if (a < a1) { a1 = a; }
                if (a > a2) { a2 = a; }
            }

            //convert the colors to rgb565 (16-bit rgb)
            int r1_565 = (r1 * 0x1f) / 0xff; if (r1_565 > 0x1f) { r1_565 = 0x1f; }
            int g1_565 = (g1 * 0x3f) / 0xff; if (g1_565 > 0x3f) { g1_565 = 0x3f; }
            int b1_565 = (b1 * 0x1f) / 0xff; if (b1_565 > 0x1f) { b1_565 = 0x1f; }

            int r2_565 = (r2 * 0x1f) / 0xff; if (r2_565 > 0x1f) { r2_565 = 0x1f; }
            int g2_565 = (g2 * 0x3f) / 0xff; if (g2_565 > 0x3f) { g2_565 = 0x3f; }
            int b2_565 = (b2 * 0x1f) / 0xff; if (b2_565 > 0x1f) { b2_565 = 0x1f; }

            //luma is also used to determine which color on the palette
            //most closely resembles each pixel to compress, so we
            //calculate this here
            int y1 = r1 * 299 + g1 * 587 + b1 * 114;
            int y2 = r2 * 299 + g2 * 587 + b2 * 114;

            byte[] newData = new byte[16];
            for (int i = 0; i < 16; i++) {
                int pixelOffset = offset + (4 * ((i % 4) + (width * (i >> 2))));
                int r, g, b, a;
                r = data[pixelOffset + 0];
                g = data[pixelOffset + 1];
                b = data[pixelOffset + 2];
                a = data[pixelOffset + 3];

                if (a1 < a2) {
                    a -= a1;
                    a = (a * 0x7) / (a2 - a1);
                    if (a > 0x7) { a = 0x7; }

                    switch (a) {
                        case 0:
                            a = 1;
                            break;
                        case 1:
                            a = 7;
                            break;
                        case 2:
                            a = 6;
                            break;
                        case 3:
                            a = 5;
                            break;
                        case 4:
                            a = 4;
                            break;
                        case 5:
                            a = 3;
                            break;
                        case 6:
                            a = 2;
                            break;
                        case 7:
                            a = 0;
                            break;
                    }
                } else {
                    a = 0;
                }

                NetBitWriter.WriteUInt32((uint)a, 3, newData, 16 + (i * 3));

                int y = r * 299 + g * 587 + b * 114;

                int max = y2 - y1;
                int diffY = y - y1;

                int paletteIndex;
                if (diffY < max / 4) {
                    paletteIndex = 0;
                } else if (diffY < max / 2) {
                    paletteIndex = 2;
                } else if (diffY < max * 3 / 4) {
                    paletteIndex = 3;
                } else {
                    paletteIndex = 1;
                }
                newData[12 + (i / 4)] |= (byte)(paletteIndex << (2 * (i % 4)));
            }

            newData[0] = (byte)a2;
            newData[1] = (byte)a1;

            newData[9] = (byte)((r1_565 << 3) | (g1_565 >> 3));
            newData[8] = (byte)((g1_565 << 5) | b1_565);
            newData[11] = (byte)((r2_565 << 3) | (g2_565 >> 3));
            newData[10] = (byte)((g2_565 << 5) | b2_565);

            output.Write(newData, 0, 16);
        }

        public void Bind() {
            // GlobalGraphics.GraphicsDevice.Textures[0] = this.monoGameTexture;
            PrimitiveDrawing.Texture = this.MonoGameTexture;
        }

        public void Unbind() {
            // GlobalGraphics.GraphicsDevice.Textures[0] = this.monoGameTexture;
            if (PrimitiveDrawing.Texture == this.MonoGameTexture)
                PrimitiveDrawing.Texture = null;
        }
    }
}

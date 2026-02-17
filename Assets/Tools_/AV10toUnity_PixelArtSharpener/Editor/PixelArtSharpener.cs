using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AV10
{
    public class PixelArtSharpener : EditorWindow
    {
        private Texture2D sourceTexture;
        private int blockWidth = 27;
        private int blockHeight = 27;
        private bool autoDetect = true;

        [MenuItem("Tools/AV10 Pixel Art Sharpener")]
        public static void ShowWindow()
        {
            GetWindow<PixelArtSharpener>("AV10 Pixel Art Sharpener");
        }

        private void OnGUI()
        {
            GUILayout.Label("Pixel Art Keskinleştirici", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Kaynak Resim (Source)", sourceTexture, typeof(Texture2D), false);

            EditorGUILayout.Space();

            autoDetect = EditorGUILayout.Toggle("Otomatik Algıla (Auto Detect)", autoDetect);

            if (!autoDetect)
            {
                GUILayout.Label("Manuel Blok Boyutları (Pixel)", EditorStyles.label);
                blockWidth = EditorGUILayout.IntField("Genişlik (X)", blockWidth);
                blockHeight = EditorGUILayout.IntField("Yükseklik (Y)", blockHeight);
            }
            else
            {
                EditorGUILayout.HelpBox("Resim analiz edilerek en uygun piksel boyutu otomatik bulunacaktır. (The optimal pixel size will be automatically detected by analyzing the image.)", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Analiz Et, Keskinleştir ve Kaydet"))
            {
                if (sourceTexture == null)
                {
                    EditorUtility.DisplayDialog("Hata", "Lütfen bir kaynak resim seçin.", "Tamam");
                    return;
                }

                if (!autoDetect && (blockWidth <= 0 || blockHeight <= 0))
                {
                    EditorUtility.DisplayDialog("Hata", "Manuel modda blok boyutları 0'dan büyük olmalıdır.", "Tamam");
                    return;
                }

                ProcessImage();
            }
        }

        private void ProcessImage()
        {
            string path = AssetDatabase.GetAssetPath(sourceTexture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            bool wasReadable = false;
            TextureImporterCompression originalCompression = TextureImporterCompression.Uncompressed;
            TextureImporterNPOTScale originalNPOT = TextureImporterNPOTScale.None;

            // Make texture readable temporarily
            if (importer != null)
            {
                wasReadable = importer.isReadable;
                originalCompression = importer.textureCompression;
                originalNPOT = importer.npotScale;

                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            try
            {
                int width = sourceTexture.width;
                int height = sourceTexture.height;
                Color[] sourcePixels = sourceTexture.GetPixels();

                if (autoDetect)
                {
                    DetectBlockSize(sourcePixels, width, height, out blockWidth, out blockHeight);
                    Debug.Log($"Otomatik Algılanan Blok Boyutu: {blockWidth}x{blockHeight}");
                }

                blockWidth = Mathf.Max(1, blockWidth);
                blockHeight = Mathf.Max(1, blockHeight);

                // --- RESIZING LOGIC ---
                // Calculate optimal width/height that is a multiple of block size
                // We round to the nearest block count to decide if we should shrink or expand
                int optimalBlocksX = Mathf.RoundToInt((float)width / blockWidth);
                int optimalBlocksY = Mathf.RoundToInt((float)height / blockHeight);

                // If optimal blocks is 0 (image too small relative to block), force at least 1
                if (optimalBlocksX < 1) optimalBlocksX = 1;
                if (optimalBlocksY < 1) optimalBlocksY = 1;

                int targetWidth = optimalBlocksX * blockWidth;
                int targetHeight = optimalBlocksY * blockHeight;

                if (targetWidth != width || targetHeight != height)
                {
                    Debug.Log($"Resim yeniden boyutlandırılıyor (Resizing): {width}x{height} -> {targetWidth}x{targetHeight} (Bloklar: {optimalBlocksX}x{optimalBlocksY})");
                    sourcePixels = ResizePixels(sourcePixels, width, height, targetWidth, targetHeight);
                    width = targetWidth;
                    height = targetHeight;
                }
                // ----------------------

                Texture2D outputTexture = new Texture2D(width, height);
                // Set point filtering for crisp pixel art look in the output texture settings (although we save PNG)
                outputTexture.filterMode = FilterMode.Point;

                Color[] outputPixels = new Color[width * height];

                for (int y = 0; y < height; y += blockHeight)
                {
                    for (int x = 0; x < width; x += blockWidth)
                    {
                        // Determine the center of the current block
                        int centerX = x + blockWidth / 2;
                        int centerY = y + blockHeight / 2;

                        // Clamp (should be safe now with perfect tiling, but safe is good)
                        int safeCenterX = Mathf.Clamp(centerX, 0, width - 1);
                        int safeCenterY = Mathf.Clamp(centerY, 0, height - 1);

                        // Get the color at the center
                        Color centerColor = sourcePixels[safeCenterY * width + safeCenterX];

                        // Fill the block with this color
                        for (int by = 0; by < blockHeight; by++)
                        {
                            int targetY = y + by;
                            if (targetY >= height) break;

                            for (int bx = 0; bx < blockWidth; bx++)
                            {
                                int targetX = x + bx;
                                if (targetX >= width) break;

                                outputPixels[targetY * width + targetX] = centerColor;
                            }
                        }
                    }
                }

                outputTexture.SetPixels(outputPixels);
                outputTexture.Apply();

                // Save options
                string dir = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                string savePath = EditorUtility.SaveFilePanel("Save Sharpened Image", dir, fileName + "_Sharpened", "png");

                if (!string.IsNullOrEmpty(savePath))
                {
                    byte[] bytes = outputTexture.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);
                    AssetDatabase.Refresh();
                    Debug.Log("Image saved to: " + savePath);
                }
            }
            finally
            {
                // Revert importer settings
                if (importer != null)
                {
                    importer.isReadable = wasReadable;
                    importer.textureCompression = originalCompression;
                    importer.npotScale = originalNPOT;
                    importer.SaveAndReimport();
                }
            }
        }

        // Simple Bilinear Resize
        private Color[] ResizePixels(Color[] pixels, int w1, int h1, int w2, int h2)
        {
            Color[] newPixels = new Color[w2 * h2];
            float x_ratio = ((float)(w1 - 1)) / w2;
            float y_ratio = ((float)(h1 - 1)) / h2;

            for (int i = 0; i < h2; i++)
            {
                for (int j = 0; j < w2; j++)
                {
                    int x = (int)(x_ratio * j);
                    int y = (int)(y_ratio * i);
                    float x_diff = (x_ratio * j) - x;
                    float y_diff = (y_ratio * i) - y;

                    int index = y * w1 + x;
                    Color a = pixels[index];
                    Color b = pixels[index + 1];
                    Color c = pixels[index + w1];
                    Color d = pixels[index + w1 + 1];

                    // Bilinear interpolation
                    Color pixel = Color.Lerp(
                        Color.Lerp(a, b, x_diff),
                        Color.Lerp(c, d, x_diff),
                        y_diff
                    );

                    newPixels[i * w2 + j] = pixel;
                }
            }
            return newPixels;
        }

        private void DetectBlockSize(Color[] pixels, int width, int height, out int bestWidth, out int bestHeight)
        {
            bestWidth = DetectPrimaryPeriod(pixels, width, height, true);
            bestHeight = DetectPrimaryPeriod(pixels, width, height, false);
        }

        private int DetectPrimaryPeriod(Color[] pixels, int width, int height, bool horizontalScan)
        {
            int scanLength = horizontalScan ? width : height;
            int crossLength = horizontalScan ? height : width;

            float[] gradients = new float[scanLength];
            int step = 1;
            if (crossLength > 500) step = 10;

            for (int i = 0; i < scanLength - 1; i++)
            {
                float totalDiff = 0f;
                int sampleCount = 0;

                for (int j = 0; j < crossLength; j += step)
                {
                    int idx1 = horizontalScan ? (j * width + i) : (i * width + j);
                    int idx2 = horizontalScan ? (j * width + i + 1) : ((i + 1) * width + j);

                    Color c1 = pixels[idx1];
                    Color c2 = pixels[idx2];

                    float diff = Mathf.Abs(c1.r - c2.r) + Mathf.Abs(c1.g - c2.g) + Mathf.Abs(c1.b - c2.b);
                    totalDiff += diff;
                    sampleCount++;
                }
                gradients[i] = totalDiff / sampleCount;
            }

            List<int> peaks = new List<int>();
            float avgGrad = gradients.Average();
            float threshold = Mathf.Max(0.02f, avgGrad * 1.5f);

            for (int i = 1; i < scanLength - 1; i++)
            {
                if (gradients[i] > threshold && gradients[i] > gradients[i - 1] && gradients[i] > gradients[i + 1])
                {
                    peaks.Add(i);
                }
            }

            if (peaks.Count < 2) return 1;

            Dictionary<int, int> distanceCounts = new Dictionary<int, int>();

            for (int i = 0; i < peaks.Count - 1; i++)
            {
                int dist = peaks[i + 1] - peaks[i];
                if (dist < 2) continue;

                if (!distanceCounts.ContainsKey(dist)) distanceCounts[dist] = 0;
                distanceCounts[dist]++;
            }

            if (distanceCounts.Count == 0) return 1;

            int bestDist = distanceCounts.OrderByDescending(x => x.Value).First().Key;

            return bestDist;
        }
    }
}

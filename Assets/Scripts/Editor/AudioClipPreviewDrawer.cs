using System;
using System.Linq;
using UnityEngine;

namespace Pingle.EditorTools
{
    public static class AudioClipPreviewDrawer
    {
        private static readonly Color BackgroundColor = Color.clear;
        private static readonly Color WaveColor = Color.blue;
        private static readonly int MaxSamplesPerPixel = 64;

        public static void DrawPreview(Texture2D texture, AudioClip clip, int startSample, int endSample, int channelIndex)
        {
            ClearTexture(texture);

            if (clip == null)
            {
                texture.Apply();
                return;
            }

            var samplesPerPixel = Mathf.Max(1, (endSample - startSample) / texture.width);
            var channelCount = clip.channels;
            var buffer = new float[samplesPerPixel * channelCount];
            var textureHeight = texture.height;
            var offset = (int)samplesPerPixel / MaxSamplesPerPixel;

            for (var x = 0; x < texture.width; x++)
            {
                clip.GetData(buffer, x * samplesPerPixel);
                var minY = int.MaxValue;
                var maxY = int.MinValue;
                for (var j = 0; j < buffer.Length / channelCount; j += offset)
                {
                    var yPos = (int)((1 - (buffer[j * channelCount + channelIndex] + 1) / 2) * textureHeight);

                    if (yPos < minY) minY = yPos;
                    if (yPos > maxY) maxY = yPos;
                }

                for (var j = minY; j <= maxY; j++)
                    texture.SetPixel(x, j, WaveColor);
            }

            texture.Apply();
        }

        private static void ClearTexture(Texture2D texture)
        {
            for (var i = 0; i < texture.width; i++)
                for (var j = 0; j < texture.height; j++)
                    texture.SetPixel(i, j, BackgroundColor);
        }
    }
}

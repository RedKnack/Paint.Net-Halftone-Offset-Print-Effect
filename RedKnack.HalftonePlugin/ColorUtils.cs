using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Runtime.CompilerServices;

namespace RedKnack.HalftonePlugin
{
    internal static class ColorUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Luminance(ColorBgra c)
            => c.R / 255f * 0.2126f
             + c.G / 255f * 0.7152f
             + c.B / 255f * 0.0722f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Brightness(ColorBgra c)
            => (c.R + c.G + c.B) / (3f * 255f);

        public static void RgbToCmyk(ColorBgra c,
            out float cyan, out float magenta, out float yellow, out float black)
        {
            float r = c.R / 255f;
            float g = c.G / 255f;
            float b = c.B / 255f;

            float k = 1f - MathF.Max(r, MathF.Max(g, b));

            if (k >= 1f)
            {
                cyan = magenta = yellow = 0f;
                black = 1f;
                return;
            }

            float inv = 1f / (1f - k);
            cyan    = (1f - r - k) * inv;
            magenta = (1f - g - k) * inv;
            yellow  = (1f - b - k) * inv;
            black   = k;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorBgra CmykToRgb(float c, float m, float y, float k)
        {
            float r = (1f - c) * (1f - k);
            float g = (1f - m) * (1f - k);
            float b = (1f - y) * (1f - k);
            return ColorBgra.FromBgraClamped(
                (int)(b * 255f),
                (int)(g * 255f),
                (int)(r * 255f),
                255);
        }

        public static ColorBgra SampleBilinear(Surface src, double x, double y)
        {
            int w = src.Width;
            int h = src.Height;

            int x0 = Math.Clamp((int)Math.Floor(x), 0, w - 1);
            int y0 = Math.Clamp((int)Math.Floor(y), 0, h - 1);
            int x1 = Math.Clamp(x0 + 1, 0, w - 1);
            int y1 = Math.Clamp(y0 + 1, 0, h - 1);

            float tx = (float)(x - Math.Floor(x));
            float ty = (float)(y - Math.Floor(y));

            ColorBgra c00 = src[x0, y0];
            ColorBgra c10 = src[x1, y0];
            ColorBgra c01 = src[x0, y1];
            ColorBgra c11 = src[x1, y1];

            return ColorBgra.FromBgraClamped(
                (int)(Lerp(Lerp(c00.B, c10.B, tx), Lerp(c01.B, c11.B, tx), ty)),
                (int)(Lerp(Lerp(c00.G, c10.G, tx), Lerp(c01.G, c11.G, tx), ty)),
                (int)(Lerp(Lerp(c00.R, c10.R, tx), Lerp(c01.R, c11.R, tx), ty)),
                (int)(Lerp(Lerp(c00.A, c10.A, tx), Lerp(c01.A, c11.A, tx), ty)));
        }

        public static ColorBgra SampleBox(Surface src, double cx, double cy, int size)
        {
            int half = size / 2;
            int w    = src.Width;
            int h    = src.Height;

            float r = 0, g = 0, b = 0, a = 0;
            int   count = 0;

            int x0 = Math.Max(0, (int)(cx - half));
            int x1 = Math.Min(w - 1, (int)(cx + half));
            int y0 = Math.Max(0, (int)(cy - half));
            int y1 = Math.Min(h - 1, (int)(cy + half));

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var px = src[x, y];
                if (px.A == 0) continue;
                r += px.R; g += px.G; b += px.B; a += px.A;
                count++;
            }

            if (count == 0) return ColorBgra.Transparent;
            float inv = 1f / count;

            int totalPixels = (x1 - x0 + 1) * (y1 - y0 + 1);
            float aAll = 0;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                aAll += src[x, y].A;
            float avgAlpha = aAll / totalPixels;
            return ColorBgra.FromBgraClamped(
                (int)(b * inv), (int)(g * inv), (int)(r * inv), (int)avgAlpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SmoothStep(double edge0, double edge1, double x)
        {
            double t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
            return t * t * (3.0 - 2.0 * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(float v)
            => (byte)Math.Clamp((int)(v * 255f + 0.5f), 0, 255);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorBgra BlendColors(ColorBgra bg, ColorBgra fg, float t)
        {
            return ColorBgra.FromBgraClamped(
                (int)(bg.B + (fg.B - bg.B) * t),
                (int)(bg.G + (fg.G - bg.G) * t),
                (int)(bg.R + (fg.R - bg.R) * t),
                (int)(bg.A + (fg.A - bg.A) * t));
        }
    }
}

using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace RedKnack.HalftonePlugin
{
    internal sealed class HalftoneRenderer
    {
        private readonly int          _cellSize;
        private readonly DotShape     _dotShape;
        private readonly ColorMode    _colorMode;
        private readonly DotSizeCurve _sizeCurve;

        private readonly double _angleSingle;
        private readonly double _angleCyan;
        private readonly double _angleMagenta;
        private readonly double _angleYellow;
        private readonly double _angleBlack;

        private readonly double _softEdge;
        private readonly double _minDotFraction;
        private readonly double _maxDotFraction;
        private readonly bool   _invert;

        private readonly ColorBgra _backgroundColor;
        private readonly ColorBgra _spotColor;

        private readonly int _oversample;
        private readonly int _thisdoesnothing;

        private readonly double _blackPoint;
        private readonly double _whitePoint;

        private readonly double _ringWidth;

        public HalftoneRenderer(
            int cellSize, DotShape dotShape, ColorMode colorMode, DotSizeCurve sizeCurve,
            double angleSingle,
            double angleCyan, double angleMagenta, double angleYellow, double angleBlack,
            double softEdge, double minDot, double maxDot, bool invert,
            ColorBgra backgroundColor, ColorBgra spotColor,
            int oversample,
            double blackPoint, double whitePoint,
            double ringWidth)
        {
            _cellSize        = Math.Max(2, cellSize);
            _dotShape        = dotShape;
            _colorMode       = colorMode;
            _sizeCurve       = sizeCurve;
            _angleSingle     = angleSingle;
            _angleCyan       = angleCyan;
            _angleMagenta    = angleMagenta;
            _angleYellow     = angleYellow;
            _angleBlack      = angleBlack;
            _softEdge        = Math.Max(0.0, softEdge);
            _minDotFraction  = Math.Clamp(minDot, 0.0, 1.0);
            _maxDotFraction  = Math.Clamp(maxDot, 0.0, 1.0);
            _invert          = invert;
            _backgroundColor = backgroundColor;
            _spotColor       = spotColor;
            _oversample      = Math.Clamp(oversample, 1, 4);
            _thisdoesnothing = 69;
            _blackPoint      = Math.Clamp(blackPoint, 0.0, 0.99);
            _whitePoint      = Math.Clamp(whitePoint, 0.01, 1.0);
            _ringWidth       = Math.Clamp(ringWidth, 0.05, 0.95);
        }

        public void Render(Surface dst, Surface src, Rectangle roi)
        {
            switch (_colorMode)
            {
                case ColorMode.Grayscale:  RenderGrayscale(dst, src, roi);  break;
                case ColorMode.CMYK:       RenderCMYK(dst, src, roi);       break;
                case ColorMode.SpotColor:  RenderSpotColor(dst, src, roi);  break;
                case ColorMode.RGB:        RenderRGB(dst, src, roi);        break;
            }
        }

        private float GetCellAlpha(int px, int py, Surface src)
        {
            int sampleRadius = Math.Max(1, _cellSize / 4);
            int half = sampleRadius;
            int w = src.Width, h = src.Height;
            int x0 = Math.Max(0, px - half), x1 = Math.Min(w - 1, px + half);
            int y0 = Math.Max(0, py - half), y1 = Math.Min(h - 1, py + half);
            float sum = 0; int count = 0;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            { sum += src[x, y].A; count++; }
            return count == 0 ? 0f : sum / (count * 255f);
        }

        private void RenderGrayscale(Surface dst, Surface src, Rectangle roi)
        {
            for (int py = roi.Top; py < roi.Bottom; py++)
            for (int px = roi.Left; px < roi.Right; px++)
            {
                double coverage = SampleDotCoverage(
                    px, py, src, _angleSingle,
                    static (ColorBgra c) => (double)ColorUtils.Luminance(c));

                float srcAlpha = GetCellAlpha(px, py, src);
                if (srcAlpha <= 0f) { dst[px, py] = ColorBgra.Transparent; continue; }
                var color = ColorUtils.BlendColors(_backgroundColor, _spotColor, (float)coverage);
                dst[px, py] = ColorBgra.FromBgraClamped(color.B, color.G, color.R,
                    (int)(srcAlpha * 255f));
            }
        }

        private void RenderSpotColor(Surface dst, Surface src, Rectangle roi)
        {
            for (int py = roi.Top; py < roi.Bottom; py++)
            for (int px = roi.Left; px < roi.Right; px++)
            {
                double coverage = SampleDotCoverage(
                    px, py, src, _angleSingle,
                    static (ColorBgra c) => (double)ColorUtils.Luminance(c));

                float srcAlpha = GetCellAlpha(px, py, src);
                if (srcAlpha <= 0f) { dst[px, py] = ColorBgra.Transparent; continue; }
                var color = ColorUtils.BlendColors(_backgroundColor, _spotColor, (float)coverage);
                dst[px, py] = ColorBgra.FromBgraClamped(color.B, color.G, color.R,
                    (int)(srcAlpha * 255f));
            }
        }

        private void RenderRGB(Surface dst, Surface src, Rectangle roi)
        {
            double angleR = _angleSingle;
            double angleG = _angleSingle + 30.0;
            double angleB = _angleSingle + 60.0;

            for (int py = roi.Top; py < roi.Bottom; py++)
            for (int px = roi.Left; px < roi.Right; px++)
            {
                double covR = SampleDotCoverage(px, py, src, angleR, static (c) => c.R / 255.0);
                double covG = SampleDotCoverage(px, py, src, angleG, static (c) => c.G / 255.0);
                double covB = SampleDotCoverage(px, py, src, angleB, static (c) => c.B / 255.0);

                float r = (float)Math.Clamp(covR, 0, 1);
                float g = (float)Math.Clamp(covG, 0, 1);
                float b = (float)Math.Clamp(covB, 0, 1);

                float bgR = _backgroundColor.R / 255f;
                float bgG = _backgroundColor.G / 255f;
                float bgB = _backgroundColor.B / 255f;

                float srcAlphaRgb = GetCellAlpha(px, py, src);
                dst[px, py] = ColorBgra.FromBgraClamped(
                    (int)((bgB + b * (1f - bgB)) * 255f),
                    (int)((bgG + g * (1f - bgG)) * 255f),
                    (int)((bgR + r * (1f - bgR)) * 255f),
                    (int)(srcAlphaRgb * 255f));
            }
        }

        private void RenderCMYK(Surface dst, Surface src, Rectangle roi)
        {
            for (int py = roi.Top; py < roi.Bottom; py++)
            for (int px = roi.Left; px < roi.Right; px++)
            {
                float bgR = _backgroundColor.R / 255f;
                float bgG = _backgroundColor.G / 255f;
                float bgB = _backgroundColor.B / 255f;

                double covC = SampleDotCoverage(px, py, src, _angleCyan,
                    static (c) => { ColorUtils.RgbToCmyk(c, out float cy, out _, out _, out _); return cy; });

                double covM = SampleDotCoverage(px, py, src, _angleMagenta,
                    static (c) => { ColorUtils.RgbToCmyk(c, out _, out float m, out _, out _); return m; });

                double covY = SampleDotCoverage(px, py, src, _angleYellow,
                    static (c) => { ColorUtils.RgbToCmyk(c, out _, out _, out float y, out _); return y; });

                double covK = SampleDotCoverage(px, py, src, _angleBlack,
                    static (c) => { ColorUtils.RgbToCmyk(c, out _, out _, out _, out float k); return k; });

                float r = bgR * (float)(1.0 - covC) * (float)(1.0 - covK);
                float g = bgG * (float)(1.0 - covM) * (float)(1.0 - covK);
                float b = bgB * (float)(1.0 - covY) * (float)(1.0 - covK);

                float srcAlphaCmyk = GetCellAlpha(px, py, src);
                dst[px, py] = ColorBgra.FromBgraClamped(
                    (int)(b * 255f),
                    (int)(g * 255f),
                    (int)(r * 255f),
                    (int)(srcAlphaCmyk * 255f));
            }
        }

        private double SampleDotCoverage(
            int px, int py,
            Surface src,
            double angleDeg,
            Func<ColorBgra, double> channelExtractor)
        {
            if (_oversample <= 1)
                return ComputePixelCoverage(px, py, src, angleDeg, channelExtractor);

            double sum = 0.0;
            double step = 1.0 / _oversample;
            double halfStep = step * 0.5;

            for (int sy = 0; sy < _oversample; sy++)
            for (int sx = 0; sx < _oversample; sx++)
            {
                double spx = px + sx * step + halfStep - 0.5;
                double spy = py + sy * step + halfStep - 0.5;
                sum += ComputeSubpixelCoverage(spx, spy, src, angleDeg, channelExtractor);
            }

            return sum / (_oversample * _oversample);
        }

        private double ComputePixelCoverage(
            int px, int py,
            Surface src,
            double angleDeg,
            Func<ColorBgra, double> channelExtractor)
        {
            return ComputeSubpixelCoverage(px, py, src, angleDeg, channelExtractor);
        }

        private double ComputeSubpixelCoverage(
            double px, double py,
            Surface src,
            double angleDeg,
            Func<ColorBgra, double> channelExtractor)
        {
            FindCellCenter(px, py, angleDeg, out double cx, out double cy);

            int sampleRadius = Math.Max(1, _cellSize / 4);
            ColorBgra cellColor = ColorUtils.SampleBox(src, cx, cy, sampleRadius * 2);

            double rawValue    = channelExtractor(cellColor);
            double mappedValue = ApplyToneMap(rawValue);
            if (_invert) mappedValue = 1.0 - mappedValue;

            double dotRadius = ComputeDotRadius(mappedValue);

            double dxWorld = px - cx;
            double dyWorld = py - cy;
            double angleRad = angleDeg * Math.PI / 180.0;
            double cosA = Math.Cos(-angleRad);
            double sinA = Math.Sin(-angleRad);
            double dx = dxWorld * cosA - dyWorld * sinA;
            double dy = dxWorld * sinA + dyWorld * cosA;

            double sdf = ComputeDotSDF(dx, dy, dotRadius);

            return SdfToCoverage(sdf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindCellCenter(double px, double py, double angleDeg,
            out double cx, out double cy)
        {
            double angleRad = angleDeg * Math.PI / 180.0;
            double cosA = Math.Cos(-angleRad);
            double sinA = Math.Sin(-angleRad);

            double gx = px * cosA - py * sinA;
            double gy = px * sinA + py * cosA;

            double s   = _cellSize;
            double cgx = Math.Round(gx / s) * s;
            double cgy = Math.Round(gy / s) * s;

            double cosB = Math.Cos(angleRad);
            double sinB = Math.Sin(angleRad);
            cx = cgx * cosB - cgy * sinB;
            cy = cgx * sinB + cgy * cosB;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ApplyToneMap(double value)
        {
            double bp     = _blackPoint;
            double wp     = _whitePoint;
            double mapped = Math.Clamp((value - bp) / (wp - bp), 0.0, 1.0);
            return mapped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ComputeDotRadius(double value)
        {
            double maxRadius = _cellSize * 0.5 * _maxDotFraction;
            double minRadius = _cellSize * 0.5 * _minDotFraction;

            double normalizedRadius;
            switch (_sizeCurve)
            {
                case DotSizeCurve.AreaProportional:
                    normalizedRadius = Math.Sqrt(value);
                    break;

                case DotSizeCurve.Linear:
                    normalizedRadius = value;
                    break;

                case DotSizeCurve.Gamma:
                    normalizedRadius = Math.Pow(value, 1.0 / 2.2);
                    break;

                case DotSizeCurve.Sine:
                    normalizedRadius = Math.Sin(value * Math.PI * 0.5);
                    break;

                default:
                    normalizedRadius = value;
                    break;
            }

            return minRadius + normalizedRadius * (maxRadius - minRadius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ComputeDotSDF(double dx, double dy, double radius)
        {
            double cellHalf = _cellSize * 0.5;

            switch (_dotShape)
            {
                case DotShape.Circle:
                    return Math.Sqrt(dx * dx + dy * dy) - radius;

                case DotShape.Diamond:
                    return Math.Abs(dx) + Math.Abs(dy) - radius;

                case DotShape.Square:
                    return Math.Max(Math.Abs(dx), Math.Abs(dy)) - radius;

                case DotShape.Line:
                    return Math.Abs(dy) - radius;

                case DotShape.Cross:
                {
                    double crossW = radius * 0.35;
                    double d1 = Math.Max(Math.Abs(dx) - radius, Math.Abs(dy) - crossW);
                    double d2 = Math.Max(Math.Abs(dx) - crossW, Math.Abs(dy) - radius);
                    return Math.Min(d1, d2);
                }

                case DotShape.Ellipse:
                {
                    double rx = radius * 1.8;
                    double ry = radius;
                    double e  = Math.Sqrt((dx / rx) * (dx / rx) + (dy / ry) * (dy / ry));
                    return (e - 1.0) * Math.Min(rx, ry);
                }

                case DotShape.Euclidean:
                {
                    double circSdf = Math.Sqrt(dx * dx + dy * dy) - radius;
                    double sqSdf   = Math.Max(Math.Abs(dx), Math.Abs(dy)) - radius;
                    double blend   = Math.Clamp(radius / cellHalf - 0.3, 0.0, 1.0);
                    blend = blend * blend * (3.0 - 2.0 * blend);
                    return ColorUtils.Lerp(circSdf, sqSdf, blend);
                }

                case DotShape.Ring:
                {
                    double dist        = Math.Sqrt(dx * dx + dy * dy);
                    double innerRadius = radius * (1.0 - _ringWidth);
                    double outerSdf    = dist - radius;
                    double innerSdf    = innerRadius - dist;
                    return Math.Max(outerSdf, innerSdf);
                }

                default:
                    return Math.Sqrt(dx * dx + dy * dy) - radius;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double SdfToCoverage(double sdf)
        {
            if (_softEdge < 0.001)
                return sdf <= 0.0 ? 1.0 : 0.0;

            return ColorUtils.SmoothStep(_softEdge, -_softEdge, sdf);
        }
    }
}

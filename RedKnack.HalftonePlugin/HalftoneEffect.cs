using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RedKnack.HalftonePlugin
{
    public enum PropertyNames
    {
        CellSize,
        DotShape,
        DotSizeCurve,
        ColorMode,

        ScreenAngle,
        AngleCyan,
        AngleMagenta,
        AngleYellow,
        AngleBlack,

        SoftEdge,
        MinDotSize,
        MaxDotSize,
        Invert,
        RingWidth,

        BackgroundR,
        BackgroundG,
        BackgroundB,
        SpotR,
        SpotG,
        SpotB,

        BlackPoint,
        WhitePoint,

        OversampleFactor,
        ThisDoesNothingLol,
    }

    public sealed class HalftoneEffect : PropertyBasedEffect
    {
        private int          _cellSize;
        private DotShape     _dotShape;
        private DotSizeCurve _sizeCurve;
        private ColorMode    _colorMode;

        private double _angleSingle;
        private double _angleCyan, _angleMagenta, _angleYellow, _angleBlack;

        private double _softEdge;
        private double _minDot, _maxDot;
        private bool   _invert;
        private double _ringWidth;

        private ColorBgra _backgroundColor;
        private ColorBgra _spotColor;

        private double _blackPoint, _whitePoint;
        private int    _oversample;

        private HalftoneRenderer? _renderer;

        public HalftoneEffect()
            : base(
                "Halftone Comic/Print",
                LoadIcon(),
                "Stylize",
                new EffectOptions { Flags = EffectFlags.Configurable })
        {
        }

        private static Bitmap LoadIcon()
        {
            var stream = typeof(HalftoneEffect).Assembly
                .GetManifestResourceStream("RedKnack.HalftonePlugin.icon.png");
            return stream is null ? new Bitmap(1, 1) : new Bitmap(stream);
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            object[] dotShapes  = Enum.GetValues(typeof(DotShape))   .Cast<object>().ToArray();
            object[] sizeCurves = Enum.GetValues(typeof(DotSizeCurve)).Cast<object>().ToArray();
            object[] colorModes = Enum.GetValues(typeof(ColorMode))  .Cast<object>().ToArray();

            var props = new List<Property>
            {
                new Int32Property(PropertyNames.CellSize, 20, 2, 120),
                new StaticListChoiceProperty(PropertyNames.DotShape,     dotShapes,  (int)DotShape.Circle,              false),
                new StaticListChoiceProperty(PropertyNames.DotSizeCurve, sizeCurves, (int)DotSizeCurve.AreaProportional, false),
                new StaticListChoiceProperty(PropertyNames.ColorMode,    colorModes, (int)ColorMode.CMYK,               false),

                new DoubleProperty(PropertyNames.ScreenAngle,   45.0,   0.0, 179.0),
                new DoubleProperty(PropertyNames.AngleCyan,     15.0,   0.0, 179.0),
                new DoubleProperty(PropertyNames.AngleMagenta,  75.0,   0.0, 179.0),
                new DoubleProperty(PropertyNames.AngleYellow,    0.0,   0.0, 179.0),
                new DoubleProperty(PropertyNames.AngleBlack,    45.0,   0.0, 179.0),

                new DoubleProperty(PropertyNames.SoftEdge,    1.5,   0.0,  10.0),
                new DoubleProperty(PropertyNames.MinDotSize,  0.0,   0.0,  99.0),
                new DoubleProperty(PropertyNames.MaxDotSize, 95.0,   1.0, 100.0),
                new BooleanProperty(PropertyNames.Invert, false),
                new DoubleProperty(PropertyNames.RingWidth, 0.3,   0.05,  0.95),

                new Int32Property(PropertyNames.BackgroundR, 255, 0, 255),
                new Int32Property(PropertyNames.BackgroundG, 255, 0, 255),
                new Int32Property(PropertyNames.BackgroundB, 255, 0, 255),

                new Int32Property(PropertyNames.SpotR, 0, 0, 255),
                new Int32Property(PropertyNames.SpotG, 0, 0, 255),
                new Int32Property(PropertyNames.SpotB, 0, 0, 255),

                new DoubleProperty(PropertyNames.BlackPoint,   0.0,  0.0,  49.0),
                new DoubleProperty(PropertyNames.WhitePoint, 100.0, 51.0, 100.0),

                new Int32Property(PropertyNames.OversampleFactor, 2, 1, 4),
            };

            var rules = new PropertyCollectionRule[]
            {
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.ScreenAngle,
                    PropertyNames.ColorMode,
                    (object)ColorMode.CMYK,
                    true),

                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.AngleCyan,    PropertyNames.ColorMode, (object)ColorMode.CMYK, false),
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.AngleMagenta, PropertyNames.ColorMode, (object)ColorMode.CMYK, false),
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.AngleYellow,  PropertyNames.ColorMode, (object)ColorMode.CMYK, false),
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.AngleBlack,   PropertyNames.ColorMode, (object)ColorMode.CMYK, false),

                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.SpotR, PropertyNames.ColorMode, (object)ColorMode.SpotColor, false),
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.SpotG, PropertyNames.ColorMode, (object)ColorMode.SpotColor, false),
                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.SpotB, PropertyNames.ColorMode, (object)ColorMode.SpotColor, false),

                new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.RingWidth, PropertyNames.DotShape, (object)DotShape.Ring, false),
            };

            return new PropertyCollection(props, rules);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo ui = CreateDefaultConfigUI(props);

            SetLabel(ui, PropertyNames.CellSize,         "Cell Size (px)");
            SetLabel(ui, PropertyNames.DotShape,         "Dot Shape");
            SetLabel(ui, PropertyNames.DotSizeCurve,     "Tone Curve");
            SetLabel(ui, PropertyNames.ColorMode,        "Color Mode");

            SetLabel(ui, PropertyNames.ScreenAngle,      "Screen Angle [deg] — Grayscale / Spot / RGB");
            SetLabel(ui, PropertyNames.AngleCyan,        "Cyan Angle [deg]");
            SetLabel(ui, PropertyNames.AngleMagenta,     "Magenta Angle [deg]");
            SetLabel(ui, PropertyNames.AngleYellow,      "Yellow Angle [deg]");
            SetLabel(ui, PropertyNames.AngleBlack,       "Black Angle [deg]");

            SetLabel(ui, PropertyNames.SoftEdge,         "Edge Softness (px)");
            SetLabel(ui, PropertyNames.MinDotSize,       "Min Dot Size (%)");
            SetLabel(ui, PropertyNames.MaxDotSize,       "Max Dot Size (%)");
            SetLabel(ui, PropertyNames.Invert,           "Invert");
            SetLabel(ui, PropertyNames.RingWidth,        "Ring Width [Ring shape only]");

            SetLabel(ui, PropertyNames.BackgroundR,      "Background Red");
            SetLabel(ui, PropertyNames.BackgroundG,      "Background Green");
            SetLabel(ui, PropertyNames.BackgroundB,      "Background Blue");

            SetLabel(ui, PropertyNames.SpotR,            "Spot Color Red [SpotColor only]");
            SetLabel(ui, PropertyNames.SpotG,            "Spot Color Green [SpotColor only]");
            SetLabel(ui, PropertyNames.SpotB,            "Spot Color Blue [SpotColor only]");

            SetLabel(ui, PropertyNames.BlackPoint,       "Black Point (%)");
            SetLabel(ui, PropertyNames.WhitePoint,       "White Point (%)");

            SetLabel(ui, PropertyNames.OversampleFactor, "Quality / Oversampling (1=Fast, 4=Max)");
            SetLabel(ui, PropertyNames.ThisDoesNothingLol, "Skibidi");

            return ui;
        }

        private static void SetLabel(ControlInfo ui, PropertyNames name, string label)
        {
            ui.SetPropertyControlValue(name, ControlInfoPropertyNames.DisplayName, label);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            _cellSize  = token.GetProperty<Int32Property>(PropertyNames.CellSize).Value;
            _dotShape  = (DotShape)     token.GetProperty<StaticListChoiceProperty>(PropertyNames.DotShape).Value;
            _sizeCurve = (DotSizeCurve) token.GetProperty<StaticListChoiceProperty>(PropertyNames.DotSizeCurve).Value;
            _colorMode = (ColorMode)    token.GetProperty<StaticListChoiceProperty>(PropertyNames.ColorMode).Value;

            _angleSingle  = token.GetProperty<DoubleProperty>(PropertyNames.ScreenAngle).Value;
            _angleCyan    = token.GetProperty<DoubleProperty>(PropertyNames.AngleCyan).Value;
            _angleMagenta = token.GetProperty<DoubleProperty>(PropertyNames.AngleMagenta).Value;
            _angleYellow  = token.GetProperty<DoubleProperty>(PropertyNames.AngleYellow).Value;
            _angleBlack   = token.GetProperty<DoubleProperty>(PropertyNames.AngleBlack).Value;

            _softEdge  = token.GetProperty<DoubleProperty>(PropertyNames.SoftEdge).Value;
            _minDot    = token.GetProperty<DoubleProperty>(PropertyNames.MinDotSize).Value / 100.0;
            _maxDot    = token.GetProperty<DoubleProperty>(PropertyNames.MaxDotSize).Value / 100.0;
            _invert    = token.GetProperty<BooleanProperty>(PropertyNames.Invert).Value;
            _ringWidth = token.GetProperty<DoubleProperty>(PropertyNames.RingWidth).Value;

            int bgR = token.GetProperty<Int32Property>(PropertyNames.BackgroundR).Value;
            int bgG = token.GetProperty<Int32Property>(PropertyNames.BackgroundG).Value;
            int bgB = token.GetProperty<Int32Property>(PropertyNames.BackgroundB).Value;
            _backgroundColor = ColorBgra.FromBgr((byte)bgB, (byte)bgG, (byte)bgR);

            int sR = token.GetProperty<Int32Property>(PropertyNames.SpotR).Value;
            int sG = token.GetProperty<Int32Property>(PropertyNames.SpotG).Value;
            int sB = token.GetProperty<Int32Property>(PropertyNames.SpotB).Value;
            _spotColor = ColorBgra.FromBgr((byte)sB, (byte)sG, (byte)sR);

            _blackPoint = token.GetProperty<DoubleProperty>(PropertyNames.BlackPoint).Value / 100.0;
            _whitePoint = token.GetProperty<DoubleProperty>(PropertyNames.WhitePoint).Value / 100.0;
            _oversample = token.GetProperty<Int32Property>(PropertyNames.OversampleFactor).Value;

            _renderer = new HalftoneRenderer(
                _cellSize, _dotShape, _colorMode, _sizeCurve,
                _angleSingle,
                _angleCyan, _angleMagenta, _angleYellow, _angleBlack,
                _softEdge, _minDot, _maxDot, _invert,
                _backgroundColor, _spotColor,
                _oversample,
                _blackPoint, _whitePoint,
                _ringWidth);

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (_renderer is null) return;

            for (int i = startIndex; i < startIndex + length; i++)
            {
                _renderer.Render(DstArgs.Surface, SrcArgs.Surface, rois[i]);
            }
        }
    }
}

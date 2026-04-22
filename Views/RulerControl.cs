using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace LabelStudio.Views;

/// <summary>
/// Mm ruler that lives inside a LayoutTransformControl so it scales with canvas zoom.
/// </summary>
public class RulerControl : Control
{
    private const double MmToPx   = 3.7795;
    private const double Thickness = 18;   // ruler cross-axis size (px)

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<RulerControl, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(Length), 300);

    static RulerControl()
    {
        AffectsRender<RulerControl>(OrientationProperty, LengthProperty);
        AffectsMeasure<RulerControl>(OrientationProperty, LengthProperty);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        Orientation == Orientation.Horizontal
            ? new Size(Length, Thickness)
            : new Size(Thickness, Length);

    public override void Render(DrawingContext ctx)
    {
        var bounds = Bounds;
        bool isH  = Orientation == Orientation.Horizontal;
        double len = isH ? bounds.Width : bounds.Height;
        double th  = Thickness;

        // ── Background & border ────────────────────────────────────────
        ctx.FillRectangle(new SolidColorBrush(Color.Parse("#F5F5FA")), bounds);

        var borderBrush = new SolidColorBrush(Color.Parse("#D8D8E2"));
        if (isH)
            ctx.FillRectangle(borderBrush, new Rect(0, th - 1, len, 1));
        else
            ctx.FillRectangle(borderBrush, new Rect(th - 1, 0, 1, len));

        var tickBrush = new SolidColorBrush(Color.Parse("#B0B0C4"));
        var textBrush = new SolidColorBrush(Color.Parse("#8888A8"));
        var typeface  = new Typeface("Inter,Segoe UI,Arial");
        const double fontSize = 7;

        double totalMm = len / MmToPx;
        // Show every 1 mm — at 3.7795 px/mm that's ~4 px per tick, fine enough
        int minorStep = 1;
        int majorStep = 10;
        int midStep   = 5;

        for (int mm = 0; mm <= (int)Math.Ceiling(totalMm); mm++)
        {
            if (mm % minorStep != 0) continue;
            double pos = mm * MmToPx;
            if (pos > len) break;

            bool   isMajor = mm % majorStep == 0;
            bool   isMid   = mm % midStep   == 0 && !isMajor;
            double tickLen = isMajor ? th * 0.50 : isMid ? th * 0.35 : th * 0.20;

            if (isH)
            {
                // Tick grows from bottom edge upward
                ctx.FillRectangle(tickBrush, new Rect(pos, th - tickLen, 1, tickLen));

                if (isMajor && mm > 0)
                {
                    var ft = new FormattedText($"{mm}",
                        CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, textBrush);
                    ctx.DrawText(ft, new Point(pos + 2, 1));
                }
            }
            else
            {
                // Tick grows from right edge leftward
                ctx.FillRectangle(tickBrush, new Rect(th - tickLen, pos, tickLen, 1));

                if (isMajor && mm > 0)
                {
                    var ft = new FormattedText($"{mm}",
                        CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, textBrush);

                    // Rotate -90° so text reads bottom-to-top along the vertical ruler.
                    // Sequence: rotate around origin, then translate to (right-edge, pos).
                    using (ctx.PushTransform(
                        Matrix.CreateRotation(-Math.PI / 2) *
                        Matrix.CreateTranslation(th - 2, pos)))
                    {
                        // After rotation x→-y, y→x.
                        // Drawing at (1, -height) places text just below the tick
                        // and flush to the right edge.
                        ctx.DrawText(ft, new Point(1, -ft.Height));
                    }
                }
            }
        }
    }
}

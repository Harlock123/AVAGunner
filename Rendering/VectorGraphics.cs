using System;
using Avalonia;
using Avalonia.Media;

namespace AVAGunner.Rendering;

public static class VectorGraphics
{
    public static readonly Color CyanNeon = Color.FromRgb(0, 255, 255);
    public static readonly Color MagentaNeon = Color.FromRgb(255, 0, 255);
    public static readonly Color YellowNeon = Color.FromRgb(255, 255, 0);
    public static readonly Color GreenNeon = Color.FromRgb(0, 255, 128);
    public static readonly Color OrangeNeon = Color.FromRgb(255, 128, 0);
    public static readonly Color RedNeon = Color.FromRgb(255, 64, 64);

    public static void DrawGlowLine(DrawingContext ctx, Point start, Point end, Color color, double baseThickness = 2, int glowLayers = 3, double glowSpread = 3)
    {
        // Draw glow layers (outer to inner)
        for (var i = glowLayers; i >= 0; i--)
        {
            var alpha = (byte)(80 / (i + 1));
            var thickness = baseThickness + i * glowSpread;
            var glowColor = Color.FromArgb(alpha, color.R, color.G, color.B);
            var pen = new Pen(new SolidColorBrush(glowColor), thickness, lineCap: PenLineCap.Round);
            ctx.DrawLine(pen, start, end);
        }

        // Draw core line
        var corePen = new Pen(new SolidColorBrush(color), baseThickness, lineCap: PenLineCap.Round);
        ctx.DrawLine(corePen, start, end);

        // Draw bright center
        var brightColor = Color.FromArgb(255,
            (byte)Math.Min(255, color.R + 50),
            (byte)Math.Min(255, color.G + 50),
            (byte)Math.Min(255, color.B + 50));
        var brightPen = new Pen(new SolidColorBrush(brightColor), baseThickness * 0.5, lineCap: PenLineCap.Round);
        ctx.DrawLine(brightPen, start, end);
    }

    public static void DrawGlowCircle(DrawingContext ctx, Point center, double radius, Color color, double baseThickness = 2)
    {
        // Draw glow layers
        for (var i = 3; i >= 0; i--)
        {
            var alpha = (byte)(80 / (i + 1));
            var thickness = baseThickness + i * 3;
            var glowColor = Color.FromArgb(alpha, color.R, color.G, color.B);
            var pen = new Pen(new SolidColorBrush(glowColor), thickness);
            ctx.DrawEllipse(null, pen, center, radius, radius);
        }

        // Draw core
        var corePen = new Pen(new SolidColorBrush(color), baseThickness);
        ctx.DrawEllipse(null, corePen, center, radius, radius);
    }

    public static void DrawGlowPolygon(DrawingContext ctx, Point[] points, Color color, double baseThickness = 2, bool closed = true)
    {
        if (points.Length < 2) return;

        for (var i = 0; i < points.Length - 1; i++)
        {
            DrawGlowLine(ctx, points[i], points[i + 1], color, baseThickness);
        }

        if (closed && points.Length > 2)
        {
            DrawGlowLine(ctx, points[^1], points[0], color, baseThickness);
        }
    }

    public static void DrawGlowText(DrawingContext ctx, string text, Point position, Color color, double fontSize = 24)
    {
        var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);

        // Round position to pixel boundaries for crisp text
        var pixelPos = new Point(Math.Round(position.X), Math.Round(position.Y));

        // Draw subtle outer glow only (reduced blur)
        var glowColor = Color.FromArgb(60, color.R, color.G, color.B);
        var glowText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            new SolidColorBrush(glowColor));

        // Draw glow at cardinal directions only (less blur than full grid)
        ctx.DrawText(glowText, new Point(pixelPos.X - 2, pixelPos.Y));
        ctx.DrawText(glowText, new Point(pixelPos.X + 2, pixelPos.Y));
        ctx.DrawText(glowText, new Point(pixelPos.X, pixelPos.Y - 2));
        ctx.DrawText(glowText, new Point(pixelPos.X, pixelPos.Y + 2));

        // Draw core text at exact pixel position
        var coreText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            new SolidColorBrush(color));
        ctx.DrawText(coreText, pixelPos);

        // Draw bright highlight for extra crispness
        var brightColor = Color.FromArgb(200,
            (byte)Math.Min(255, color.R + 80),
            (byte)Math.Min(255, color.G + 80),
            (byte)Math.Min(255, color.B + 80));
        var brightText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            new SolidColorBrush(brightColor));
        ctx.DrawText(brightText, pixelPos);
    }

    public static void DrawReticle(DrawingContext ctx, Point center, double size, Color color)
    {
        var halfSize = size / 2;

        // Outer circle
        DrawGlowCircle(ctx, center, halfSize, color, 1.5);

        // Inner circle
        DrawGlowCircle(ctx, center, halfSize * 0.3, color, 1);

        // Crosshairs
        var gap = halfSize * 0.4;
        DrawGlowLine(ctx, new Point(center.X - halfSize, center.Y), new Point(center.X - gap, center.Y), color, 1.5);
        DrawGlowLine(ctx, new Point(center.X + gap, center.Y), new Point(center.X + halfSize, center.Y), color, 1.5);
        DrawGlowLine(ctx, new Point(center.X, center.Y - halfSize), new Point(center.X, center.Y - gap), color, 1.5);
        DrawGlowLine(ctx, new Point(center.X, center.Y + gap), new Point(center.X, center.Y + halfSize), color, 1.5);

        // Corner accents
        var cornerSize = halfSize * 0.2;
        var cornerOffset = halfSize * 0.85;

        DrawCorner(ctx, new Point(center.X - cornerOffset, center.Y - cornerOffset), cornerSize, color, true, true);
        DrawCorner(ctx, new Point(center.X + cornerOffset, center.Y - cornerOffset), cornerSize, color, false, true);
        DrawCorner(ctx, new Point(center.X - cornerOffset, center.Y + cornerOffset), cornerSize, color, true, false);
        DrawCorner(ctx, new Point(center.X + cornerOffset, center.Y + cornerOffset), cornerSize, color, false, false);
    }

    private static void DrawCorner(DrawingContext ctx, Point corner, double size, Color color, bool left, bool top)
    {
        var hDir = left ? 1 : -1;
        var vDir = top ? 1 : -1;

        DrawGlowLine(ctx, corner, new Point(corner.X + size * hDir, corner.Y), color, 1);
        DrawGlowLine(ctx, corner, new Point(corner.X, corner.Y + size * vDir), color, 1);
    }

    public static void DrawFighter(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Main fuselage - sleek arrow shape
        var fuselage = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size), center, rotation),
            RotatePoint(new Point(center.X + size * 0.15, center.Y - size * 0.5), center, rotation),
            RotatePoint(new Point(center.X + size * 0.2, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X - size * 0.2, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.15, center.Y - size * 0.5), center, rotation)
        };
        DrawGlowPolygon(ctx, fuselage, color, 1.5);

        // Left wing
        var leftWing = new[]
        {
            RotatePoint(new Point(center.X - size * 0.15, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X - size * 0.8, center.Y + size * 0.5), center, rotation),
            RotatePoint(new Point(center.X - size * 0.7, center.Y + size * 0.7), center, rotation),
            RotatePoint(new Point(center.X - size * 0.2, center.Y + size * 0.3), center, rotation)
        };
        DrawGlowPolygon(ctx, leftWing, color, 1.5);

        // Right wing
        var rightWing = new[]
        {
            RotatePoint(new Point(center.X + size * 0.15, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X + size * 0.8, center.Y + size * 0.5), center, rotation),
            RotatePoint(new Point(center.X + size * 0.7, center.Y + size * 0.7), center, rotation),
            RotatePoint(new Point(center.X + size * 0.2, center.Y + size * 0.3), center, rotation)
        };
        DrawGlowPolygon(ctx, rightWing, color, 1.5);

        // Cockpit window
        var cockpit = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.1, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.1, center.Y - size * 0.3), center, rotation)
        };
        DrawGlowPolygon(ctx, cockpit, color, 1);

        // Engine exhausts
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.1, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.1, center.Y + size * 0.9), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X + size * 0.1, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.1, center.Y + size * 0.9), center, rotation),
            color, 1);
    }

    public static void DrawBomber(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Main hull - heavy freighter shape
        var hull = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 0.8), center, rotation),
            RotatePoint(new Point(center.X + size * 0.4, center.Y - size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y + size * 0.5), center, rotation),
            RotatePoint(new Point(center.X + size * 0.3, center.Y + size * 0.8), center, rotation),
            RotatePoint(new Point(center.X - size * 0.3, center.Y + size * 0.8), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y + size * 0.5), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X - size * 0.4, center.Y - size * 0.6), center, rotation)
        };
        DrawGlowPolygon(ctx, hull, color, 2);

        // Left engine nacelle
        var leftEngine = new[]
        {
            RotatePoint(new Point(center.X - size * 0.5, center.Y - size * 0.1), center, rotation),
            RotatePoint(new Point(center.X - size * 0.9, center.Y + size * 0.1), center, rotation),
            RotatePoint(new Point(center.X - size * 0.9, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y + size * 0.5), center, rotation)
        };
        DrawGlowPolygon(ctx, leftEngine, color, 1.5);

        // Right engine nacelle
        var rightEngine = new[]
        {
            RotatePoint(new Point(center.X + size * 0.5, center.Y - size * 0.1), center, rotation),
            RotatePoint(new Point(center.X + size * 0.9, center.Y + size * 0.1), center, rotation),
            RotatePoint(new Point(center.X + size * 0.9, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y + size * 0.5), center, rotation)
        };
        DrawGlowPolygon(ctx, rightEngine, color, 1.5);

        // Cockpit section
        var cockpit = new[]
        {
            RotatePoint(new Point(center.X - size * 0.2, center.Y - size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.2, center.Y - size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.15, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.15, center.Y - size * 0.3), center, rotation)
        };
        DrawGlowPolygon(ctx, cockpit, color, 1);

        // Cargo bay lines
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.3, center.Y), center, rotation),
            RotatePoint(new Point(center.X + size * 0.3, center.Y), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.3, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.3, center.Y + size * 0.3), center, rotation),
            color, 1);

        // Engine exhausts
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.7, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.7, center.Y + size * 1.0), center, rotation),
            color, 1.5);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X + size * 0.7, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.7, center.Y + size * 1.0), center, rotation),
            color, 1.5);
    }

    public static void DrawInterceptor(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Central fuselage - needle shape
        var fuselage = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 1.1), center, rotation),
            RotatePoint(new Point(center.X + size * 0.12, center.Y - size * 0.4), center, rotation),
            RotatePoint(new Point(center.X + size * 0.15, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.15, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X - size * 0.12, center.Y - size * 0.4), center, rotation)
        };
        DrawGlowPolygon(ctx, fuselage, color, 1.5);

        // Left swept wing
        var leftWing = new[]
        {
            RotatePoint(new Point(center.X - size * 0.1, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X - size * 1.0, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X - size * 0.9, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X - size * 0.15, center.Y + size * 0.2), center, rotation)
        };
        DrawGlowPolygon(ctx, leftWing, color, 1.5);

        // Right swept wing
        var rightWing = new[]
        {
            RotatePoint(new Point(center.X + size * 0.1, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X + size * 1.0, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X + size * 0.9, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X + size * 0.15, center.Y + size * 0.2), center, rotation)
        };
        DrawGlowPolygon(ctx, rightWing, color, 1.5);

        // Wing-mounted weapons/engines
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.6, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.6, center.Y + size * 0.7), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X + size * 0.6, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.6, center.Y + size * 0.7), center, rotation),
            color, 1);

        // Cockpit canopy
        var cockpit = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 0.7), center, rotation),
            RotatePoint(new Point(center.X + size * 0.08, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.08, center.Y - size * 0.3), center, rotation)
        };
        DrawGlowPolygon(ctx, cockpit, color, 1);

        // Tail fins
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.25, center.Y + size * 0.8), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.25, center.Y + size * 0.8), center, rotation),
            color, 1);

        // Main engine exhaust
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X, center.Y + size * 0.6), center, rotation),
            RotatePoint(new Point(center.X, center.Y + size * 1.0), center, rotation),
            color, 1.5);
    }

    public static void DrawScout(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Compact triangular body
        var body = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 0.9), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y + size * 0.5), center, rotation),
            RotatePoint(new Point(center.X, center.Y + size * 0.2), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y + size * 0.5), center, rotation)
        };
        DrawGlowPolygon(ctx, body, color, 1);

        // Small sensor dish on front
        DrawGlowCircle(ctx,
            RotatePoint(new Point(center.X, center.Y - size * 0.5), center, rotation),
            size * 0.15, color, 1);

        // Twin engine pods
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.3, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.3, center.Y + size * 0.7), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X + size * 0.3, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.3, center.Y + size * 0.7), center, rotation),
            color, 1);
    }

    public static void DrawDestroyer(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Main hull - massive wedge shape
        var hull = new[]
        {
            RotatePoint(new Point(center.X, center.Y - size * 0.9), center, rotation),
            RotatePoint(new Point(center.X + size * 0.3, center.Y - size * 0.7), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.6, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X + size * 0.5, center.Y + size * 0.7), center, rotation),
            RotatePoint(new Point(center.X + size * 0.2, center.Y + size * 0.9), center, rotation),
            RotatePoint(new Point(center.X - size * 0.2, center.Y + size * 0.9), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y + size * 0.7), center, rotation),
            RotatePoint(new Point(center.X - size * 0.6, center.Y + size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.5, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X - size * 0.3, center.Y - size * 0.7), center, rotation)
        };
        DrawGlowPolygon(ctx, hull, color, 2.5);

        // Bridge tower
        var bridge = new[]
        {
            RotatePoint(new Point(center.X - size * 0.15, center.Y - size * 0.5), center, rotation),
            RotatePoint(new Point(center.X + size * 0.15, center.Y - size * 0.5), center, rotation),
            RotatePoint(new Point(center.X + size * 0.2, center.Y - size * 0.2), center, rotation),
            RotatePoint(new Point(center.X - size * 0.2, center.Y - size * 0.2), center, rotation)
        };
        DrawGlowPolygon(ctx, bridge, color, 1.5);

        // Gun turrets (left and right)
        DrawGlowCircle(ctx,
            RotatePoint(new Point(center.X - size * 0.35, center.Y), center, rotation),
            size * 0.1, color, 1.5);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.35, center.Y), center, rotation),
            RotatePoint(new Point(center.X - size * 0.35, center.Y - size * 0.25), center, rotation),
            color, 1.5);

        DrawGlowCircle(ctx,
            RotatePoint(new Point(center.X + size * 0.35, center.Y), center, rotation),
            size * 0.1, color, 1.5);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X + size * 0.35, center.Y), center, rotation),
            RotatePoint(new Point(center.X + size * 0.35, center.Y - size * 0.25), center, rotation),
            color, 1.5);

        // Forward gun turret
        DrawGlowCircle(ctx,
            RotatePoint(new Point(center.X, center.Y - size * 0.3), center, rotation),
            size * 0.08, color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X, center.Y - size * 0.3), center, rotation),
            RotatePoint(new Point(center.X, center.Y - size * 0.55), center, rotation),
            color, 1);

        // Hull detail lines
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.4, center.Y + size * 0.1), center, rotation),
            RotatePoint(new Point(center.X + size * 0.4, center.Y + size * 0.1), center, rotation),
            color, 1);
        DrawGlowLine(ctx,
            RotatePoint(new Point(center.X - size * 0.35, center.Y + size * 0.4), center, rotation),
            RotatePoint(new Point(center.X + size * 0.35, center.Y + size * 0.4), center, rotation),
            color, 1);

        // Engine array (4 engines)
        for (var i = -1.5; i <= 1.5; i += 1.0)
        {
            DrawGlowLine(ctx,
                RotatePoint(new Point(center.X + size * 0.12 * i, center.Y + size * 0.9), center, rotation),
                RotatePoint(new Point(center.X + size * 0.12 * i, center.Y + size * 1.15), center, rotation),
                color, 1.5);
        }
    }

    public static void DrawProjectile(DrawingContext ctx, Point center, double size, Color color)
    {
        // Draw as a bright dot with glow
        for (var i = 3; i >= 0; i--)
        {
            var alpha = (byte)(150 / (i + 1));
            var radius = size + i * 2;
            var glowColor = Color.FromArgb(alpha, color.R, color.G, color.B);
            ctx.DrawEllipse(new SolidColorBrush(glowColor), null, center, radius, radius);
        }

        // Bright core
        ctx.DrawEllipse(new SolidColorBrush(Colors.White), null, center, size * 0.5, size * 0.5);
    }

    public static void DrawShipIcon(DrawingContext ctx, Point center, double size, Color color)
    {
        var points = new[]
        {
            new Point(center.X, center.Y - size),
            new Point(center.X + size * 0.6, center.Y + size),
            new Point(center.X, center.Y + size * 0.5),
            new Point(center.X - size * 0.6, center.Y + size)
        };
        DrawGlowPolygon(ctx, points, color, 1);
    }

    public static void DrawDebrisTriangle(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Draw a spinning triangle debris piece
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);

        var points = new Point[3];
        for (var i = 0; i < 3; i++)
        {
            var angle = rotation + i * Math.PI * 2 / 3;
            points[i] = new Point(
                center.X + Math.Cos(angle) * size,
                center.Y + Math.Sin(angle) * size
            );
        }

        DrawGlowPolygon(ctx, points, color, 1.5);
    }

    public static void DrawDebrisLine(DrawingContext ctx, Point center, double size, float rotation, Color color)
    {
        // Draw a spinning line debris piece
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);

        var halfLen = size / 2;
        var start = new Point(center.X - cos * halfLen, center.Y - sin * halfLen);
        var end = new Point(center.X + cos * halfLen, center.Y + sin * halfLen);

        DrawGlowLine(ctx, start, end, color, 1.5);
    }

    public static void DrawSpark(DrawingContext ctx, Point center, double size, Color color)
    {
        // Draw a small bright spark
        var pen = new Pen(new SolidColorBrush(color), size, lineCap: PenLineCap.Round);
        ctx.DrawLine(pen, center, new Point(center.X + 0.5, center.Y + 0.5));

        // Glow around spark
        var glowColor = Color.FromArgb((byte)(color.A / 2), color.R, color.G, color.B);
        var glowPen = new Pen(new SolidColorBrush(glowColor), size * 2, lineCap: PenLineCap.Round);
        ctx.DrawLine(glowPen, center, new Point(center.X + 0.5, center.Y + 0.5));
    }

    public static void DrawExplosionFlash(DrawingContext ctx, Point center, double size, Color color)
    {
        // Draw vector-style explosion flash with crossing lines
        var lineCount = 12;
        for (var i = 0; i < lineCount; i++)
        {
            var angle = i * Math.PI * 2 / lineCount;
            var length = size * (0.8 + (i % 2) * 0.4); // Alternating lengths
            var endX = center.X + Math.Cos(angle) * length;
            var endY = center.Y + Math.Sin(angle) * length;

            DrawGlowLine(ctx, center, new Point(endX, endY), color, 2);
        }

        // Inner burst
        var innerCount = 6;
        var innerColor = Color.FromArgb(color.A, 255, 255, 200);
        for (var i = 0; i < innerCount; i++)
        {
            var angle = i * Math.PI * 2 / innerCount + Math.PI / 12;
            var length = size * 0.5;
            var endX = center.X + Math.Cos(angle) * length;
            var endY = center.Y + Math.Sin(angle) * length;

            DrawGlowLine(ctx, center, new Point(endX, endY), innerColor, 1.5);
        }
    }

    public static void DrawWarpEffect(DrawingContext ctx, double width, double height, float progress, Color color)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Draw stretching star lines from center
        var lineCount = 32;
        var maxLength = Math.Max(width, height);

        for (var i = 0; i < lineCount; i++)
        {
            var angle = i * Math.PI * 2 / lineCount;
            var startDist = maxLength * 0.05 * (1 - progress);
            var endDist = maxLength * progress;

            var startX = centerX + Math.Cos(angle) * startDist;
            var startY = centerY + Math.Sin(angle) * startDist;
            var endX = centerX + Math.Cos(angle) * endDist;
            var endY = centerY + Math.Sin(angle) * endDist;

            var alpha = (byte)(255 * (1 - Math.Abs(progress - 0.5) * 2));
            var lineColor = Color.FromArgb(alpha, color.R, color.G, color.B);

            var thickness = 1 + progress * 3;
            var pen = new Pen(new SolidColorBrush(lineColor), thickness, lineCap: PenLineCap.Round);
            ctx.DrawLine(pen, new Point(startX, startY), new Point(endX, endY));
        }

        // Central bright flash at peak
        if (progress > 0.4 && progress < 0.6)
        {
            var flashIntensity = 1 - Math.Abs(progress - 0.5) * 10;
            var flashAlpha = (byte)(200 * flashIntensity);
            var flashColor = Color.FromArgb(flashAlpha, 255, 255, 255);
            var flashRadius = maxLength * 0.1 * flashIntensity;

            for (var i = 3; i >= 0; i--)
            {
                var layerAlpha = (byte)(flashAlpha / (i + 1));
                var layerColor = Color.FromArgb(layerAlpha, 255, 255, 255);
                var layerRadius = flashRadius * (1 + i * 0.5);
                ctx.DrawEllipse(new SolidColorBrush(layerColor), null,
                    new Point(centerX, centerY), layerRadius, layerRadius);
            }
        }
    }

    public static void DrawShieldEffect(DrawingContext ctx, double width, double height, float progress)
    {
        // progress goes from 0 (just activated) to 1 (about to expire)
        // Flash bright at activation, fade as it expires
        var intensity = 1f - progress * 0.7f;
        var alpha = (byte)(200 * intensity);
        var glowAlpha = (byte)(80 * intensity);

        var color = Color.FromArgb(alpha, 0, 255, 255);
        var glowColor = Color.FromArgb(glowAlpha, 0, 255, 255);

        var spacing = 45.0;
        var thickness = 1.5;

        // Slight shimmer animation based on progress
        var offset = progress * 15.0;

        // Draw +45 degree lines
        var diagonal = Math.Sqrt(width * width + height * height);
        var numLines = (int)(diagonal / spacing) + 2;

        var glowPen = new Pen(new SolidColorBrush(glowColor), thickness + 4, lineCap: PenLineCap.Round);
        var corePen = new Pen(new SolidColorBrush(color), thickness, lineCap: PenLineCap.Round);

        for (var i = -numLines; i <= numLines; i++)
        {
            // +45 degree lines (top-left to bottom-right)
            var d = i * spacing + offset;
            var x1 = d;
            var y1 = 0.0;
            var x2 = d + height;
            var y2 = height;

            // Clip to screen bounds (approximate)
            var start45 = new Point(x1, y1);
            var end45 = new Point(x2, y2);

            ctx.DrawLine(glowPen, start45, end45);
            ctx.DrawLine(corePen, start45, end45);

            // -45 degree lines (top-right to bottom-left)
            var x3 = width - d;
            var y3 = 0.0;
            var x4 = width - d - height;
            var y4 = height;

            var startN45 = new Point(x3, y3);
            var endN45 = new Point(x4, y4);

            ctx.DrawLine(glowPen, startN45, endN45);
            ctx.DrawLine(corePen, startN45, endN45);
        }

        // Bright flash overlay at activation (first 20% of duration)
        if (progress < 0.2f)
        {
            var flashAlpha = (byte)(100 * (1f - progress / 0.2f));
            var flashColor = Color.FromArgb(flashAlpha, 0, 255, 255);
            ctx.DrawRectangle(new SolidColorBrush(flashColor), null, new Rect(0, 0, width, height));
        }
    }

    private static Point RotatePoint(Point point, Point center, float angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);

        var dx = point.X - center.X;
        var dy = point.Y - center.Y;

        return new Point(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos
        );
    }
}

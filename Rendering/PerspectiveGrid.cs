using System;
using Avalonia;
using Avalonia.Media;

namespace AVAGunner.Rendering;

public class PerspectiveGrid
{
    public float FocalLength { get; set; } = 400f;
    public int GridLines { get; set; } = 8;
    public float MinZ { get; set; } = 50f;
    public float MaxZ { get; set; } = 1000f;
    public int DepthRings { get; set; } = 12;

    private float _scrollOffset;
    private readonly float _scrollSpeed = 100f;

    public void Update(float deltaTime)
    {
        _scrollOffset += _scrollSpeed * deltaTime;
        if (_scrollOffset > (MaxZ - MinZ) / DepthRings)
        {
            _scrollOffset = 0;
        }
    }

    public void Draw(DrawingContext ctx, double width, double height, bool isPlaying)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        var gridColor = isPlaying
            ? Color.FromArgb(40, 0, 255, 128)
            : Color.FromArgb(30, 0, 255, 255);

        // Draw radial lines from center (vanishing point) to edges
        var angles = GridLines * 2;
        for (var i = 0; i < angles; i++)
        {
            var angle = (Math.PI * 2 / angles) * i;
            var endX = centerX + Math.Cos(angle) * Math.Max(width, height);
            var endY = centerY + Math.Sin(angle) * Math.Max(width, height);

            DrawFadingLine(ctx,
                new Point(centerX, centerY),
                new Point(endX, endY),
                gridColor);
        }

        // Draw depth rings (squares that appear to recede)
        for (var i = 0; i < DepthRings; i++)
        {
            var z = MinZ + ((MaxZ - MinZ) / DepthRings) * i + _scrollOffset;
            if (z > MaxZ) z -= (MaxZ - MinZ);

            var scale = FocalLength / z;
            var ringSize = Math.Max(width, height) * 0.8;

            var halfW = ringSize * scale / 2;
            var halfH = ringSize * scale / 2;

            // Fade based on distance
            var alpha = (byte)(40 * (1 - (z - MinZ) / (MaxZ - MinZ)));
            var ringColor = Color.FromArgb(alpha, gridColor.R, gridColor.G, gridColor.B);

            var pen = new Pen(new SolidColorBrush(ringColor), 1);

            var rect = new Rect(
                centerX - halfW,
                centerY - halfH,
                halfW * 2,
                halfH * 2);

            ctx.DrawRectangle(null, pen, rect);
        }

        // Draw "tunnel" corners for extra depth effect
        DrawTunnelCorners(ctx, centerX, centerY, width, height, gridColor);
    }

    private void DrawFadingLine(DrawingContext ctx, Point start, Point end, Color color)
    {
        // Simple line with the color
        var pen = new Pen(new SolidColorBrush(color), 1);
        ctx.DrawLine(pen, start, end);
    }

    private void DrawTunnelCorners(DrawingContext ctx, double centerX, double centerY, double width, double height, Color color)
    {
        var margin = 20.0;
        var cornerColor = Color.FromArgb(60, color.R, color.G, color.B);
        var pen = new Pen(new SolidColorBrush(cornerColor), 1);

        // Lines from corners toward center
        var corners = new[]
        {
            new Point(margin, margin),
            new Point(width - margin, margin),
            new Point(width - margin, height - margin),
            new Point(margin, height - margin)
        };

        foreach (var corner in corners)
        {
            // Line from corner toward center (but not all the way)
            var dirX = centerX - corner.X;
            var dirY = centerY - corner.Y;
            var len = Math.Sqrt(dirX * dirX + dirY * dirY);

            var endX = corner.X + dirX * 0.3;
            var endY = corner.Y + dirY * 0.3;

            ctx.DrawLine(pen, corner, new Point(endX, endY));
        }
    }
}

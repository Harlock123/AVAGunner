using System;
using Avalonia;
using Avalonia.Media;

namespace AVAGunner.Rendering;

public class Starfield
{
    private readonly Star[] _stars;
    private readonly Random _random = new();

    public float FocalLength { get; set; } = 400f;
    public float MinZ { get; set; } = 1f;
    public float MaxZ { get; set; } = 1000f;
    public float Speed { get; set; } = 200f;

    // Field of view for star spawning
    private const float FieldWidth = 800f;
    private const float FieldHeight = 600f;

    public Starfield(int starCount = 200)
    {
        _stars = new Star[starCount];
        InitializeStars();
    }

    private void InitializeStars()
    {
        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = CreateStar(randomZ: true);
        }
    }

    private Star CreateStar(bool randomZ)
    {
        // Spawn in a wide field that will project to screen edges
        var x = (_random.NextSingle() - 0.5f) * FieldWidth * 2;
        var y = (_random.NextSingle() - 0.5f) * FieldHeight * 2;
        var z = randomZ
            ? MinZ + _random.NextSingle() * (MaxZ - MinZ)
            : MinZ + _random.NextSingle() * 50; // Spawn at near end

        // Vary star properties for visual interest
        var brightness = 0.5f + _random.NextSingle() * 0.5f;
        var size = 0.5f + _random.NextSingle() * 1.5f;

        // Some stars have slight color tint
        var colorTint = _random.Next(5);
        var color = colorTint switch
        {
            0 => Color.FromRgb(200, 220, 255), // Blue-white
            1 => Color.FromRgb(255, 240, 220), // Warm white
            2 => Color.FromRgb(255, 200, 150), // Orange tint
            3 => Color.FromRgb(180, 200, 255), // Cool blue
            _ => Color.FromRgb(255, 255, 255)  // Pure white
        };

        return new Star
        {
            X = x,
            Y = y,
            Z = z,
            Brightness = brightness,
            BaseSize = size,
            Color = color
        };
    }

    public void Update(float deltaTime)
    {
        var movement = Speed * deltaTime;

        for (var i = 0; i < _stars.Length; i++)
        {
            // Move star away from camera (increasing Z) - we're looking backward as ship flies forward
            _stars[i].Z += movement;

            // Wrap star to near distance when it reaches far end
            if (_stars[i].Z >= MaxZ)
            {
                _stars[i] = CreateStar(randomZ: false);
            }
        }
    }

    public void Draw(DrawingContext ctx, double width, double height, bool isPlaying)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Draw a subtle deep space gradient background
        DrawSpaceBackground(ctx, width, height, isPlaying);

        // Draw all stars
        foreach (var star in _stars)
        {
            if (star.Z <= 0) continue;

            // Project 3D position to screen
            var scale = FocalLength / star.Z;
            var screenX = centerX + star.X * scale;
            var screenY = centerY + star.Y * scale;

            // Skip stars outside screen bounds (with margin)
            if (screenX < -20 || screenX > width + 20 ||
                screenY < -20 || screenY > height + 20)
                continue;

            // Calculate star size and brightness based on distance
            // Close stars (low Z) are bigger and brighter
            var distanceRatio = 1 - (star.Z - MinZ) / (MaxZ - MinZ);
            var size = star.BaseSize * (0.5f + distanceRatio * 3f);
            var alpha = (byte)(255 * star.Brightness * (0.2f + distanceRatio * 0.8f));

            // Stars get stretched into lines when close (motion blur effect)
            // Stretch stars that are close to camera (low Z, high distanceRatio)
            var stretch = distanceRatio > 0.6f ? (distanceRatio - 0.6f) * 8f : 0f;

            var color = Color.FromArgb(
                alpha,
                star.Color.R,
                star.Color.G,
                star.Color.B);

            DrawStar(ctx, screenX, screenY, size, stretch, centerX, centerY, color, isPlaying);
        }
    }

    private void DrawSpaceBackground(DrawingContext ctx, double width, double height, bool isPlaying)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Subtle radial gradient suggesting depth of space
        var maxRadius = Math.Sqrt(centerX * centerX + centerY * centerY);

        // Draw concentric circles for gradient effect (subtle nebula glow at center)
        var glowColor = isPlaying
            ? Color.FromArgb(8, 50, 100, 150)   // Subtle blue during play
            : Color.FromArgb(10, 100, 50, 150); // Subtle purple when paused

        for (var i = 5; i >= 1; i--)
        {
            var radius = maxRadius * (i / 10.0);
            var alpha = (byte)(glowColor.A * (6 - i) / 5);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B));
            ctx.DrawEllipse(brush, null, new Point(centerX, centerY), radius, radius);
        }
    }

    private void DrawStar(DrawingContext ctx, double x, double y, float size, float stretch,
        double centerX, double centerY, Color color, bool isPlaying)
    {
        if (stretch > 0.5f && isPlaying)
        {
            // Draw stretched star (streak effect for close, fast stars)
            // Stars are moving toward center, so streak trails outward (away from center)
            var dirX = x - centerX;
            var dirY = y - centerY;
            var len = Math.Sqrt(dirX * dirX + dirY * dirY);

            if (len > 0.1)
            {
                dirX /= len;
                dirY /= len;

                var streakLength = size * (1 + stretch * 4);
                // Streak extends outward from star position (trailing behind as star moves toward center)
                var endX = x + dirX * streakLength;
                var endY = y + dirY * streakLength;

                // Draw streak with glow - fades outward
                var pen = new Pen(new SolidColorBrush(color), Math.Max(1, size * 0.8));
                ctx.DrawLine(pen, new Point(x, y), new Point(endX, endY));

                // Bright point at leading edge (the star itself, moving toward center)
                var brightColor = Color.FromArgb(255, 255, 255, 255);
                var brightBrush = new SolidColorBrush(brightColor);
                ctx.DrawEllipse(brightBrush, null, new Point(x, y), size * 0.6, size * 0.6);
            }
        }
        else
        {
            // Draw point star with subtle glow
            if (size > 1.5)
            {
                // Glow layer for brighter stars
                var glowAlpha = (byte)(color.A / 3);
                var glowColor = Color.FromArgb(glowAlpha, color.R, color.G, color.B);
                var glowBrush = new SolidColorBrush(glowColor);
                ctx.DrawEllipse(glowBrush, null, new Point(x, y), size * 2, size * 2);
            }

            // Core star
            var brush = new SolidColorBrush(color);
            ctx.DrawEllipse(brush, null, new Point(x, y), size, size);

            // Bright center for larger stars
            if (size > 1)
            {
                var brightBrush = new SolidColorBrush(Color.FromArgb(color.A, 255, 255, 255));
                ctx.DrawEllipse(brightBrush, null, new Point(x, y), size * 0.5, size * 0.5);
            }
        }
    }

    private struct Star
    {
        public float X;
        public float Y;
        public float Z;
        public float Brightness;
        public float BaseSize;
        public Color Color;
    }
}

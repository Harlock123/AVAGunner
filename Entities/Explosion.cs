using System;
using System.Collections.Generic;
using System.Numerics;

namespace AVAGunner.Entities;

public enum DebrisType
{
    Triangle,
    Line,
    Spark
}

public class ExplosionDebris
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public float Life { get; set; }
    public float MaxLife { get; set; }
    public float Size { get; set; }
    public DebrisType Type { get; set; }
}

public class Explosion : Entity
{
    public List<ExplosionDebris> Debris { get; } = new();
    public float Duration { get; set; } = 0.8f;
    public float Elapsed { get; set; }
    public float InitialSize { get; set; }
    public float FlashIntensity { get; set; } = 1f;

    private static readonly Random Random = new();

    public static Explosion Create(Vector3 position, float size)
    {
        var explosion = new Explosion
        {
            Position = position,
            InitialSize = size,
            BaseSize = size,
            FlashIntensity = 1f
        };

        // Create triangle debris (ship hull pieces)
        var triangleCount = Math.Max(6, (int)(size / 3));
        for (var i = 0; i < triangleCount; i++)
        {
            var angle = (float)(Random.NextDouble() * Math.PI * 2);
            var elevation = (float)(Random.NextDouble() * Math.PI - Math.PI / 2);
            var speed = (float)(Random.NextDouble() * 180 + 60);

            var vx = (float)(Math.Cos(angle) * Math.Cos(elevation) * speed);
            var vy = (float)(Math.Sin(elevation) * speed);
            var vz = (float)(Math.Sin(angle) * Math.Cos(elevation) * speed * 0.4f);

            explosion.Debris.Add(new ExplosionDebris
            {
                Position = position,
                Velocity = new Vector3(vx, vy, vz),
                Rotation = (float)(Random.NextDouble() * Math.PI * 2),
                RotationSpeed = (float)(Random.NextDouble() * 15 - 7.5),
                Life = (float)(Random.NextDouble() * 0.4f + 0.4f),
                MaxLife = (float)(Random.NextDouble() * 0.4f + 0.4f),
                Size = (float)(Random.NextDouble() * size * 0.4f + size * 0.15f),
                Type = DebrisType.Triangle
            });
        }

        // Create line debris (struts, beams)
        var lineCount = Math.Max(8, (int)(size / 2));
        for (var i = 0; i < lineCount; i++)
        {
            var angle = (float)(Random.NextDouble() * Math.PI * 2);
            var speed = (float)(Random.NextDouble() * 200 + 80);

            explosion.Debris.Add(new ExplosionDebris
            {
                Position = position,
                Velocity = new Vector3(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed,
                    (float)(Random.NextDouble() - 0.5) * speed * 0.3f
                ),
                Rotation = angle,
                RotationSpeed = (float)(Random.NextDouble() * 20 - 10),
                Life = (float)(Random.NextDouble() * 0.5f + 0.3f),
                MaxLife = (float)(Random.NextDouble() * 0.5f + 0.3f),
                Size = (float)(Random.NextDouble() * size * 0.6f + size * 0.2f),
                Type = DebrisType.Line
            });
        }

        // Create sparks (small fast particles)
        var sparkCount = Math.Max(12, (int)(size));
        for (var i = 0; i < sparkCount; i++)
        {
            var angle = (float)(Random.NextDouble() * Math.PI * 2);
            var elevation = (float)(Random.NextDouble() * Math.PI - Math.PI / 2);
            var speed = (float)(Random.NextDouble() * 300 + 100);

            explosion.Debris.Add(new ExplosionDebris
            {
                Position = position,
                Velocity = new Vector3(
                    (float)(Math.Cos(angle) * Math.Cos(elevation) * speed),
                    (float)(Math.Sin(elevation) * speed),
                    (float)(Math.Sin(angle) * Math.Cos(elevation) * speed * 0.3f)
                ),
                Rotation = 0,
                RotationSpeed = 0,
                Life = (float)(Random.NextDouble() * 0.2f + 0.15f),
                MaxLife = (float)(Random.NextDouble() * 0.2f + 0.15f),
                Size = (float)(Random.NextDouble() * 3 + 2),
                Type = DebrisType.Spark
            });
        }

        return explosion;
    }

    public override void Update(float deltaTime)
    {
        Elapsed += deltaTime;

        // Flash fades quickly
        FlashIntensity = Math.Max(0, 1 - Elapsed * 5);

        foreach (var debris in Debris)
        {
            debris.Position += debris.Velocity * deltaTime;
            debris.Velocity *= 0.97f; // Drag
            debris.Rotation += debris.RotationSpeed * deltaTime;
            debris.Life -= deltaTime;
        }

        Debris.RemoveAll(d => d.Life <= 0);

        if (Elapsed >= Duration || Debris.Count == 0)
        {
            IsActive = false;
        }
    }

    public float GetProgress()
    {
        return Math.Clamp(Elapsed / Duration, 0, 1);
    }
}

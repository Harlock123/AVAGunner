using System;
using System.Numerics;

namespace AVAGunner.Entities;

public class CapitolMissile : Entity
{
    public int ColorIndex { get; set; }
    public float Speed { get; set; } = 200f;
    public float SpinAngle { get; set; }
    public float SpinSpeed { get; set; } = 8f;

    private static readonly Random Random = new();

    public static CapitolMissile Create(Vector3 turretWorldPos, int colorIndex, float difficultyMultiplier)
    {
        // Direction toward player origin (0,0,0) with slight random spread
        var spreadX = (float)(Random.NextDouble() * 20f - 10f);
        var spreadY = (float)(Random.NextDouble() * 20f - 10f);
        var target = new Vector3(spreadX, spreadY, 0);
        var direction = Vector3.Normalize(target - turretWorldPos);

        var speed = 200f * (0.9f + difficultyMultiplier * 0.1f);

        return new CapitolMissile
        {
            Position = turretWorldPos,
            Velocity = direction * speed,
            ColorIndex = colorIndex,
            Speed = speed,
            BaseSize = 6f,
            SpinAngle = (float)(Random.NextDouble() * MathF.PI * 2),
            SpinSpeed = 6f + (float)(Random.NextDouble() * 4f),
        };
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        SpinAngle += SpinSpeed * deltaTime;

        if (Position.Z <= 0)
        {
            IsActive = false;
        }
    }

    public bool HasReachedPlayer()
    {
        return Position.Z <= 5f;
    }
}

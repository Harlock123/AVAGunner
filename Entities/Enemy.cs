using System;
using System.Numerics;

namespace AVAGunner.Entities;

public enum EnemyType
{
    Fighter,
    Bomber,
    Interceptor,
    Scout,
    Destroyer
}

public class Enemy : Entity
{
    public EnemyType Type { get; set; }
    public int PointValue { get; set; } = 100;
    public float ApproachSpeed { get; set; } = 150f;

    // Rotation angles for 3D effect
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public float RotationSpeedX { get; set; }
    public float RotationSpeedY { get; set; }
    public float RotationSpeedZ { get; set; }

    // Curved trajectory
    public float CurvePhase { get; set; }
    public float CurveAmplitudeX { get; set; }
    public float CurveAmplitudeY { get; set; }
    public float CurveFrequency { get; set; }

    // Track if we've triggered the "passed" event
    public bool HasTriggeredPass { get; set; }

    private static readonly Random Random = new();

    public Enemy()
    {
        BaseSize = 25f;
    }

    public static Enemy SpawnRandom(float minZ, float maxZ, float spreadX, float spreadY, float difficultyMultiplier)
    {
        var types = Enum.GetValues<EnemyType>();
        var type = types[Random.Next(types.Length)];

        var enemy = new Enemy
        {
            Type = type,
            Position = new Vector3(
                (float)(Random.NextDouble() * 2 - 1) * spreadX,
                (float)(Random.NextDouble() * 2 - 1) * spreadY,
                (float)(Random.NextDouble() * (maxZ - minZ) + minZ)
            ),
            // Random rotation speeds for tumbling effect
            RotationSpeedX = (float)(Random.NextDouble() - 0.5) * 2f,
            RotationSpeedY = (float)(Random.NextDouble() - 0.5) * 3f,
            RotationSpeedZ = (float)(Random.NextDouble() - 0.5) * 1.5f,
            // Curved path parameters
            CurvePhase = (float)(Random.NextDouble() * Math.PI * 2),
            CurveAmplitudeX = (float)(Random.NextDouble() * 40 + 10),
            CurveAmplitudeY = (float)(Random.NextDouble() * 30 + 10),
            CurveFrequency = (float)(Random.NextDouble() * 2 + 1)
        };

        switch (type)
        {
            case EnemyType.Fighter:
                enemy.ApproachSpeed = 180f * difficultyMultiplier;
                enemy.PointValue = 100;
                enemy.BaseSize = 20f;
                enemy.RotationSpeedY *= 1.5f; // Fighters spin more
                break;
            case EnemyType.Bomber:
                enemy.ApproachSpeed = 120f * difficultyMultiplier;
                enemy.PointValue = 150;
                enemy.BaseSize = 35f;
                enemy.CurveAmplitudeX *= 0.5f; // Bombers curve less
                enemy.CurveAmplitudeY *= 0.5f;
                break;
            case EnemyType.Interceptor:
                enemy.ApproachSpeed = 250f * difficultyMultiplier;
                enemy.PointValue = 200;
                enemy.BaseSize = 15f;
                enemy.CurveFrequency *= 1.5f; // Interceptors weave more
                break;
            case EnemyType.Scout:
                enemy.ApproachSpeed = 300f * difficultyMultiplier;
                enemy.PointValue = 75;
                enemy.BaseSize = 12f;
                enemy.CurveFrequency *= 2f; // Scouts are very agile
                enemy.CurveAmplitudeX *= 1.5f;
                enemy.CurveAmplitudeY *= 1.5f;
                enemy.RotationSpeedY *= 2f;
                break;
            case EnemyType.Destroyer:
                enemy.ApproachSpeed = 100f * difficultyMultiplier;
                enemy.PointValue = 300;
                enemy.BaseSize = 45f;
                enemy.CurveAmplitudeX *= 0.3f; // Destroyers are slow and steady
                enemy.CurveAmplitudeY *= 0.3f;
                enemy.RotationSpeedX *= 0.5f;
                enemy.RotationSpeedY *= 0.5f;
                break;
        }

        // Base velocity toward center
        var targetX = (float)(Random.NextDouble() * 0.4 - 0.2) * spreadX;
        var targetY = (float)(Random.NextDouble() * 0.4 - 0.2) * spreadY;

        enemy.Velocity = new Vector3(
            (targetX - enemy.Position.X) / (enemy.Position.Z / enemy.ApproachSpeed),
            (targetY - enemy.Position.Y) / (enemy.Position.Z / enemy.ApproachSpeed),
            -enemy.ApproachSpeed
        );

        return enemy;
    }

    public override void Update(float deltaTime)
    {
        // Update rotation
        RotationX += RotationSpeedX * deltaTime;
        RotationY += RotationSpeedY * deltaTime;
        RotationZ += RotationSpeedZ * deltaTime;

        // Calculate curved path offset based on time/distance
        CurvePhase += CurveFrequency * deltaTime;

        // Apply base velocity
        var baseMove = Velocity * deltaTime;

        // Add sinusoidal curve to X and Y movement
        var curveOffsetX = (float)Math.Sin(CurvePhase) * CurveAmplitudeX * deltaTime;
        var curveOffsetY = (float)Math.Cos(CurvePhase * 0.7f) * CurveAmplitudeY * deltaTime;

        Position += new Vector3(
            baseMove.X + curveOffsetX,
            baseMove.Y + curveOffsetY,
            baseMove.Z
        );
    }

    public bool HasPassedShip()
    {
        return Position.Z <= 10; // Slightly before 0 to trigger earlier
    }

    public bool ShouldRemove()
    {
        return Position.Z <= -50; // Remove when well past the ship
    }

    public float GetCollisionRadius(float focalLength)
    {
        return GetScreenSize(focalLength) * 0.6f;
    }
}

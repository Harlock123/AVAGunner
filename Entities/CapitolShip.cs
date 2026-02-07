using System;
using System.Collections.Generic;
using System.Numerics;

namespace AVAGunner.Entities;

public enum CapitolShipDirection
{
    LeftToRight,
    RightToLeft
}

public class Turret
{
    public Vector3 LocalOffset { get; set; }
    public bool IsDestroyed { get; set; }
    public int ColorIndex { get; set; }
    public float FireTimer { get; set; }
    public float FireInterval { get; set; }
}

public class CapitolShip : Entity
{
    public CapitolShipDirection Direction { get; set; }
    public float TraverseSpeed { get; set; } = 60f;
    public List<Turret> Turrets { get; } = new();
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public float BobPhase { get; set; }
    public float BobAmplitude { get; set; } = 15f;
    public float BobFrequency { get; set; } = 0.5f;
    public const float ShipBaseSize = 160f;
    public const float ShipZ = 450f;
    public const int PointsPerTurret = 500;

    private static readonly Random Random = new();

    // Turret hull positions (local offset in -1..+1 space)
    private static readonly (Vector3 Offset, int ColorIndex)[] TurretDefinitions =
    {
        (new Vector3(-0.6f, -0.2f, 0.25f), 0), // Port forward - Cyan
        (new Vector3(0.6f, -0.2f, 0.25f), 1),   // Starboard forward - Yellow
        (new Vector3(0f, -0.5f, 0.3f), 2),       // Bow center - Green
        (new Vector3(-0.4f, 0.3f, 0.2f), 3),     // Port aft - Orange
        (new Vector3(0.4f, 0.3f, 0.2f), 4),      // Starboard aft - Red
    };

    public static CapitolShip Spawn(float spreadX, float difficultyMultiplier, int encounterNumber = 1)
    {
        var direction = Random.NextDouble() > 0.5
            ? CapitolShipDirection.LeftToRight
            : CapitolShipDirection.RightToLeft;

        var startX = direction == CapitolShipDirection.LeftToRight
            ? -(spreadX + ShipBaseSize)
            : spreadX + ShipBaseSize;

        // Vary Y offset and speed each spawn
        var yOffset = (float)(Random.NextDouble() * 30f - 15f);
        var speedVariation = 50f + (float)(Random.NextDouble() * 20f);
        var rollVariation = (float)(Random.NextDouble() * 0.08f - 0.04f);

        var ship = new CapitolShip
        {
            Position = new Vector3(startX, yOffset, ShipZ),
            BaseSize = ShipBaseSize,
            Direction = direction,
            TraverseSpeed = speedVariation,
            RotationX = 0.15f, // Slight nose-down pitch
            RotationY = direction == CapitolShipDirection.LeftToRight ? -0.1f : 0.1f,
            RotationZ = rollVariation,
            BobPhase = (float)(Random.NextDouble() * MathF.PI * 2),
            BobAmplitude = 10f + (float)(Random.NextDouble() * 10f),
            BobFrequency = 0.3f + (float)(Random.NextDouble() * 0.4f),
        };

        // Fire interval: 4x longer on first encounter (1/4 fire rate), speeding up each time
        // Encounter 1: 4.0x → ~8-12s, Enc 2: 3.28x, Enc 3: 2.69x, Enc 4: 2.20x ...
        var fireIntervalMultiplier = MathF.Max(1.0f, 4.0f * MathF.Pow(0.82f, encounterNumber - 1));

        // Pick 3 random turret positions from the 5 available
        var indices = new List<int> { 0, 1, 2, 3, 4 };
        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (var i = 0; i < 3; i++)
        {
            var (offset, colorIndex) = TurretDefinitions[indices[i]];
            var baseInterval = 2.0f + (float)(Random.NextDouble() * 1.0f) - difficultyMultiplier * 0.15f;
            ship.Turrets.Add(new Turret
            {
                LocalOffset = offset,
                ColorIndex = colorIndex,
                IsDestroyed = false,
                FireTimer = (float)(Random.NextDouble() * 2.0f), // Stagger initial timers
                FireInterval = baseInterval * fireIntervalMultiplier,
            });
        }

        return ship;
    }

    public override void Update(float deltaTime)
    {
        // Move laterally with gentle vertical bob
        var moveDir = Direction == CapitolShipDirection.LeftToRight ? 1f : -1f;
        BobPhase += deltaTime;
        var bobY = (float)Math.Sin(BobPhase * BobFrequency * Math.PI * 2) * BobAmplitude;
        Position = new Vector3(Position.X + moveDir * TraverseSpeed * deltaTime, bobY, Position.Z);

        // Increment turret fire timers
        foreach (var turret in Turrets)
        {
            if (!turret.IsDestroyed)
            {
                turret.FireTimer += deltaTime;
            }
        }
    }

    public Vector3 GetTurretWorldPosition(Turret turret)
    {
        return Position + turret.LocalOffset * ShipBaseSize;
    }

    public bool ShouldRemove(float spreadX)
    {
        var margin = ShipBaseSize + 50f;
        return Direction == CapitolShipDirection.LeftToRight
            ? Position.X > spreadX + margin
            : Position.X < -(spreadX + margin);
    }

    public bool AllTurretsDestroyed()
    {
        foreach (var turret in Turrets)
        {
            if (!turret.IsDestroyed) return false;
        }
        return true;
    }

    public List<Turret> GetReadyToFireTurrets()
    {
        var ready = new List<Turret>();
        foreach (var turret in Turrets)
        {
            if (!turret.IsDestroyed && turret.FireTimer >= turret.FireInterval)
            {
                turret.FireTimer = 0;
                ready.Add(turret);
            }
        }
        return ready;
    }
}

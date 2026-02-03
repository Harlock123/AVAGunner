using System;
using System.Numerics;

namespace AVAGunner.Entities;

public class Projectile : Entity
{
    public float Speed { get; set; } = 800f;
    public float MaxZ { get; set; } = 1000f;

    public Projectile()
    {
        BaseSize = 4f;
    }

    public static Projectile Create(float screenX, float screenY, float centerX, float centerY, float focalLength)
    {
        // Convert screen position to world direction
        var dirX = (screenX - centerX) / focalLength;
        var dirY = (screenY - centerY) / focalLength;

        var projectile = new Projectile
        {
            Position = new Vector3(dirX * 10f, dirY * 10f, 10f)
        };

        // Normalize and apply speed
        var direction = Vector3.Normalize(new Vector3(dirX, dirY, 1f));
        projectile.Velocity = direction * projectile.Speed;

        return projectile;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (Position.Z > MaxZ)
        {
            IsActive = false;
        }
    }

    public bool CheckCollision(Enemy enemy, float focalLength)
    {
        if (!enemy.IsActive || !IsActive)
            return false;

        // Check if projectile is at similar Z depth
        var zDiff = Math.Abs(Position.Z - enemy.Position.Z);
        if (zDiff > 30f)
            return false;

        // Check XY distance at that Z depth
        var dx = Position.X - enemy.Position.X;
        var dy = Position.Y - enemy.Position.Y;
        var distance = (float)Math.Sqrt(dx * dx + dy * dy);

        var hitRadius = enemy.BaseSize * 0.8f;
        return distance < hitRadius;
    }
}

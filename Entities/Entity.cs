using System.Numerics;

namespace AVAGunner.Entities;

public abstract class Entity
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public bool IsActive { get; set; } = true;
    public float BaseSize { get; set; } = 20f;

    public virtual void Update(float deltaTime)
    {
        Position += Velocity * deltaTime;
    }

    public float GetScreenScale(float focalLength)
    {
        if (Position.Z <= 0) return 0;
        return focalLength / Position.Z;
    }

    public Vector2 GetScreenPosition(float centerX, float centerY, float focalLength)
    {
        var scale = GetScreenScale(focalLength);
        return new Vector2(
            centerX + Position.X * scale,
            centerY + Position.Y * scale
        );
    }

    public float GetScreenSize(float focalLength)
    {
        return BaseSize * GetScreenScale(focalLength);
    }
}

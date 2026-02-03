using System;
using System.Numerics;

namespace AVAGunner.Entities;

public class Reticle
{
    public Vector2 ScreenPosition { get; set; }
    public float Size { get; set; } = 30f;
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    private float _cooldown;
    private const float FireCooldown = 0.15f;

    public void UpdateBounds(float width, float height, float margin = 50f)
    {
        MinX = margin;
        MaxX = width - margin;
        MinY = margin;
        MaxY = height - margin;

        // Initialize to center if not set
        if (ScreenPosition == Vector2.Zero)
        {
            ScreenPosition = new Vector2(width / 2, height / 2);
        }
    }

    public void SetPosition(float x, float y)
    {
        ScreenPosition = new Vector2(
            Math.Clamp(x, MinX, MaxX),
            Math.Clamp(y, MinY, MaxY)
        );
    }

    public void Move(float dx, float dy, float speed, float deltaTime)
    {
        var newX = ScreenPosition.X + dx * speed * deltaTime;
        var newY = ScreenPosition.Y + dy * speed * deltaTime;
        SetPosition(newX, newY);
    }

    public void Update(float deltaTime)
    {
        if (_cooldown > 0)
            _cooldown -= deltaTime;
    }

    public bool CanFire()
    {
        return _cooldown <= 0;
    }

    public void Fire()
    {
        _cooldown = FireCooldown;
    }
}

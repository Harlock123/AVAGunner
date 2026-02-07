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

public enum EnemyBounceState
{
    Normal,
    Bouncing,
    Recovering
}

public class Enemy : Entity
{
    public EnemyType Type { get; set; }
    public int PointValue { get; set; } = 100;
    public float ApproachSpeed { get; set; } = 150f;

    // Rotation angles for 3D orientation (calculated from flight direction)
    public float RotationX { get; set; }  // Pitch
    public float RotationY { get; set; }  // Yaw
    public float RotationZ { get; set; }  // Roll

    // Smoothing for rotation (to avoid jittery movement)
    private float _targetRotationX;
    private float _targetRotationY;
    private float _targetRotationZ;
    private const float RotationSmoothing = 8f;

    // Base pitch to orient ships toward the player (nose forward)
    // Ships are modeled with nose pointing -Y, so we pitch 90 degrees to face camera
    private const float BasePitch = -MathF.PI / 2f;

    // Banking intensity when turning (roll into turns)
    public float BankingFactor { get; set; } = 1.0f;

    // Turn rate - how fast the ship can turn toward player (radians per second)
    public float TurnRate { get; set; } = 1.0f;

    // Distance at which ships start turning more aggressively toward player
    public float TurnEngageDistance { get; set; } = 400f;

    // Minimum turn rate multiplier when far away (0 = no turning until close)
    public float MinTurnMultiplier { get; set; } = 0.1f;

    // Weave parameters (small oscillations during flight)
    public float WeavePhase { get; set; }
    public float WeaveAmplitude { get; set; }
    public float WeaveFrequency { get; set; }

    // Current flight direction (normalized, updated as ship turns)
    private Vector3 _flightDirection;

    // Track previous lateral velocity for banking calculation
    private float _prevLateralVelocity;

    // Track if we've triggered the "passed" event
    public bool HasTriggeredPass { get; set; }

    // Bounce/tumble state machine
    public EnemyBounceState BounceState { get; private set; } = EnemyBounceState.Normal;
    private float _bounceTimer;
    private float _recoverTimer;
    private float _tumbleSpeedX;
    private float _tumbleSpeedY;
    private float _tumbleSpeedZ;
    private float _bounceSpeed;
    private const float BounceDuration = 1.5f;
    private const float RecoverDuration = 0.8f;

    private static readonly Random Random = new();

    public Enemy()
    {
        BaseSize = 25f;
    }

    public static Enemy SpawnRandom(float minZ, float maxZ, float spreadX, float spreadY, float difficultyMultiplier)
    {
        var types = Enum.GetValues<EnemyType>();
        var type = types[Random.Next(types.Length)];

        // Choose spawn pattern based on ship type and randomness
        var spawnPattern = Random.Next(5); // 0-4 different entry patterns

        Vector3 position;
        Vector3 initialDirection;

        switch (spawnPattern)
        {
            case 0: // Enter from far left, curving right toward player
                position = new Vector3(
                    -spreadX * (1.2f + (float)Random.NextDouble() * 0.5f),
                    (float)(Random.NextDouble() * 2 - 1) * spreadY * 0.8f,
                    minZ + (float)Random.NextDouble() * (maxZ - minZ) * 0.7f
                );
                initialDirection = Vector3.Normalize(new Vector3(0.6f, 0, -1f));
                break;

            case 1: // Enter from far right, curving left toward player
                position = new Vector3(
                    spreadX * (1.2f + (float)Random.NextDouble() * 0.5f),
                    (float)(Random.NextDouble() * 2 - 1) * spreadY * 0.8f,
                    minZ + (float)Random.NextDouble() * (maxZ - minZ) * 0.7f
                );
                initialDirection = Vector3.Normalize(new Vector3(-0.6f, 0, -1f));
                break;

            case 2: // Enter from above, diving down toward player
                position = new Vector3(
                    (float)(Random.NextDouble() * 2 - 1) * spreadX * 0.8f,
                    -spreadY * (1.0f + (float)Random.NextDouble() * 0.5f),
                    minZ + (float)Random.NextDouble() * (maxZ - minZ) * 0.6f
                );
                initialDirection = Vector3.Normalize(new Vector3(0, 0.5f, -1f));
                break;

            case 3: // Enter from below, climbing toward player
                position = new Vector3(
                    (float)(Random.NextDouble() * 2 - 1) * spreadX * 0.8f,
                    spreadY * (1.0f + (float)Random.NextDouble() * 0.5f),
                    minZ + (float)Random.NextDouble() * (maxZ - minZ) * 0.6f
                );
                initialDirection = Vector3.Normalize(new Vector3(0, -0.5f, -1f));
                break;

            default: // Classic approach from distance (but offset)
                position = new Vector3(
                    (float)(Random.NextDouble() * 2 - 1) * spreadX,
                    (float)(Random.NextDouble() * 2 - 1) * spreadY,
                    maxZ - (float)Random.NextDouble() * 100
                );
                // Slight angle, not perfectly straight
                var angleOffset = (float)(Random.NextDouble() - 0.5) * 0.4f;
                initialDirection = Vector3.Normalize(new Vector3(angleOffset, angleOffset * 0.5f, -1f));
                break;
        }

        var enemy = new Enemy
        {
            Type = type,
            Position = position,
            _flightDirection = initialDirection,
            // Weave parameters for small oscillations during flight
            WeavePhase = (float)(Random.NextDouble() * Math.PI * 2),
            WeaveAmplitude = (float)(Random.NextDouble() * 20 + 5),
            WeaveFrequency = (float)(Random.NextDouble() * 2 + 1)
        };

        switch (type)
        {
            case EnemyType.Fighter:
                enemy.ApproachSpeed = 180f * difficultyMultiplier;
                enemy.PointValue = 100;
                enemy.BaseSize = 20f;
                enemy.BankingFactor = 1.2f;
                enemy.TurnRate = 1.2f;
                enemy.TurnEngageDistance = 350f; // Start turning at medium range
                enemy.MinTurnMultiplier = 0.05f; // Very little turning when far
                enemy.WeaveAmplitude *= 1.0f;
                break;

            case EnemyType.Bomber:
                enemy.ApproachSpeed = 120f * difficultyMultiplier;
                enemy.PointValue = 150;
                enemy.BaseSize = 35f;
                enemy.BankingFactor = 0.5f;
                enemy.TurnRate = 0.5f;
                enemy.TurnEngageDistance = 500f; // Long sweeping approach
                enemy.MinTurnMultiplier = 0.02f; // Commits to trajectory
                enemy.WeaveAmplitude *= 0.3f;
                break;

            case EnemyType.Interceptor:
                enemy.ApproachSpeed = 250f * difficultyMultiplier;
                enemy.PointValue = 200;
                enemy.BaseSize = 15f;
                enemy.BankingFactor = 1.5f;
                enemy.TurnRate = 1.8f;
                enemy.TurnEngageDistance = 300f; // Quick to engage
                enemy.MinTurnMultiplier = 0.1f; // Some early correction
                enemy.WeaveFrequency *= 1.5f;
                break;

            case EnemyType.Scout:
                enemy.ApproachSpeed = 300f * difficultyMultiplier;
                enemy.PointValue = 75;
                enemy.BaseSize = 12f;
                enemy.BankingFactor = 1.8f;
                enemy.TurnRate = 2.0f;
                enemy.TurnEngageDistance = 250f; // Agile, turns quickly when needed
                enemy.MinTurnMultiplier = 0.15f;
                enemy.WeaveAmplitude *= 1.5f;
                enemy.WeaveFrequency *= 2f;
                break;

            case EnemyType.Destroyer:
                enemy.ApproachSpeed = 100f * difficultyMultiplier;
                enemy.PointValue = 300;
                enemy.BaseSize = 45f;
                enemy.BankingFactor = 0.3f;
                enemy.TurnRate = 0.25f;
                enemy.TurnEngageDistance = 600f; // Very long approach run
                enemy.MinTurnMultiplier = 0.0f; // No turning until committed distance
                enemy.WeaveAmplitude *= 0.1f;
                break;
        }

        // Set initial velocity based on flight direction and speed
        enemy.Velocity = enemy._flightDirection * enemy.ApproachSpeed;

        return enemy;
    }

    public void Bounce()
    {
        BounceState = EnemyBounceState.Bouncing;
        _bounceTimer = 0;

        // Reverse flight direction (push back toward spawn area)
        _flightDirection = new Vector3(
            _flightDirection.X * 0.3f,
            _flightDirection.Y * 0.3f,
            MathF.Abs(_flightDirection.Z) + 0.5f
        );
        _flightDirection = Vector3.Normalize(_flightDirection);

        // Bounce speed is ~60% of approach speed
        _bounceSpeed = ApproachSpeed * 0.6f;

        // Generate random wild tumble rotation speeds (8-15 rad/s on all axes)
        _tumbleSpeedX = (float)(Random.NextDouble() * 7 + 8) * (Random.Next(2) == 0 ? 1 : -1);
        _tumbleSpeedY = (float)(Random.NextDouble() * 7 + 8) * (Random.Next(2) == 0 ? 1 : -1);
        _tumbleSpeedZ = (float)(Random.NextDouble() * 7 + 8) * (Random.Next(2) == 0 ? 1 : -1);

        // Reset pass flag so they can threaten the player again on re-approach
        HasTriggeredPass = false;
    }

    public override void Update(float deltaTime)
    {
        if (BounceState == EnemyBounceState.Bouncing)
        {
            UpdateBouncing(deltaTime);
            return;
        }

        if (BounceState == EnemyBounceState.Recovering)
        {
            UpdateRecovering(deltaTime);
            return;
        }

        // Target position is the player (center of screen at Z=0)
        var targetPos = new Vector3(0, 0, 0);

        // Calculate direction to target
        var toTarget = targetPos - Position;
        var distanceToTarget = toTarget.Length();

        if (distanceToTarget > 0.1f)
        {
            var desiredDirection = Vector3.Normalize(toTarget);

            // Scale turn rate based on distance - ships maintain trajectory when far,
            // turn more aggressively as they get closer
            // turnMultiplier goes from MinTurnMultiplier (far) to 1.0 (at TurnEngageDistance or closer)
            float turnMultiplier;
            if (distanceToTarget <= TurnEngageDistance)
            {
                // Within engage distance - full turn rate, scaling up as we get very close
                turnMultiplier = 1.0f + (1.0f - distanceToTarget / TurnEngageDistance) * 0.5f;
            }
            else
            {
                // Beyond engage distance - reduced turning, maintain trajectory
                var farRatio = (distanceToTarget - TurnEngageDistance) / TurnEngageDistance;
                turnMultiplier = Math.Max(MinTurnMultiplier, 1.0f - farRatio * 0.9f);
            }

            // Calculate how much we can turn this frame
            var maxTurnAngle = TurnRate * turnMultiplier * deltaTime;

            // Gradually turn flight direction toward target
            _flightDirection = TurnToward(_flightDirection, desiredDirection, maxTurnAngle);
        }

        // Update weave phase for small oscillations
        WeavePhase += WeaveFrequency * deltaTime;

        // Calculate weave offset (perpendicular to flight direction)
        // This creates a slight side-to-side motion during flight
        var weaveRight = Vector3.Cross(_flightDirection, Vector3.UnitY);
        if (weaveRight.LengthSquared() < 0.01f)
            weaveRight = Vector3.UnitX; // Fallback if flying straight up/down
        else
            weaveRight = Vector3.Normalize(weaveRight);

        var weaveUp = Vector3.Cross(weaveRight, _flightDirection);
        var weaveOffset = weaveRight * (float)Math.Sin(WeavePhase) * WeaveAmplitude * deltaTime
                       + weaveUp * (float)Math.Sin(WeavePhase * 0.7f) * WeaveAmplitude * 0.5f * deltaTime;

        // Update velocity to match current flight direction
        Velocity = _flightDirection * ApproachSpeed;

        // Total velocity including weave
        var totalVelocity = Velocity + weaveOffset / Math.Max(deltaTime, 0.001f);

        // Calculate orientation from velocity direction
        // Yaw: rotation around Y axis (left/right steering)
        _targetRotationY = (float)Math.Atan2(totalVelocity.X, -totalVelocity.Z);

        // Pitch: rotation around X axis (up/down)
        // Base pitch orients nose toward player, then adjust based on vertical velocity
        var horizontalSpeed = (float)Math.Sqrt(totalVelocity.X * totalVelocity.X + totalVelocity.Z * totalVelocity.Z);
        _targetRotationX = BasePitch + (float)Math.Atan2(-totalVelocity.Y, horizontalSpeed);

        // Roll: bank into turns based on lateral velocity change
        var lateralVelocity = totalVelocity.X;
        var lateralAccel = (lateralVelocity - _prevLateralVelocity) / Math.Max(deltaTime, 0.001f);
        _prevLateralVelocity = lateralVelocity;

        // Bank angle proportional to lateral acceleration
        _targetRotationZ = Math.Clamp(-lateralAccel * 0.003f * BankingFactor, -0.7f, 0.7f);

        // Smoothly interpolate current rotation toward target
        var smoothing = RotationSmoothing * deltaTime;
        RotationX += (_targetRotationX - RotationX) * Math.Min(smoothing, 1f);
        RotationY += (_targetRotationY - RotationY) * Math.Min(smoothing, 1f);
        RotationZ += (_targetRotationZ - RotationZ) * Math.Min(smoothing, 1f);

        // Move the ship
        Position += Velocity * deltaTime + weaveOffset;
    }

    private void UpdateBouncing(float deltaTime)
    {
        // Move backward (positive Z direction) at bounce speed
        Position += _flightDirection * _bounceSpeed * deltaTime;

        // Apply wild tumble rotations directly (bypass smooth interpolation)
        RotationX += _tumbleSpeedX * deltaTime;
        RotationY += _tumbleSpeedY * deltaTime;
        RotationZ += _tumbleSpeedZ * deltaTime;

        _bounceTimer += deltaTime;
        if (_bounceTimer >= BounceDuration)
        {
            BounceState = EnemyBounceState.Recovering;
            _recoverTimer = 0;
        }
    }

    private void UpdateRecovering(float deltaTime)
    {
        _recoverTimer += deltaTime;
        var t = Math.Clamp(_recoverTimer / RecoverDuration, 0f, 1f);

        // Gradually reduce tumble rotation speeds toward zero
        _tumbleSpeedX *= (1f - 3f * deltaTime);
        _tumbleSpeedY *= (1f - 3f * deltaTime);
        _tumbleSpeedZ *= (1f - 3f * deltaTime);

        // Apply diminishing tumble
        RotationX += _tumbleSpeedX * deltaTime;
        RotationY += _tumbleSpeedY * deltaTime;
        RotationZ += _tumbleSpeedZ * deltaTime;

        // Gradually re-orient flight direction toward player (0,0,0)
        var toPlayer = -Position;
        if (toPlayer.LengthSquared() > 0.01f)
        {
            var desiredDir = Vector3.Normalize(toPlayer);
            _flightDirection = Vector3.Normalize(Vector3.Lerp(_flightDirection, desiredDir, t));
        }

        // Slow down during recovery
        var speed = _bounceSpeed * (1f - t) + ApproachSpeed * t;
        Position += _flightDirection * speed * 0.3f * deltaTime;

        if (_recoverTimer >= RecoverDuration)
        {
            BounceState = EnemyBounceState.Normal;
            // Reset velocity to approach player again
            Velocity = _flightDirection * ApproachSpeed;
        }
    }

    /// <summary>
    /// Smoothly turn one direction toward another by a maximum angle
    /// </summary>
    private static Vector3 TurnToward(Vector3 current, Vector3 target, float maxAngle)
    {
        // Calculate angle between current and target direction
        var dot = Math.Clamp(Vector3.Dot(current, target), -1f, 1f);
        var angle = (float)Math.Acos(dot);

        // If already facing target (or very close), just return target
        if (angle < 0.001f)
            return target;

        // If we can turn all the way this frame, just return target
        if (angle <= maxAngle)
            return target;

        // Otherwise, turn by maxAngle toward target
        var t = maxAngle / angle;
        return Vector3.Normalize(Vector3.Lerp(current, target, t));
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AVAGunner.Entities;
using AVAGunner.Rendering;

namespace AVAGunner.Game;

public class GameEngine : IDisposable
{
    private readonly GameState _state = new();
    private readonly GameRenderer _renderer = new();
    private readonly InputManager _input;
    private readonly Reticle _reticle = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly List<Explosion> _explosions = new();
    private readonly DispatcherTimer _gameTimer;
    private readonly SoundManager _sound = new();

    private DateTime _lastUpdate = DateTime.Now;
    private float _spawnTimer;
    private float _spawnInterval = 2.5f; // Slower initial spawn
    private bool _disposed;
    private bool _escPressedOnce;

    // Warp transition
    private float _warpTimer;
    private const float WarpDuration = 1.2f;

    private double _screenWidth;
    private double _screenHeight;

    public const float FocalLength = 400f;
    private const float SpawnMinZ = 600f;
    private const float SpawnMaxZ = 900f;
    private const float SpawnSpreadX = 300f;
    private const float SpawnSpreadY = 200f;

    public event Action? OnInvalidate;
    public event Action? OnScreenshotRequested;
    public event Action? OnExitRequested;

    public GameEngine(InputManager input)
    {
        _input = input;
        _renderer.FocalLength = FocalLength;

        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // 60 FPS
        };
        _gameTimer.Tick += GameLoop;
    }

    public void Start()
    {
        _sound.Initialize();
        _gameTimer.Start();
    }

    public void Stop()
    {
        _gameTimer.Stop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gameTimer.Stop();
        _sound.Dispose();
    }

    public void SetScreenSize(double width, double height)
    {
        _screenWidth = width;
        _screenHeight = height;
        _reticle.UpdateBounds((float)width, (float)height);
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var deltaTime = (float)(now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Cap delta time to prevent huge jumps
        deltaTime = Math.Min(deltaTime, 0.1f);

        Update(deltaTime);
        OnInvalidate?.Invoke();
    }

    private void Update(float deltaTime)
    {
        // Handle screenshot request (works in any phase)
        if (_input.IsScreenshotPressed)
        {
            OnScreenshotRequested?.Invoke();
            _input.ClearScreenshotState();
        }

        // Handle escape key
        HandleEscapeKey();

        // Always update explosions even when not playing
        UpdateExplosions(deltaTime);

        switch (_state.Phase)
        {
            case GamePhase.Title:
                UpdateTitle();
                break;
            case GamePhase.Playing:
                UpdateGameplay(deltaTime);
                break;
            case GamePhase.Paused:
                UpdatePaused();
                break;
            case GamePhase.Warping:
                UpdateWarping(deltaTime);
                break;
            case GamePhase.GameOver:
                UpdateGameOver(deltaTime);
                break;
        }

        _renderer.Update(deltaTime, _state.Phase == GamePhase.Playing || _state.Phase == GamePhase.Warping);
    }

    private void HandleEscapeKey()
    {
        if (_input.IsEscapePressed)
        {
            _input.ClearEscapeState();

            switch (_state.Phase)
            {
                case GamePhase.Playing:
                    // First ESC: pause the game
                    _state.Phase = GamePhase.Paused;
                    _escPressedOnce = false;
                    break;

                case GamePhase.Paused:
                    if (!_escPressedOnce)
                    {
                        // First ESC while paused: mark it
                        _escPressedOnce = true;
                    }
                    else
                    {
                        // Second ESC while paused: exit
                        OnExitRequested?.Invoke();
                    }
                    break;

                case GamePhase.Title:
                case GamePhase.GameOver:
                    // ESC on title or game over: exit
                    OnExitRequested?.Invoke();
                    break;
            }
        }
    }

    private void UpdatePaused()
    {
        // Resume on Enter or Start
        if (_input.IsStartPressed)
        {
            _state.Phase = GamePhase.Playing;
            _input.ClearStartState();
            _escPressedOnce = false;
        }
    }

    private void UpdateWarping(float deltaTime)
    {
        _warpTimer += deltaTime;

        // Clear remaining enemies and projectiles during warp
        _enemies.Clear();
        _projectiles.Clear();

        if (_warpTimer >= WarpDuration)
        {
            // Warp complete - start next wave
            _warpTimer = 0;
            _state.Phase = GamePhase.Playing;
            _spawnTimer = 0; // Reset spawn timer for new wave
        }
    }

    public float GetWarpProgress()
    {
        return Math.Clamp(_warpTimer / WarpDuration, 0, 1);
    }

    private void UpdateTitle()
    {
        if (_input.IsStartPressed)
        {
            StartGame();
            _input.ClearStartState();
        }
    }

    private void UpdateGameOver(float deltaTime)
    {
        // Keep updating enemies so they fly past
        foreach (var enemy in _enemies)
        {
            enemy.Update(deltaTime);
        }
        _enemies.RemoveAll(e => e.ShouldRemove());

        if (_input.IsStartPressed)
        {
            StartGame();
            _input.ClearStartState();
        }
    }

    private void StartGame()
    {
        _state.Reset();
        _enemies.Clear();
        _projectiles.Clear();
        _explosions.Clear();
        _spawnTimer = 0;
        _spawnInterval = 2.5f; // Slower initial spawn
        _warpTimer = 0;
        _reticle.ScreenPosition = new Vector2((float)_screenWidth / 2, (float)_screenHeight / 2);
    }

    private void UpdateGameplay(float deltaTime)
    {
        UpdateReticle(deltaTime);
        UpdateShooting();
        UpdateEnemies(deltaTime);
        UpdateProjectiles(deltaTime);
        CheckCollisions();
        UpdateSpawning(deltaTime);

        _reticle.Update(deltaTime);
    }

    private void UpdateExplosions(float deltaTime)
    {
        foreach (var explosion in _explosions)
        {
            explosion.Update(deltaTime);
        }
        _explosions.RemoveAll(ex => !ex.IsActive);
    }

    private void UpdateReticle(float deltaTime)
    {
        // Keyboard movement
        var keyboardMove = _input.GetKeyboardMovement();
        if (keyboardMove != Avalonia.Vector.Zero)
        {
            _reticle.Move((float)keyboardMove.X, (float)keyboardMove.Y, _input.ReticleSpeed, deltaTime);
        }

        // Mouse position (takes priority if mouse has moved)
        if (_input.IsUsingMouse())
        {
            var mousePos = _input.MousePosition;
            _reticle.SetPosition((float)mousePos.X, (float)mousePos.Y);
        }
    }

    private void UpdateShooting()
    {
        if (_input.IsFirePressed && _reticle.CanFire())
        {
            _reticle.Fire();

            var projectile = Projectile.Create(
                _reticle.ScreenPosition.X,
                _reticle.ScreenPosition.Y,
                (float)_screenWidth / 2,
                (float)_screenHeight / 2,
                FocalLength);

            _projectiles.Add(projectile);
            _sound.PlayLaser();
            _input.ClearFireState();
        }
    }

    private void UpdateEnemies(float deltaTime)
    {
        foreach (var enemy in _enemies)
        {
            enemy.Update(deltaTime);

            // Check if enemy has passed the ship (only trigger once per enemy)
            if (enemy.HasPassedShip() && !enemy.HasTriggeredPass && enemy.IsActive)
            {
                enemy.HasTriggeredPass = true;
                _state.LoseLife();

                if (_state.Phase == GamePhase.GameOver)
                {
                    _sound.PlayGameOver();
                }
                else
                {
                    _sound.PlayEnemyPass();
                }
            }
        }

        // Remove enemies that are well past the ship
        _enemies.RemoveAll(e => e.ShouldRemove() || !e.IsActive);
    }

    private void UpdateProjectiles(float deltaTime)
    {
        foreach (var projectile in _projectiles)
        {
            projectile.Update(deltaTime);
        }

        _projectiles.RemoveAll(p => !p.IsActive);
    }

    private void CheckCollisions()
    {
        foreach (var projectile in _projectiles.Where(p => p.IsActive))
        {
            foreach (var enemy in _enemies.Where(e => e.IsActive && !e.HasTriggeredPass))
            {
                if (projectile.CheckCollision(enemy, FocalLength))
                {
                    // Create explosion at enemy position
                    var explosion = Explosion.Create(enemy.Position, enemy.BaseSize);
                    _explosions.Add(explosion);

                    projectile.IsActive = false;
                    enemy.IsActive = false;
                    _state.AddScore(enemy.PointValue);
                    _state.EnemiesDestroyedThisWave++;
                    _sound.PlayExplosion();

                    // Check for wave completion - trigger warp transition
                    if (_state.EnemiesDestroyedThisWave >= _state.EnemiesPerWave)
                    {
                        _state.NextWave();
                        // Slower spawn interval decrease
                        _spawnInterval = Math.Max(1.5f, _spawnInterval - 0.1f);
                        // Start warp transition
                        _state.Phase = GamePhase.Warping;
                        _warpTimer = 0;
                        _sound.PlayWarp();
                    }

                    break;
                }
            }
        }
    }

    private void UpdateSpawning(float deltaTime)
    {
        _spawnTimer += deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0;

            // Spawn 1-2 enemies based on wave (slower spawning)
            var spawnCount = Math.Min(2, 1 + _state.Wave / 4);
            for (var i = 0; i < spawnCount; i++)
            {
                var enemy = Enemy.SpawnRandom(
                    SpawnMinZ, SpawnMaxZ,
                    SpawnSpreadX, SpawnSpreadY,
                    _state.DifficultyMultiplier);
                _enemies.Add(enemy);
            }
        }
    }

    public void Render(DrawingContext ctx)
    {
        _renderer.Draw(ctx, _screenWidth, _screenHeight, _state, _reticle, _enemies, _projectiles, _explosions, GetWarpProgress());
    }
}

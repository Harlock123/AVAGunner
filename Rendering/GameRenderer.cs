using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AVAGunner.Entities;
using AVAGunner.Game;

namespace AVAGunner.Rendering;

public class GameRenderer
{
    private readonly Starfield _starfield = new(250);

    // 3D wireframe ship models
    private static readonly Wireframe3D FighterModel = Wireframe3D.CreateFighter();
    private static readonly Wireframe3D BomberModel = Wireframe3D.CreateBomber();
    private static readonly Wireframe3D InterceptorModel = Wireframe3D.CreateInterceptor();
    private static readonly Wireframe3D ScoutModel = Wireframe3D.CreateScout();
    private static readonly Wireframe3D DestroyerModel = Wireframe3D.CreateDestroyer();
    private static readonly Wireframe3D CapitolShipHullModel = Wireframe3D.CreateCapitolShipHull();
    private static readonly Wireframe3D TurretModel = Wireframe3D.CreateTurret();
    private static readonly Wireframe3D MissileModel = Wireframe3D.CreateMissile();

    private static readonly Color[] TurretColors =
    {
        VectorGraphics.CyanNeon,    // 0 - Cyan
        VectorGraphics.YellowNeon,  // 1 - Yellow
        VectorGraphics.GreenNeon,   // 2 - Green
        VectorGraphics.OrangeNeon,  // 3 - Orange
        VectorGraphics.RedNeon,     // 4 - Red
    };

    public float FocalLength { get; set; } = 400f;

    public void Update(float deltaTime, bool isPlaying)
    {
        // Always update starfield for continuous motion feel
        _starfield.Update(deltaTime);
    }

    public void Draw(DrawingContext ctx, double width, double height, GameState state,
        Reticle reticle, IEnumerable<Enemy> enemies, IEnumerable<Projectile> projectiles,
        IEnumerable<Explosion> explosions, float warpProgress = 0,
        bool shieldActive = false, float shieldProgress = 0,
        CapitolShip? capitolShip = null, IEnumerable<CapitolMissile>? capitolMissiles = null)
    {
        var centerX = (float)(width / 2);
        var centerY = (float)(height / 2);

        // Draw starfield background
        _starfield.Draw(ctx, width, height, state.Phase == GamePhase.Playing || state.Phase == GamePhase.Warping);

        // Draw game elements based on phase
        switch (state.Phase)
        {
            case GamePhase.Title:
                DrawTitleScreen(ctx, width, height);
                break;

            case GamePhase.Playing:
                DrawGameplay(ctx, width, height, centerX, centerY, reticle, enemies, projectiles, explosions, shieldActive, shieldProgress, capitolShip, capitolMissiles);
                DrawHUD(ctx, width, height, state, capitolShip);
                break;

            case GamePhase.Paused:
                DrawGameplay(ctx, width, height, centerX, centerY, reticle, enemies, projectiles, explosions, capitolShip: capitolShip, capitolMissiles: capitolMissiles);
                DrawHUD(ctx, width, height, state, capitolShip);
                DrawPausedScreen(ctx, width, height);
                break;

            case GamePhase.Warping:
                DrawGameplay(ctx, width, height, centerX, centerY, reticle, enemies, projectiles, explosions, capitolShip: capitolShip, capitolMissiles: capitolMissiles);
                DrawWarp(ctx, width, height, warpProgress);
                DrawWarpHUD(ctx, width, height, state);
                break;

            case GamePhase.GameOver:
                DrawGameplay(ctx, width, height, centerX, centerY, reticle, enemies, projectiles, explosions, capitolShip: capitolShip, capitolMissiles: capitolMissiles);
                DrawHUD(ctx, width, height, state, capitolShip);
                DrawGameOverScreen(ctx, width, height, state);
                break;
        }
    }

    private void DrawTitleScreen(DrawingContext ctx, double width, double height)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Title
        var titleText = "AVA GUNNER";
        var titleWidth = titleText.Length * 32;
        VectorGraphics.DrawGlowText(ctx, titleText,
            new Point(centerX - titleWidth / 2, centerY - 100),
            VectorGraphics.CyanNeon, 48);

        // Subtitle
        var subtitle = "TAIL GUNNER REDUX";
        var subtitleWidth = subtitle.Length * 12;
        VectorGraphics.DrawGlowText(ctx, subtitle,
            new Point(centerX - subtitleWidth / 2, centerY - 40),
            VectorGraphics.MagentaNeon, 20);

        // Instructions
        var instructions = "PRESS ENTER TO START";
        var instWidth = instructions.Length * 10;
        VectorGraphics.DrawGlowText(ctx, instructions,
            new Point(centerX - instWidth / 2, centerY + 60),
            VectorGraphics.GreenNeon, 18);

        // Controls info
        var controls1 = "MOUSE OR ARROW KEYS TO AIM";
        var controls2 = "CLICK OR SPACE TO FIRE";
        var controls3 = "V OR RIGHT CLICK FOR SHIELD";
        VectorGraphics.DrawGlowText(ctx, controls1,
            new Point(centerX - controls1.Length * 6, centerY + 120),
            Color.FromArgb(180, 255, 255, 255), 14);
        VectorGraphics.DrawGlowText(ctx, controls2,
            new Point(centerX - controls2.Length * 6, centerY + 145),
            Color.FromArgb(180, 255, 255, 255), 14);
        VectorGraphics.DrawGlowText(ctx, controls3,
            new Point(centerX - controls3.Length * 6, centerY + 170),
            Color.FromArgb(180, 255, 255, 255), 14);

        // Draw decorative ship
        VectorGraphics.DrawFighter(ctx, new Point(centerX, centerY + 220), 30, 0, VectorGraphics.CyanNeon);
    }

    private void DrawGameplay(DrawingContext ctx, double width, double height, float centerX, float centerY,
        Reticle reticle, IEnumerable<Enemy> enemies, IEnumerable<Projectile> projectiles,
        IEnumerable<Explosion> explosions, bool shieldActive = false, float shieldProgress = 0,
        CapitolShip? capitolShip = null, IEnumerable<CapitolMissile>? capitolMissiles = null)
    {
        // Draw capitol ship (far away, behind everything else)
        if (capitolShip != null)
        {
            DrawCapitolShip(ctx, capitolShip, centerX, centerY);
        }

        // Draw capitol missiles
        if (capitolMissiles != null)
        {
            foreach (var missile in capitolMissiles.Where(m => m.IsActive))
            {
                DrawCapitolMissile(ctx, missile, centerX, centerY);
            }
        }

        // Draw projectiles (behind enemies for depth)
        foreach (var proj in projectiles.Where(p => p.IsActive))
        {
            var screenPos = proj.GetScreenPosition(centerX, centerY, FocalLength);
            var size = proj.GetScreenSize(FocalLength);
            VectorGraphics.DrawProjectile(ctx,
                new Point(screenPos.X, screenPos.Y),
                Math.Max(2, size),
                VectorGraphics.YellowNeon);
        }

        // Draw explosions sorted by Z (far to near)
        foreach (var explosion in explosions.Where(e => e.IsActive).OrderByDescending(e => e.Position.Z))
        {
            DrawExplosion(ctx, explosion, centerX, centerY);
        }

        // Draw enemies sorted by Z (far to near)
        foreach (var enemy in enemies.Where(e => e.IsActive).OrderByDescending(e => e.Position.Z))
        {
            var screenPos = enemy.GetScreenPosition(centerX, centerY, FocalLength);
            var size = enemy.GetScreenSize(FocalLength);

            if (size < 2) continue; // Too small to see

            // Fade color based on distance
            var distanceFade = Math.Clamp(1 - (enemy.Position.Z - 100) / 800, 0.3, 1.0);
            var enemyColor = Color.FromArgb(
                (byte)(255 * distanceFade),
                VectorGraphics.MagentaNeon.R,
                VectorGraphics.MagentaNeon.G,
                VectorGraphics.MagentaNeon.B);

            // Get the appropriate 3D wireframe model
            var model = enemy.Type switch
            {
                EnemyType.Fighter => FighterModel,
                EnemyType.Bomber => BomberModel,
                EnemyType.Interceptor => InterceptorModel,
                EnemyType.Scout => ScoutModel,
                EnemyType.Destroyer => DestroyerModel,
                _ => FighterModel
            };

            // Draw 3D wireframe with full rotation on all three axes
            model.Draw(ctx,
                new Point(screenPos.X, screenPos.Y),
                size,
                enemy.RotationX,
                enemy.RotationY,
                enemy.RotationZ,
                enemyColor);
        }

        // Draw shield effect overlay
        if (shieldActive)
        {
            VectorGraphics.DrawShieldEffect(ctx, width, height, shieldProgress);
        }

        // Draw reticle
        VectorGraphics.DrawReticle(ctx,
            new Point(reticle.ScreenPosition.X, reticle.ScreenPosition.Y),
            reticle.Size,
            VectorGraphics.CyanNeon);
    }

    private void DrawCapitolShip(DrawingContext ctx, CapitolShip ship, float centerX, float centerY)
    {
        var screenPos = ship.GetScreenPosition(centerX, centerY, FocalLength);
        var size = ship.GetScreenSize(FocalLength);

        if (size < 2) return;

        // Hull color: light blue-gray
        var hullColor = Color.FromArgb(200, 140, 160, 200);

        // Draw the hull wireframe
        CapitolShipHullModel.Draw(ctx,
            new Point(screenPos.X, screenPos.Y),
            size,
            ship.RotationX,
            ship.RotationY,
            ship.RotationZ,
            hullColor, 1.8f);

        // Draw each turret
        foreach (var turret in ship.Turrets)
        {
            var turretWorldPos = ship.GetTurretWorldPosition(turret);

            // Calculate turret screen position
            if (turretWorldPos.Z <= 0) continue;
            var turretScale = FocalLength / turretWorldPos.Z;
            var turretScreenX = centerX + turretWorldPos.X * turretScale;
            var turretScreenY = centerY + turretWorldPos.Y * turretScale;
            var turretSize = 14f * turretScale; // Turret size

            if (turretSize < 1) continue;

            if (turret.IsDestroyed)
            {
                // Draw X mark for destroyed turrets (dim orange)
                var dimOrange = Color.FromArgb(120, 255, 128, 0);
                var xSize = turretSize * 0.8;
                var center = new Point(turretScreenX, turretScreenY);
                VectorGraphics.DrawGlowLine(ctx,
                    new Point(center.X - xSize, center.Y - xSize),
                    new Point(center.X + xSize, center.Y + xSize),
                    dimOrange, 1.5);
                VectorGraphics.DrawGlowLine(ctx,
                    new Point(center.X + xSize, center.Y - xSize),
                    new Point(center.X - xSize, center.Y + xSize),
                    dimOrange, 1.5);
            }
            else
            {
                // Draw active turret in its neon color
                var turretColor = turret.ColorIndex < TurretColors.Length
                    ? TurretColors[turret.ColorIndex]
                    : VectorGraphics.CyanNeon;

                TurretModel.Draw(ctx,
                    new Point(turretScreenX, turretScreenY),
                    turretSize,
                    ship.RotationX,
                    ship.RotationY,
                    ship.RotationZ,
                    turretColor);
            }
        }
    }

    private void DrawCapitolMissile(DrawingContext ctx, CapitolMissile missile, float centerX, float centerY)
    {
        var screenPos = missile.GetScreenPosition(centerX, centerY, FocalLength);
        var size = missile.GetScreenSize(FocalLength);

        if (size < 1) return;

        var missileColor = missile.ColorIndex < TurretColors.Length
            ? TurretColors[missile.ColorIndex]
            : VectorGraphics.RedNeon;

        // Draw the missile wireframe with spin
        MissileModel.Draw(ctx,
            new Point(screenPos.X, screenPos.Y),
            Math.Max(3, size),
            missile.SpinAngle,
            0f,
            0f,
            missileColor);

        // Draw trailing glow dot
        var glowColor = Color.FromArgb(150, missileColor.R, missileColor.G, missileColor.B);
        var glowSize = Math.Max(2, size * 0.6);
        ctx.DrawEllipse(new SolidColorBrush(glowColor), null,
            new Point(screenPos.X, screenPos.Y), glowSize, glowSize);
    }

    private void DrawExplosion(DrawingContext ctx, Explosion explosion, float centerX, float centerY)
    {
        var progress = explosion.GetProgress();

        // Draw central flash first (background)
        if (explosion.FlashIntensity > 0.1f)
        {
            var flashScale = FocalLength / explosion.Position.Z;
            var flashX = centerX + explosion.Position.X * flashScale;
            var flashY = centerY + explosion.Position.Y * flashScale;
            var flashSize = explosion.InitialSize * flashScale * (1 + (1 - explosion.FlashIntensity) * 2);
            var flashAlpha = (byte)(255 * explosion.FlashIntensity);

            VectorGraphics.DrawExplosionFlash(ctx, new Point(flashX, flashY), flashSize,
                Color.FromArgb(flashAlpha, 255, 200, 100));
        }

        // Draw vector debris
        foreach (var debris in explosion.Debris)
        {
            if (debris.Position.Z <= 0) continue;

            var scale = FocalLength / debris.Position.Z;
            var screenX = centerX + debris.Position.X * scale;
            var screenY = centerY + debris.Position.Y * scale;
            var screenSize = debris.Size * scale;

            if (screenSize < 1) continue;

            var lifeRatio = debris.Life / debris.MaxLife;
            var alpha = (byte)(255 * lifeRatio);

            // Color based on debris type and life
            Color color;
            switch (debris.Type)
            {
                case DebrisType.Triangle:
                    // Hull pieces: magenta fading to dark red
                    color = Color.FromArgb(alpha,
                        (byte)(255 * lifeRatio),
                        (byte)(50 * lifeRatio),
                        (byte)(200 * lifeRatio));
                    VectorGraphics.DrawDebrisTriangle(ctx, new Point(screenX, screenY),
                        screenSize, debris.Rotation, color);
                    break;

                case DebrisType.Line:
                    // Struts: cyan fading to blue
                    color = Color.FromArgb(alpha,
                        (byte)(100 * lifeRatio),
                        (byte)(255 * lifeRatio),
                        (byte)(255));
                    VectorGraphics.DrawDebrisLine(ctx, new Point(screenX, screenY),
                        screenSize, debris.Rotation, color);
                    break;

                case DebrisType.Spark:
                    // Sparks: yellow/orange fading
                    color = Color.FromArgb(alpha,
                        255,
                        (byte)(200 * lifeRatio),
                        (byte)(50 * lifeRatio * lifeRatio));
                    VectorGraphics.DrawSpark(ctx, new Point(screenX, screenY),
                        screenSize, color);
                    break;
            }
        }
    }

    public void DrawWarp(DrawingContext ctx, double width, double height, float progress)
    {
        VectorGraphics.DrawWarpEffect(ctx, width, height, progress, VectorGraphics.CyanNeon);
    }

    private void DrawWarpHUD(DrawingContext ctx, double width, double height, GameState state)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // "WARPING TO WAVE X" text
        var warpText = $"WARPING TO WAVE {state.Wave}";
        var textWidth = warpText.Length * 18;
        VectorGraphics.DrawGlowText(ctx, warpText,
            new Point(centerX - textWidth / 2, centerY + 80),
            VectorGraphics.YellowNeon, 28);
    }

    private void DrawHUD(DrawingContext ctx, double width, double height, GameState state, CapitolShip? capitolShip = null)
    {
        var margin = 20.0;

        // Score (top-left)
        var scoreText = $"SCORE: {state.Score:D6}";
        VectorGraphics.DrawGlowText(ctx, scoreText,
            new Point(margin, margin),
            VectorGraphics.GreenNeon, 20);

        // Wave (below score)
        var waveText = $"WAVE: {state.Wave}";
        VectorGraphics.DrawGlowText(ctx, waveText,
            new Point(margin, margin + 30),
            VectorGraphics.CyanNeon, 16);

        // Lives (top-right as ship icons)
        var livesX = width - margin - 30;
        for (var i = 0; i < state.Lives; i++)
        {
            VectorGraphics.DrawShipIcon(ctx,
                new Point(livesX - i * 35, margin + 15),
                12,
                VectorGraphics.GreenNeon);
        }

        // Shield charges (below lives)
        var shieldText = "SHIELD: " + new string('I', state.ShieldsRemaining);
        VectorGraphics.DrawGlowText(ctx, shieldText,
            new Point(width - margin - shieldText.Length * 10, margin + 40),
            state.ShieldsRemaining > 0 ? VectorGraphics.CyanNeon : Color.FromArgb(100, 100, 100, 100), 14);

        // Capitol ship turret status
        if (capitolShip != null)
        {
            var activeTurrets = 0;
            foreach (var t in capitolShip.Turrets)
                if (!t.IsDestroyed) activeTurrets++;

            var csText = $"CAPITOL SHIP - TURRETS: {activeTurrets}/{capitolShip.Turrets.Count}";
            var csWidth = csText.Length * 10;
            VectorGraphics.DrawGlowText(ctx, csText,
                new Point(width / 2 - csWidth / 2, margin),
                VectorGraphics.OrangeNeon, 16);

            // Row of colored dots showing turret status
            var dotStartX = width / 2 - (capitolShip.Turrets.Count * 20) / 2.0;
            for (var i = 0; i < capitolShip.Turrets.Count; i++)
            {
                var turret = capitolShip.Turrets[i];
                var dotX = dotStartX + i * 20 + 10;
                var dotY = margin + 25;
                var dotColor = turret.IsDestroyed
                    ? Color.FromArgb(100, 100, 100, 100)
                    : (turret.ColorIndex < TurretColors.Length ? TurretColors[turret.ColorIndex] : VectorGraphics.CyanNeon);
                ctx.DrawEllipse(new SolidColorBrush(dotColor), null,
                    new Point(dotX, dotY), 5, 5);
            }
        }

        // Warning zone indicator at bottom
        var dangerPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 64, 64)), 2);
        var dangerY = height - 50;
        ctx.DrawLine(dangerPen, new Point(0, dangerY), new Point(width, dangerY));
    }

    private void DrawPausedScreen(DrawingContext ctx, double width, double height)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Semi-transparent overlay
        ctx.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
            null,
            new Rect(0, 0, width, height));

        // Paused text
        var paused = "PAUSED";
        var pausedWidth = paused.Length * 32;
        VectorGraphics.DrawGlowText(ctx, paused,
            new Point(centerX - pausedWidth / 2, centerY - 40),
            VectorGraphics.YellowNeon, 48);

        // Resume prompt
        var resume = "PRESS ENTER TO RESUME";
        var resumeWidth = resume.Length * 9;
        VectorGraphics.DrawGlowText(ctx, resume,
            new Point(centerX - resumeWidth / 2, centerY + 30),
            VectorGraphics.GreenNeon, 16);

        // Exit prompt
        var exit = "PRESS ESC TWICE TO EXIT";
        var exitWidth = exit.Length * 8;
        VectorGraphics.DrawGlowText(ctx, exit,
            new Point(centerX - exitWidth / 2, centerY + 60),
            VectorGraphics.CyanNeon, 14);
    }

    private void DrawGameOverScreen(DrawingContext ctx, double width, double height, GameState state)
    {
        var centerX = width / 2;
        var centerY = height / 2;

        // Semi-transparent overlay
        ctx.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            null,
            new Rect(0, 0, width, height));

        // Game Over text
        var gameOver = "GAME OVER";
        var goWidth = gameOver.Length * 28;
        VectorGraphics.DrawGlowText(ctx, gameOver,
            new Point(centerX - goWidth / 2, centerY - 60),
            VectorGraphics.RedNeon, 42);

        // Final score
        var finalScore = $"FINAL SCORE: {state.Score}";
        var fsWidth = finalScore.Length * 14;
        VectorGraphics.DrawGlowText(ctx, finalScore,
            new Point(centerX - fsWidth / 2, centerY),
            VectorGraphics.YellowNeon, 24);

        // Wave reached
        var waveReached = $"WAVE REACHED: {state.Wave}";
        var wrWidth = waveReached.Length * 10;
        VectorGraphics.DrawGlowText(ctx, waveReached,
            new Point(centerX - wrWidth / 2, centerY + 35),
            VectorGraphics.CyanNeon, 18);

        // Restart prompt
        var restart = "PRESS ENTER TO PLAY AGAIN";
        var rWidth = restart.Length * 9;
        VectorGraphics.DrawGlowText(ctx, restart,
            new Point(centerX - rWidth / 2, centerY + 90),
            VectorGraphics.GreenNeon, 16);
    }
}

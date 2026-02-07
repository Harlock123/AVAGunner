using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace AVAGunner.Rendering;

/// <summary>
/// Represents a 3D wireframe model defined by vertices and edges.
/// Supports full 3D rotation and perspective projection.
/// </summary>
public class Wireframe3D
{
    public Vector3[] Vertices { get; }
    public (int A, int B, float Thickness)[] Edges { get; }

    public Wireframe3D(Vector3[] vertices, (int A, int B, float Thickness)[] edges)
    {
        Vertices = vertices;
        Edges = edges;
    }

    /// <summary>
    /// Transforms and draws the wireframe with 3D rotation and perspective projection.
    /// </summary>
    public void Draw(DrawingContext ctx, Point screenCenter, double scale,
        float rotationX, float rotationY, float rotationZ, Color color, float baseThickness = 1.5f)
    {
        // Create rotation matrices
        var rotX = Matrix4x4.CreateRotationX(rotationX);
        var rotY = Matrix4x4.CreateRotationY(rotationY);
        var rotZ = Matrix4x4.CreateRotationZ(rotationZ);
        var rotation = rotZ * rotX * rotY;

        // Transform all vertices
        var transformed = new Vector3[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++)
        {
            transformed[i] = Vector3.Transform(Vertices[i], rotation);
        }

        // Calculate depth-sorted edges for proper occlusion hints
        var edgeDepths = new List<(int Index, float Depth, float BackFacing)>();
        for (var i = 0; i < Edges.Length; i++)
        {
            var (a, b, _) = Edges[i];
            var midZ = (transformed[a].Z + transformed[b].Z) / 2;
            // Calculate how "back-facing" this edge is (positive Z means facing away)
            var backFacing = Math.Max(0, midZ);
            edgeDepths.Add((i, midZ, backFacing));
        }

        // Sort by depth (draw far edges first)
        edgeDepths.Sort((a, b) => b.Depth.CompareTo(a.Depth));

        // Draw edges with depth-based shading
        foreach (var (index, depth, backFacing) in edgeDepths)
        {
            var (a, b, thickness) = Edges[index];
            var v1 = transformed[a];
            var v2 = transformed[b];

            // Project to 2D (simple orthographic with depth hint)
            var p1 = new Point(
                screenCenter.X + v1.X * scale,
                screenCenter.Y + v1.Y * scale
            );
            var p2 = new Point(
                screenCenter.X + v2.X * scale,
                screenCenter.Y + v2.Y * scale
            );

            // Depth-based alpha (edges facing away are dimmer)
            var depthFade = 1.0f - (backFacing * 0.4f);
            var alpha = (byte)(color.A * Math.Clamp(depthFade, 0.3, 1.0));
            var edgeColor = Color.FromArgb(alpha, color.R, color.G, color.B);

            VectorGraphics.DrawGlowLine(ctx, p1, p2, edgeColor, thickness * baseThickness, glowLayers: 1, glowSpread: 1.5);
        }
    }

    // Pre-defined ship models

    public static Wireframe3D CreateFighter()
    {
        // Sleek fighter with actual 3D depth
        var vertices = new Vector3[]
        {
            // Nose
            new(0, -1.0f, 0.1f),
            // Cockpit top
            new(0, -0.6f, 0.25f),
            // Cockpit sides
            new(-0.15f, -0.5f, 0.15f),
            new(0.15f, -0.5f, 0.15f),
            // Fuselage mid top
            new(0, -0.2f, 0.2f),
            // Fuselage mid bottom
            new(0, -0.2f, -0.1f),
            // Fuselage sides
            new(-0.2f, 0.0f, 0.1f),
            new(0.2f, 0.0f, 0.1f),
            // Fuselage rear
            new(0, 0.4f, 0.15f),
            new(0, 0.4f, -0.05f),
            // Left wing tip
            new(-0.8f, 0.5f, 0),
            // Left wing root
            new(-0.2f, 0.3f, 0.05f),
            // Right wing tip
            new(0.8f, 0.5f, 0),
            // Right wing root
            new(0.2f, 0.3f, 0.05f),
            // Left engine
            new(-0.1f, 0.6f, 0.05f),
            new(-0.1f, 0.9f, 0),
            // Right engine
            new(0.1f, 0.6f, 0.05f),
            new(0.1f, 0.9f, 0),
        };

        var edges = new (int, int, float)[]
        {
            // Nose to cockpit
            (0, 1, 1.5f), (0, 2, 1.2f), (0, 3, 1.2f),
            // Cockpit frame
            (1, 2, 1.0f), (1, 3, 1.0f), (2, 3, 1.0f),
            // Cockpit to fuselage
            (1, 4, 1.2f), (2, 6, 1.0f), (3, 7, 1.0f),
            // Fuselage top/bottom connection
            (4, 5, 1.0f), (4, 6, 1.0f), (4, 7, 1.0f),
            (5, 6, 1.0f), (5, 7, 1.0f),
            // Fuselage to rear
            (6, 8, 1.2f), (7, 8, 1.2f), (6, 9, 1.0f), (7, 9, 1.0f),
            (8, 9, 1.0f),
            // Left wing
            (6, 11, 1.5f), (11, 10, 1.5f), (10, 8, 1.2f),
            // Right wing
            (7, 13, 1.5f), (13, 12, 1.5f), (12, 8, 1.2f),
            // Engines
            (8, 14, 1.0f), (14, 15, 1.2f),
            (8, 16, 1.0f), (16, 17, 1.2f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateBomber()
    {
        // Heavy bomber with boxy fuselage
        var vertices = new Vector3[]
        {
            // Nose top
            new(0, -0.8f, 0.2f),
            // Nose bottom
            new(0, -0.8f, -0.1f),
            // Nose sides
            new(-0.3f, -0.6f, 0.1f),
            new(0.3f, -0.6f, 0.1f),
            // Cockpit
            new(-0.25f, -0.5f, 0.3f),
            new(0.25f, -0.5f, 0.3f),
            new(0, -0.7f, 0.3f),
            // Mid fuselage top
            new(-0.4f, 0, 0.25f),
            new(0.4f, 0, 0.25f),
            // Mid fuselage bottom
            new(-0.4f, 0, -0.15f),
            new(0.4f, 0, -0.15f),
            // Rear fuselage
            new(-0.3f, 0.8f, 0.2f),
            new(0.3f, 0.8f, 0.2f),
            new(-0.3f, 0.8f, -0.1f),
            new(0.3f, 0.8f, -0.1f),
            // Left engine nacelle
            new(-0.7f, -0.1f, 0.1f),
            new(-0.9f, 0.1f, 0.1f),
            new(-0.9f, 0.6f, 0.05f),
            new(-0.7f, 0.5f, 0.05f),
            // Right engine nacelle
            new(0.7f, -0.1f, 0.1f),
            new(0.9f, 0.1f, 0.1f),
            new(0.9f, 0.6f, 0.05f),
            new(0.7f, 0.5f, 0.05f),
            // Engine exhausts
            new(-0.8f, 1.0f, 0),
            new(0.8f, 1.0f, 0),
        };

        var edges = new (int, int, float)[]
        {
            // Nose
            (0, 1, 1.5f), (0, 2, 1.3f), (0, 3, 1.3f), (1, 2, 1.3f), (1, 3, 1.3f), (2, 3, 1.0f),
            // Cockpit
            (4, 5, 1.0f), (4, 6, 1.0f), (5, 6, 1.0f), (0, 6, 1.0f),
            // Nose to mid
            (2, 7, 1.5f), (3, 8, 1.5f), (2, 9, 1.3f), (3, 10, 1.3f),
            // Mid box
            (7, 8, 1.5f), (9, 10, 1.3f), (7, 9, 1.3f), (8, 10, 1.3f),
            // Mid to rear
            (7, 11, 1.5f), (8, 12, 1.5f), (9, 13, 1.3f), (10, 14, 1.3f),
            // Rear box
            (11, 12, 1.5f), (13, 14, 1.3f), (11, 13, 1.3f), (12, 14, 1.3f),
            // Left nacelle
            (7, 15, 1.2f), (15, 16, 1.5f), (16, 17, 1.5f), (17, 18, 1.2f), (18, 11, 1.2f),
            (15, 18, 1.0f), (16, 17, 1.0f),
            // Right nacelle
            (8, 19, 1.2f), (19, 20, 1.5f), (20, 21, 1.5f), (21, 22, 1.2f), (22, 12, 1.2f),
            (19, 22, 1.0f), (20, 21, 1.0f),
            // Exhausts
            (17, 23, 1.5f), (21, 24, 1.5f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateInterceptor()
    {
        // Needle-shaped fast interceptor
        var vertices = new Vector3[]
        {
            // Sharp nose
            new(0, -1.1f, 0),
            // Forward fuselage
            new(-0.1f, -0.6f, 0.1f),
            new(0.1f, -0.6f, 0.1f),
            new(0, -0.6f, -0.08f),
            // Mid fuselage
            new(-0.12f, -0.1f, 0.12f),
            new(0.12f, -0.1f, 0.12f),
            new(0, -0.1f, -0.1f),
            // Rear fuselage
            new(0, 0.5f, 0.1f),
            new(0, 0.5f, -0.05f),
            // Left wing
            new(-0.1f, -0.1f, 0),
            new(-1.0f, 0.4f, -0.05f),
            new(-0.9f, 0.6f, 0),
            // Right wing
            new(0.1f, -0.1f, 0),
            new(1.0f, 0.4f, -0.05f),
            new(0.9f, 0.6f, 0),
            // Tail fins
            new(0, 0.3f, 0.2f),
            new(-0.2f, 0.8f, 0.15f),
            new(0.2f, 0.8f, 0.15f),
            // Engine
            new(0, 0.6f, 0),
            new(0, 1.0f, 0),
        };

        var edges = new (int, int, float)[]
        {
            // Nose
            (0, 1, 1.3f), (0, 2, 1.3f), (0, 3, 1.3f),
            // Forward frame
            (1, 2, 1.0f), (1, 3, 1.0f), (2, 3, 1.0f),
            // Forward to mid
            (1, 4, 1.3f), (2, 5, 1.3f), (3, 6, 1.2f),
            // Mid frame
            (4, 5, 1.0f), (4, 6, 1.0f), (5, 6, 1.0f),
            // Mid to rear
            (4, 7, 1.3f), (5, 7, 1.3f), (6, 8, 1.2f), (7, 8, 1.0f),
            // Left wing
            (9, 10, 1.5f), (10, 11, 1.5f), (11, 7, 1.2f), (4, 9, 1.0f),
            // Right wing
            (12, 13, 1.5f), (13, 14, 1.5f), (14, 7, 1.2f), (5, 12, 1.0f),
            // Tail fins
            (7, 15, 1.0f), (15, 16, 1.2f), (15, 17, 1.2f),
            // Engine
            (7, 18, 1.2f), (8, 18, 1.0f), (18, 19, 1.5f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateScout()
    {
        // Small, compact scout ship
        var vertices = new Vector3[]
        {
            // Nose/sensor
            new(0, -0.9f, 0),
            // Sensor dish
            new(-0.15f, -0.6f, 0.1f),
            new(0.15f, -0.6f, 0.1f),
            new(0, -0.6f, -0.1f),
            new(0, -0.5f, 0.15f),
            // Body top
            new(-0.3f, 0, 0.15f),
            new(0.3f, 0, 0.15f),
            new(0, -0.2f, 0.2f),
            // Body bottom
            new(-0.3f, 0, -0.1f),
            new(0.3f, 0, -0.1f),
            // Rear
            new(-0.2f, 0.5f, 0.1f),
            new(0.2f, 0.5f, 0.1f),
            new(0, 0.5f, -0.05f),
            // Wing tips
            new(-0.5f, 0.5f, 0),
            new(0.5f, 0.5f, 0),
            // Engines
            new(-0.2f, 0.7f, 0),
            new(0.2f, 0.7f, 0),
        };

        var edges = new (int, int, float)[]
        {
            // Sensor dish
            (0, 1, 1.0f), (0, 2, 1.0f), (0, 3, 1.0f),
            (1, 2, 1.0f), (2, 3, 1.0f), (3, 1, 1.0f),
            (1, 4, 0.8f), (2, 4, 0.8f),
            // Body
            (4, 7, 1.2f), (7, 5, 1.0f), (7, 6, 1.0f),
            (5, 6, 1.2f), (5, 8, 1.0f), (6, 9, 1.0f), (8, 9, 1.0f),
            (3, 8, 1.0f), (3, 9, 1.0f),
            // Body to rear
            (5, 10, 1.2f), (6, 11, 1.2f), (8, 12, 1.0f), (9, 12, 1.0f),
            (10, 11, 1.2f), (10, 12, 1.0f), (11, 12, 1.0f),
            // Wings
            (5, 13, 1.3f), (13, 10, 1.0f),
            (6, 14, 1.3f), (14, 11, 1.0f),
            // Engines
            (10, 15, 1.2f), (11, 16, 1.2f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateDestroyer()
    {
        // Large capital ship with bridge and turrets
        var vertices = new Vector3[]
        {
            // Bow
            new(0, -0.9f, 0.1f),
            new(-0.2f, -0.7f, 0.15f),
            new(0.2f, -0.7f, 0.15f),
            new(-0.2f, -0.7f, -0.05f),
            new(0.2f, -0.7f, -0.05f),
            // Forward hull
            new(-0.4f, -0.3f, 0.2f),
            new(0.4f, -0.3f, 0.2f),
            new(-0.4f, -0.3f, -0.1f),
            new(0.4f, -0.3f, -0.1f),
            // Mid hull
            new(-0.5f, 0.2f, 0.2f),
            new(0.5f, 0.2f, 0.2f),
            new(-0.5f, 0.2f, -0.15f),
            new(0.5f, 0.2f, -0.15f),
            // Rear hull
            new(-0.4f, 0.7f, 0.15f),
            new(0.4f, 0.7f, 0.15f),
            new(-0.4f, 0.7f, -0.1f),
            new(0.4f, 0.7f, -0.1f),
            // Bridge tower
            new(-0.15f, -0.4f, 0.25f),
            new(0.15f, -0.4f, 0.25f),
            new(-0.15f, -0.2f, 0.35f),
            new(0.15f, -0.2f, 0.35f),
            new(0, -0.3f, 0.4f),
            // Gun turrets (positions)
            new(-0.3f, 0, 0.22f),
            new(0.3f, 0, 0.22f),
            new(0, -0.5f, 0.2f),
            // Gun barrels
            new(-0.3f, -0.25f, 0.22f),
            new(0.3f, -0.25f, 0.22f),
            new(0, -0.75f, 0.2f),
            // Engine array
            new(-0.3f, 0.9f, 0.05f),
            new(-0.1f, 0.9f, 0.05f),
            new(0.1f, 0.9f, 0.05f),
            new(0.3f, 0.9f, 0.05f),
            new(-0.3f, 1.1f, 0),
            new(-0.1f, 1.1f, 0),
            new(0.1f, 1.1f, 0),
            new(0.3f, 1.1f, 0),
        };

        var edges = new (int, int, float)[]
        {
            // Bow
            (0, 1, 2.0f), (0, 2, 2.0f), (0, 3, 1.5f), (0, 4, 1.5f),
            (1, 2, 1.5f), (3, 4, 1.5f), (1, 3, 1.5f), (2, 4, 1.5f),
            // Bow to forward
            (1, 5, 2.0f), (2, 6, 2.0f), (3, 7, 1.5f), (4, 8, 1.5f),
            // Forward hull
            (5, 6, 2.0f), (7, 8, 1.5f), (5, 7, 1.5f), (6, 8, 1.5f),
            // Forward to mid
            (5, 9, 2.0f), (6, 10, 2.0f), (7, 11, 1.5f), (8, 12, 1.5f),
            // Mid hull
            (9, 10, 2.0f), (11, 12, 1.5f), (9, 11, 1.5f), (10, 12, 1.5f),
            // Mid to rear
            (9, 13, 2.0f), (10, 14, 2.0f), (11, 15, 1.5f), (12, 16, 1.5f),
            // Rear hull
            (13, 14, 2.0f), (15, 16, 1.5f), (13, 15, 1.5f), (14, 16, 1.5f),
            // Bridge tower
            (17, 18, 1.2f), (19, 20, 1.2f), (17, 19, 1.2f), (18, 20, 1.2f),
            (19, 21, 1.0f), (20, 21, 1.0f),
            (5, 17, 1.0f), (6, 18, 1.0f),
            // Gun turrets
            (22, 25, 1.5f), (23, 26, 1.5f), (24, 27, 1.5f),
            // Hull detail lines
            (5, 6, 1.0f), (9, 10, 1.0f),
            // Engines
            (13, 28, 1.3f), (28, 32, 1.5f),
            (29, 33, 1.5f),
            (30, 34, 1.5f),
            (14, 31, 1.3f), (31, 35, 1.5f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateCapitolShipHull()
    {
        // Large dreadnought hull with pointed bow, wide mid-hull, tapered stern,
        // sponson wings, sensor mast, and ventral keel detail
        var vertices = new Vector3[]
        {
            // Bow point
            new(0, -1.0f, 0.1f),           // 0
            // Forward hull top
            new(-0.2f, -0.8f, 0.2f),       // 1
            new(0.2f, -0.8f, 0.2f),        // 2
            // Forward hull bottom
            new(-0.2f, -0.8f, -0.1f),      // 3
            new(0.2f, -0.8f, -0.1f),       // 4
            // Forward-mid hull top
            new(-0.4f, -0.4f, 0.2f),       // 5
            new(0.4f, -0.4f, 0.2f),        // 6
            // Forward-mid hull bottom
            new(-0.4f, -0.4f, -0.15f),     // 7
            new(0.4f, -0.4f, -0.15f),      // 8
            // Mid hull top (widest)
            new(-0.6f, 0.0f, 0.2f),        // 9
            new(0.6f, 0.0f, 0.2f),         // 10
            // Mid hull bottom
            new(-0.6f, 0.0f, -0.15f),      // 11
            new(0.6f, 0.0f, -0.15f),       // 12
            // Rear-mid hull top
            new(-0.5f, 0.4f, 0.18f),       // 13
            new(0.5f, 0.4f, 0.18f),        // 14
            // Rear-mid hull bottom
            new(-0.5f, 0.4f, -0.12f),      // 15
            new(0.5f, 0.4f, -0.12f),       // 16
            // Stern hull top
            new(-0.35f, 0.8f, 0.15f),      // 17
            new(0.35f, 0.8f, 0.15f),       // 18
            // Stern hull bottom
            new(-0.35f, 0.8f, -0.1f),      // 19
            new(0.35f, 0.8f, -0.1f),       // 20
            // Bridge superstructure
            new(-0.15f, -0.5f, 0.25f),     // 21
            new(0.15f, -0.5f, 0.25f),      // 22
            new(-0.15f, -0.3f, 0.35f),     // 23
            new(0.15f, -0.3f, 0.35f),      // 24
            new(0, -0.4f, 0.4f),           // 25 bridge peak
            // Engine array (5 engines at stern)
            new(-0.28f, 0.95f, 0.05f),     // 26
            new(-0.14f, 0.95f, 0.05f),     // 27
            new(0f, 0.95f, 0.05f),         // 28
            new(0.14f, 0.95f, 0.05f),      // 29
            new(0.28f, 0.95f, 0.05f),      // 30
            // Engine exhaust tips
            new(-0.28f, 1.15f, 0f),        // 31
            new(-0.14f, 1.15f, 0f),        // 32
            new(0f, 1.15f, 0f),            // 33
            new(0.14f, 1.15f, 0f),         // 34
            new(0.28f, 1.15f, 0f),         // 35
            // Port sponson wing
            new(-0.75f, -0.05f, 0.08f),    // 36 forward root
            new(-0.88f, 0.15f, 0.02f),     // 37 tip
            new(-0.7f, 0.35f, 0.05f),      // 38 trailing
            // Starboard sponson wing
            new(0.75f, -0.05f, 0.08f),     // 39 forward root
            new(0.88f, 0.15f, 0.02f),      // 40 tip
            new(0.7f, 0.35f, 0.05f),       // 41 trailing
            // Sensor mast
            new(0, -0.92f, 0.32f),         // 42 mast tip
            // Ventral keel
            new(0, 0.0f, -0.22f),          // 43 keel midpoint
        };

        var edges = new (int, int, float)[]
        {
            // Bow
            (0, 1, 2.0f), (0, 2, 2.0f), (0, 3, 1.5f), (0, 4, 1.5f),
            (1, 2, 1.5f), (3, 4, 1.5f), (1, 3, 1.5f), (2, 4, 1.5f),
            // Bow to forward-mid
            (1, 5, 2.0f), (2, 6, 2.0f), (3, 7, 1.5f), (4, 8, 1.5f),
            // Forward-mid frame
            (5, 6, 1.8f), (7, 8, 1.5f), (5, 7, 1.5f), (6, 8, 1.5f),
            // Forward-mid to mid
            (5, 9, 2.0f), (6, 10, 2.0f), (7, 11, 1.5f), (8, 12, 1.5f),
            // Mid frame (widest)
            (9, 10, 2.0f), (11, 12, 1.5f), (9, 11, 1.5f), (10, 12, 1.5f),
            // Mid to rear-mid
            (9, 13, 2.0f), (10, 14, 2.0f), (11, 15, 1.5f), (12, 16, 1.5f),
            // Rear-mid frame
            (13, 14, 1.8f), (15, 16, 1.5f), (13, 15, 1.5f), (14, 16, 1.5f),
            // Rear-mid to stern
            (13, 17, 1.8f), (14, 18, 1.8f), (15, 19, 1.5f), (16, 20, 1.5f),
            // Stern frame
            (17, 18, 1.8f), (19, 20, 1.5f), (17, 19, 1.5f), (18, 20, 1.5f),
            // Bridge superstructure
            (21, 22, 1.2f), (23, 24, 1.2f), (21, 23, 1.2f), (22, 24, 1.2f),
            (23, 25, 1.0f), (24, 25, 1.0f),
            (5, 21, 1.0f), (6, 22, 1.0f),
            // Hull ridgelines (top center)
            (1, 5, 1.0f), (5, 9, 1.0f),
            // Engines
            (17, 26, 1.2f), (26, 31, 1.5f),
            (27, 32, 1.5f),
            (28, 33, 1.5f),
            (29, 34, 1.5f),
            (18, 30, 1.2f), (30, 35, 1.5f),
            // Port sponson wing
            (9, 36, 1.5f), (36, 37, 1.5f), (37, 38, 1.3f), (38, 13, 1.2f),
            // Starboard sponson wing
            (10, 39, 1.5f), (39, 40, 1.5f), (40, 41, 1.3f), (41, 14, 1.2f),
            // Sensor mast
            (0, 42, 1.0f), (1, 42, 0.8f), (2, 42, 0.8f),
            // Ventral keel
            (7, 43, 1.0f), (43, 15, 1.0f),
            // Bottom hull panel lines
            (3, 7, 1.0f), (4, 8, 1.0f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateTurret()
    {
        // Small turret dome: octagonal base ring, dome spokes to apex, gun barrel
        var vertices = new Vector3[]
        {
            // Octagonal base ring
            new(0.5f, 0, 0),               // 0
            new(0.35f, -0.35f, 0),         // 1
            new(0, -0.5f, 0),              // 2
            new(-0.35f, -0.35f, 0),        // 3
            new(-0.5f, 0, 0),              // 4
            new(-0.35f, 0.35f, 0),         // 5
            new(0, 0.5f, 0),               // 6
            new(0.35f, 0.35f, 0),          // 7
            // Dome apex
            new(0, 0, 0.5f),               // 8
            // Gun barrel
            new(0, -0.5f, 0.15f),          // 9
            new(0, -1.0f, 0.15f),          // 10
            // Second barrel
            new(0, -0.5f, 0.3f),           // 11
            new(0, -1.0f, 0.3f),           // 12
        };

        var edges = new (int, int, float)[]
        {
            // Base ring
            (0, 1, 1.0f), (1, 2, 1.0f), (2, 3, 1.0f), (3, 4, 1.0f),
            (4, 5, 1.0f), (5, 6, 1.0f), (6, 7, 1.0f), (7, 0, 1.0f),
            // Dome spokes (4 cardinal directions to apex)
            (0, 8, 0.8f), (2, 8, 0.8f), (4, 8, 0.8f), (6, 8, 0.8f),
            // Gun barrels extending forward (-Y)
            (9, 10, 1.2f),
            (11, 12, 1.2f),
            // Barrel base connections
            (2, 9, 0.8f), (8, 11, 0.8f),
        };

        return new Wireframe3D(vertices, edges);
    }

    public static Wireframe3D CreateMissile()
    {
        // Small torpedo shape: pointed nose, triangular body cross-section, tail fins
        var vertices = new Vector3[]
        {
            // Pointed nose
            new(0, -1.0f, 0),              // 0
            // Body cross-section (triangular)
            new(0, -0.3f, 0.25f),          // 1 top
            new(-0.2f, -0.3f, -0.15f),     // 2 bottom-left
            new(0.2f, -0.3f, -0.15f),      // 3 bottom-right
            // Rear cross-section
            new(0, 0.4f, 0.25f),           // 4 top
            new(-0.2f, 0.4f, -0.15f),      // 5 bottom-left
            new(0.2f, 0.4f, -0.15f),       // 6 bottom-right
            // Tail fins
            new(0, 0.7f, 0.4f),            // 7 top fin
            new(-0.35f, 0.7f, -0.2f),      // 8 left fin
            new(0.35f, 0.7f, -0.2f),       // 9 right fin
        };

        var edges = new (int, int, float)[]
        {
            // Nose to body
            (0, 1, 1.2f), (0, 2, 1.2f), (0, 3, 1.2f),
            // Body cross-section front
            (1, 2, 1.0f), (2, 3, 1.0f), (3, 1, 1.0f),
            // Body length
            (1, 4, 1.0f), (2, 5, 1.0f), (3, 6, 1.0f),
            // Body cross-section rear
            (4, 5, 1.0f), (5, 6, 1.0f), (6, 4, 1.0f),
            // Tail fins
            (4, 7, 0.8f), (5, 8, 0.8f), (6, 9, 0.8f),
        };

        return new Wireframe3D(vertices, edges);
    }
}

using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;

namespace AVAGunner.Game;

public class InputManager
{
    private readonly HashSet<Key> _pressedKeys = new();
    private Point _mousePosition;
    private bool _mouseLeftPressed;
    private bool _firePressed;
    private bool _startPressed;
    private bool _escapePressed;
    private bool _screenshotPressed;
    private bool _shieldPressed;

    public Point MousePosition => _mousePosition;
    public bool IsFirePressed => _firePressed || _mouseLeftPressed;
    public bool IsStartPressed => _startPressed;
    public bool IsEscapePressed => _escapePressed;
    public bool IsScreenshotPressed => _screenshotPressed;
    public bool IsShieldPressed => _shieldPressed;

    public float ReticleSpeed { get; set; } = 400f;

    public void OnKeyDown(KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);

        if (e.Key == Key.Space)
            _firePressed = true;

        if (e.Key == Key.Enter || e.Key == Key.Return)
            _startPressed = true;

        if (e.Key == Key.Escape)
            _escapePressed = true;

        // Support both Ctrl+S and Cmd+S (for Mac)
        if (e.Key == Key.S && ((e.KeyModifiers & KeyModifiers.Control) != 0 || (e.KeyModifiers & KeyModifiers.Meta) != 0))
            _screenshotPressed = true;

        if (e.Key == Key.V)
            _shieldPressed = true;
    }

    public void OnKeyUp(KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);

        if (e.Key == Key.Space)
            _firePressed = false;

        if (e.Key == Key.Enter || e.Key == Key.Return)
            _startPressed = false;
    }

    public void OnPointerMoved(PointerEventArgs e, Visual relativeTo)
    {
        _mousePosition = e.GetPosition(relativeTo);
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            _mouseLeftPressed = true;

        if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
            _shieldPressed = true;
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _mouseLeftPressed = false;
    }

    public Vector GetKeyboardMovement()
    {
        var dx = 0f;
        var dy = 0f;

        if (_pressedKeys.Contains(Key.Left) || _pressedKeys.Contains(Key.A))
            dx -= 1f;
        if (_pressedKeys.Contains(Key.Right) || _pressedKeys.Contains(Key.D))
            dx += 1f;
        if (_pressedKeys.Contains(Key.Up) || _pressedKeys.Contains(Key.W))
            dy -= 1f;
        if (_pressedKeys.Contains(Key.Down) || _pressedKeys.Contains(Key.S))
            dy += 1f;

        return new Vector(dx, dy);
    }

    public void ClearFireState()
    {
        _firePressed = false;
        _mouseLeftPressed = false;
    }

    public void ClearStartState()
    {
        _startPressed = false;
    }

    public void ClearEscapeState()
    {
        _escapePressed = false;
    }

    public void ClearScreenshotState()
    {
        _screenshotPressed = false;
    }

    public void ClearShieldState()
    {
        _shieldPressed = false;
    }

    public bool IsUsingMouse()
    {
        return _mousePosition.X > 0 || _mousePosition.Y > 0;
    }
}

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AVAGunner.Game;

namespace AVAGunner.Views;

public class GameControl : Control
{
    private readonly InputManager _input = new();
    private readonly GameEngine _engine;

    public GameControl()
    {
        _engine = new GameEngine(_input);
        _engine.OnInvalidate += () => InvalidateVisual();
        _engine.OnScreenshotRequested += CaptureScreenshot;
        _engine.OnExitRequested += RequestExit;

        Focusable = true;
        ClipToBounds = true;
        UseLayoutRounding = true;
    }

    private void CaptureScreenshot()
    {
        try
        {
            var pixelSize = new PixelSize((int)Bounds.Width, (int)Bounds.Height);
            if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            {
                Console.WriteLine("Screenshot failed: Invalid bounds");
                return;
            }

            using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            bitmap.Render(this);

            // Use home directory directly for macOS compatibility
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var screenshotsDir = Path.Combine(homeDir, "Documents", "AVAGunner_Screenshots");
            Directory.CreateDirectory(screenshotsDir);

            var filename = $"AVAGunner_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filepath = Path.Combine(screenshotsDir, filename);

            bitmap.Save(filepath);
            Console.WriteLine($"Screenshot saved: {filepath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Screenshot error: {ex.Message}");
        }
    }

    private void RequestExit()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        window?.Close();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _engine.Start();
        Focus();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _engine.Stop();
        _engine.Dispose();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _engine.SetScreenSize(Bounds.Width, Bounds.Height);
    }

    public override void Render(DrawingContext context)
    {
        // Black background
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));

        _engine.SetScreenSize(Bounds.Width, Bounds.Height);
        _engine.Render(context);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _input.OnKeyDown(e);
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _input.OnKeyUp(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _input.OnPointerMoved(e, this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _input.OnPointerPressed(e);
        Focus();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _input.OnPointerReleased(e);
    }
}

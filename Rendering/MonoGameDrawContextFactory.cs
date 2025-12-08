using CPUgame.Core.Primitives;
using CPUgame.Rendering.Abstractions;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.Rendering;

/// <summary>
/// MonoGame implementation of IDrawContextFactory.
/// Creates MonoGameDrawContext instances for world and screen rendering.
/// </summary>
public class MonoGameDrawContextFactory : IDrawContextFactory
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;

    public MonoGameDrawContextFactory(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _font = font;
    }

    public int ScreenWidth => _graphicsDevice.Viewport.Width;
    public int ScreenHeight => _graphicsDevice.Viewport.Height;

    public IDrawContext CreateWorldContext(Transform2D cameraTransform)
    {
        // For world context, we'd typically apply camera transform via SpriteBatch.Begin
        // The actual transform is applied when SpriteBatch.Begin is called externally
        return new MonoGameDrawContext(_spriteBatch, _pixel, _font);
    }

    public IDrawContext CreateScreenContext()
    {
        return new MonoGameDrawContext(_spriteBatch, _pixel, _font);
    }
}

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Minecraft.Core;

public class GameWindow
{
    public static IWindow window;
    private static IKeyboard primaryKeyboard;

    public GameWindow()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "OpenGL Application";
        var flags = ContextFlags.ForwardCompatible;
            
        flags |= ContextFlags.Debug;
            
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, flags, new APIVersion(4, 6));
        options.PreferredDepthBufferBits = 24;
        window = Window.Create(options);
    }

    public void Run()
    {
        window.Run();
    }

    public void Dispose()
    {
        window.Dispose();
    }
}
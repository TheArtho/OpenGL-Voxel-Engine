using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Minecraft.Core;

public class Inputs
{
    private GL _gl;
    private IWindow window;
    
    /*
    private void KeyDown(IKeyboard keyboard, Key key, int arg3)
            {
                // Close window
                if (key == Key.Escape)
                {
                    window.Close();
                }
                
                // Wireframe Mode
                if (key == Key.Z)
                {
                    wireframeMode = !wireframeMode;
                    if (wireframeMode)
                    {
                        Gl.PolygonMode((GLEnum) GLEnum.FrontAndBack, (PolygonMode) PolygonMode.Line);
                    }
                    else
                    {
                        Gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);
                    }
                }
                
                // Camera Switch
                if (key == Key.F5)
                {
                    if (viewMode == ViewMode.FirstPerson)
                    {
                        viewMode = ViewMode.Orbit;
                        camera = orbit_camera;
                    }
                    else if (viewMode == ViewMode.Orbit)
                    {
                        viewMode = ViewMode.FirstPerson;
                        camera = fps_camera;
                    }
                    // _deferredRenderer.camera = camera;
                    
                    Console.WriteLine($"Switching view mode to {viewMode}");
                }
    
                if (key == Key.F11)
                {
                    if (window.WindowState == WindowState.Fullscreen)
                    {
                        window.WindowState = WindowState.Normal; // Bascule en mode fenêtré
                    }
                    else
                    {
                        window.WindowState = WindowState.Fullscreen; // Bascule en mode plein écran
                    }
                }
            }
            */
}
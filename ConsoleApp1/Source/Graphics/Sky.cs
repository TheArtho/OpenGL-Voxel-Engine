using System.Numerics;
using ConsoleApp1.Source.Graphics.Shapes;
using MathNet.Numerics.LinearAlgebra.Double;
using Minecraft.Core;
using Minecraft.Utils;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Minecraft.Graphics;

public class Sky
{
    private GL _gl;
    
    private CubeGeometry cube;
    private QuadGeometry quad;
    
    private Texture2D sunTexture2D;
    private Texture2D moonTexture2D;
    
    public Sky(GL _gl)
    {
        this._gl = _gl;

        cube = new CubeGeometry(_gl);

        sunTexture2D = new Texture2D(_gl, AssetPaths.texturePath + "Environment/sun.png");
        moonTexture2D = new Texture2D(_gl, AssetPaths.texturePath + "Environment/moon.png");
    }

    public Matrix4x4 Transform()
    {
        return Matrix4x4.CreateTranslation(Engine.MainCamera.Position);
    }

    public void RenderSky()
    {
        cube.Draw();
    }

    public void RenderStar()
    {
        
    }
}